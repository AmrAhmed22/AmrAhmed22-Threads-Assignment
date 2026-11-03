using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class SyncOrderProcessor
    {
        readonly SyncCustomerRepository _customers;
        readonly SyncInventoryService _inventory;
        readonly SyncPricingService _pricing;
        readonly SyncPaymentGateway _payment;
        readonly SyncOrderRepository _orders;
        readonly SyncEmailService _email;

        public SyncOrderProcessor(SyncCustomerRepository customers, SyncInventoryService inventory,
            SyncPricingService pricing, SyncPaymentGateway payment, SyncOrderRepository orders, SyncEmailService email)
        {
            _customers = customers;
            _inventory = inventory;
            _pricing = pricing;
            _payment = payment;
            _orders = orders;
            _email = email;
        }

        public bool ProcessOrder(Order order)
        {
            Customer? customer = _customers.GetById(order.CustomerId);
            if (customer is null)
                return Stop(order, $"Customer #{order.CustomerId} not found");

            if (!customer.IsActive)
                return Stop(order, "Customer is inactive");

            if (!_inventory.ReserveStock(order))
                return Stop(order, order.FailureReason!);

            _pricing.CalculateTotal(order);

            order.TransactionId = _payment.ProcessPayment(order.Id, order.TotalAmount);

            order.Status = OrderStatus.Completed;
            _orders.Save(order);

            _email.SendConfirmation(order, customer);

            Log.Success($"SyncOrderProcessor -> Order #{order.Id} Completed");
            return true;
        }

        bool Stop(Order order, string reason)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
            _orders.Save(order);

            Log.Error($"SyncOrderProcessor -> Order #{order.Id} Rejected ({reason})");
            return false;
        }
    }
}
