using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class PaymentGateway : IPaymentGateway
    {
        readonly JsonFileStore _store;
        readonly HashSet<int> _timeoutOrderIds;
        readonly HashSet<int> _declineOrderIds;

        public PaymentGateway(JsonFileStore store,
            IEnumerable<int>? timeoutOrderIds = null, IEnumerable<int>? declineOrderIds = null)
        {
            _store = store;
            _timeoutOrderIds = (timeoutOrderIds ?? Array.Empty<int>()).ToHashSet();
            _declineOrderIds = (declineOrderIds ?? Array.Empty<int>()).ToHashSet();
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Order #{orderId}: authorizing...");
            await Task.Delay(800, ct);

            Log.Info($"PaymentGateway -> Order #{orderId}: fraud check...");
            await Task.Delay(500, ct);

            if (_declineOrderIds.Contains(orderId))
            {
                await SaveAsync(orderId, amount, PaymentStatus.Declined, null, ct);
                return new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            Log.Info($"PaymentGateway -> Order #{orderId}: capturing...");
            await Task.Delay(_timeoutOrderIds.Contains(orderId) ? 5000 : 700, ct);

            string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();
            await SaveAsync(orderId, amount, PaymentStatus.Captured, transactionId, ct);

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured
            };
        }

        public async Task RefundAsync(string transactionId, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Refund {transactionId}: DNS/handshake...");
            await Task.Delay(150, ct);

            Log.Info($"PaymentGateway -> Refund {transactionId}: sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PaymentGateway -> Refund {transactionId}: waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            await _store.ModifyAsync<PaymentTransaction>(DataFiles.Payments, list =>
            {
                PaymentTransaction? payment = list.FirstOrDefault(p => p.TransactionId == transactionId);
                if (payment is not null)
                    payment.Status = PaymentStatus.Refunded;
            }, ct);

            Log.Info($"PaymentGateway -> Refund {transactionId}: done");
        }

        Task SaveAsync(int orderId, decimal amount, PaymentStatus status, string? transactionId, CancellationToken ct) =>
            _store.ModifyAsync<PaymentTransaction>(DataFiles.Payments, list => list.Add(new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amount,
                Status = status,
                TransactionId = transactionId,
                CreatedAt = DateTime.Now
            }), ct);
    }
}
