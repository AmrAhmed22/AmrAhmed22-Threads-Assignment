using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public sealed class CustomerDashboardService
    {
        readonly JsonFileStore _store;
        readonly HashSet<int> _failPaymentsForCustomerIds;

        public CustomerDashboardService(JsonFileStore store, IEnumerable<int>? failPaymentsForCustomerIds = null)
        {
            _store = store;
            _failPaymentsForCustomerIds = (failPaymentsForCustomerIds ?? Array.Empty<int>()).ToHashSet();
        }

        public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct)
        {
            Task<Customer> profile = LoadProfileAsync(customerId, ct);
            Task<List<Order>> orders = LoadOrdersAsync(customerId, ct);
            Task<List<PaymentTransaction>> payments = LoadPaymentsAsync(customerId, ct);
            Task<List<EmailMessage>> emails = LoadEmailsAsync(customerId, ct);

            // one await for all four
            Task all = Task.WhenAll(profile, orders, payments, emails);
            try
            {
                await all;
            }
            catch (Exception) when (all.IsFaulted)
            {
                 throw all.Exception!;
            }

            return new CustomerDashboard
            {
                Customer = await profile,
                Orders = await orders,
                Payments = await payments,
                Emails = await emails
            };
        }

        async Task<Customer> LoadProfileAsync(int id, CancellationToken ct)
        {
            await QueryAsync($"profile of customer #{id}", ct);

            List<Customer> customers = await _store.ReadAllAsync<Customer>(DataFiles.Customers, ct);
            return customers.FirstOrDefault(c => c.Id == id)
                ?? throw new CustomerNotFoundException($"Customer #{id} not found");
        }

        async Task<List<Order>> LoadOrdersAsync(int id, CancellationToken ct)
        {
            await QueryAsync($"orders of customer #{id}", ct);

            List<Order> orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            return orders.Where(o => o.CustomerId == id).OrderBy(o => o.Id).ToList();
        }

        async Task<List<PaymentTransaction>> LoadPaymentsAsync(int id, CancellationToken ct)
        {
            await QueryAsync($"payments of customer #{id}", ct);

            if (_failPaymentsForCustomerIds.Contains(id))
                throw new TimeoutException("Payments store timeout");

            List<Order> orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            List<PaymentTransaction> payments = await _store.ReadAllAsync<PaymentTransaction>(DataFiles.Payments, ct);

            HashSet<int> orderIds = orders.Where(o => o.CustomerId == id).Select(o => o.Id).ToHashSet();
            return payments.Where(p => orderIds.Contains(p.OrderId)).OrderBy(p => p.OrderId).ToList();
        }

        async Task<List<EmailMessage>> LoadEmailsAsync(int id, CancellationToken ct)
        {
            await QueryAsync($"emails of customer #{id}", ct);

            List<Order> orders = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            List<EmailMessage> emails = await _store.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct);

            HashSet<int> orderIds = orders.Where(o => o.CustomerId == id).Select(o => o.Id).ToHashSet();
            return emails.Where(e => orderIds.Contains(e.OrderId)).OrderBy(e => e.OrderId).ToList();
        }

        static async Task QueryAsync(string what, CancellationToken ct)
        {
            Log.Info($"DashboardService -> {what}: opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"DashboardService -> {what}: executing query...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            Log.Info($"DashboardService -> {what}: done");
        }
    }
}
