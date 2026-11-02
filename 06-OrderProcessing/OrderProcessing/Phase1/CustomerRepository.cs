using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class CustomerRepository
    {
        readonly JsonFileStore _store;

        public CustomerRepository(JsonFileStore store) => _store = store;

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("CustomerRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerRepository -> Executing query for Customer #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Customer> all = await _store.ReadAllAsync<Customer>(DataFiles.Customers, ct);
            Customer? customer = all.FirstOrDefault(c => c.Id == id);

            Log.Info(customer is null
                ? $"CustomerRepository -> Customer #{id} not found"
                : $"CustomerRepository -> Customer #{id} found ({customer.Name})");

            return customer;
        }
    }
}
