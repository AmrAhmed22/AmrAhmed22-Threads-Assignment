using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class OrderProcessor
    {
        readonly CustomerService _customers;
        readonly InventoryService _inventory;
        readonly PricingService _pricing;
        readonly IPaymentGateway _payment;
        readonly OrderRepository _orders;
        readonly IEmailService _email;
        readonly JsonFileStore _store;

        public OrderProcessor(CustomerService customers, InventoryService inventory, PricingService pricing,
            IPaymentGateway payment, OrderRepository orders, IEmailService email, JsonFileStore store)
        {
            _customers = customers;
            _inventory = inventory;
            _pricing = pricing;
            _payment = payment;
            _orders = orders;
            _email = email;
            _store = store;
        }

        public async Task<bool> ProcessOrderAsync(Order order, CancellationToken ct)
        {
            bool stockReserved = false;
            string? transactionId = null;

            try
            {
                var sw = Stopwatch.StartNew();

                Customer customer;
                try
                {
                    customer = await _customers.GetCustomerAsync(order.CustomerId, ct);
                }
                catch (CustomerNotFoundException ex)
                {
                    LogStep(order, 1, "Customer", "NOT FOUND", false, sw);
                    return await StopAsync(order, OrderStatus.Rejected, ex.Message, ct);
                }

                if (!customer.IsActive)
                {
                    LogStep(order, 1, "Customer", "INACTIVE", false, sw);
                    return await StopAsync(order, OrderStatus.Rejected, "Customer is inactive", ct);
                }
                LogStep(order, 1, "Customer", "OK", true, sw);

                sw.Restart();
                if (!await _inventory.ReserveStockAsync(order, ct))
                {
                    LogStep(order, 2, "Stock", "INSUFFICIENT", false, sw);
                    return await StopAsync(order, OrderStatus.Rejected, order.FailureReason!, ct);
                }
                stockReserved = true;
                LogStep(order, 2, "Stock", "OK", true, sw);

                sw.Restart();
                try
                {
                    await _pricing.CalculateTotalAsync(order, ct);
                }
                catch (InvalidCouponException ex)
                {
                    LogStep(order, 3, "Pricing", "INVALID COUPON", false, sw);
                    await _inventory.ReleaseStockAsync(order, CancellationToken.None);
                    stockReserved = false;
                    return await StopAsync(order, OrderStatus.Rejected, ex.Message, ct);
                }
                LogStep(order, 3, "Pricing", "OK", true, sw);

                sw.Restart();
                PaymentResult payment;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                try
                {
                    payment = await _payment.ProcessPaymentAsync(order.Id, order.TotalAmount, timeout.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    Log.Error($"Order #{order.Id} -> Step 4/6 Payment ... TIMEOUT after {sw.ElapsedMilliseconds} ms, released");
                    await _inventory.ReleaseStockAsync(order, CancellationToken.None);
                    stockReserved = false;
                    return await StopAsync(order, OrderStatus.PaymentFailed, "Payment gateway timeout (3s)", ct);
                }

                if (!payment.Success)
                {
                    LogStep(order, 4, "Payment", "DECLINED", false, sw);
                    await _inventory.ReleaseStockAsync(order, CancellationToken.None);
                    stockReserved = false;
                    return await StopAsync(order, OrderStatus.PaymentFailed, payment.Message!, ct);
                }

                transactionId = payment.TransactionId;
                order.TransactionId = transactionId;
                LogStep(order, 4, "Payment", $"OK ({transactionId})", true, sw);

                sw.Restart();
                order.Status = OrderStatus.Completed;
                try
                {
                    await _orders.SaveAsync(order, ct);
                }
                catch (DatabaseWriteException)
                {
                    LogStep(order, 5, "Save", "FAILED, compensating", false, sw);

                    await _payment.RefundAsync(transactionId!, CancellationToken.None);
                    await _inventory.ReleaseStockAsync(order, CancellationToken.None);

                    string reason = $"Database save failed after payment; payment {transactionId} refunded";
                    transactionId = null;
                    stockReserved = false;

                    order.Status = OrderStatus.Failed;
                    order.FailureReason = reason;
                    await _orders.SaveAsync(order, CancellationToken.None);
                    return false;
                }
                LogStep(order, 5, "Save", "OK", true, sw);

                sw.Restart();
                bool sent = await SendConfirmationWithRetryAsync(order, customer, ct);
                LogStep(order, 6, "Email", sent ? "OK" : "QUEUED for retry", sent, sw);

                return order.Status == OrderStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                if (transactionId is not null)
                    await _payment.RefundAsync(transactionId, CancellationToken.None);
                if (stockReserved)
                    await _inventory.ReleaseStockAsync(order, CancellationToken.None);

                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";
                await _orders.SaveAsync(order, CancellationToken.None);

                Log.Error($"Order #{order.Id} -> Cancelled");
                return false;
            }
        }

        async Task<bool> SendConfirmationWithRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            int[] waits = [500, 1000, 2000];
            string lastError = "";

            for (int attempt = 1; attempt <= 4; attempt++)
            {
                if (attempt > 1)
                    await Task.Delay(waits[attempt - 2], ct);

                try
                {
                    await _email.SendConfirmationAsync(order, customer, ct);
                    return true;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    lastError = ex.Message;
                    Log.Error($"Order #{order.Id} -> Email attempt {attempt} failed: {ex.Message}");
                }
            }

            var failed = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmed",
                Body = $"Hi {customer.Name}, your order #{order.Id} (total {order.TotalAmount}) is confirmed.",
                Attempts = 4,
                LastError = lastError
            };
            await _store.ModifyAsync<EmailMessage>(DataFiles.EmailRetry, list => list.Add(failed), ct);

            Log.Error($"Email for Order #{order.Id} queued for retry later");
            return false;
        }

        async Task<bool> StopAsync(Order order, OrderStatus status, string reason, CancellationToken ct)
        {
            order.Status = status;
            order.FailureReason = reason;
            await _orders.SaveAsync(order, ct);
            return false;
        }

        static void LogStep(Order order, int step, string name, string result, bool ok, Stopwatch sw)
        {
            string line = $"Order #{order.Id} -> Step {step}/6 {name} ... {result} in {sw.ElapsedMilliseconds} ms";
            if (ok) Log.Success(line); else Log.Error(line);
        }
    }
}
