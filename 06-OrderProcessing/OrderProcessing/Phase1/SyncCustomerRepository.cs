using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class SyncCustomerRepository
    {
        readonly JsonFileStore _store;

        public SyncCustomerRepository(JsonFileStore store) => _store = store;

        public Customer? GetById(int id)
        {
            Log.Info("SyncCustomerRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncCustomerRepository -> Executing query for Customer #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            Customer? customer = _store.ReadAll<Customer>(DataFiles.Customers).FirstOrDefault(c => c.Id == id);

            Log.Info(customer is null
                ? $"SyncCustomerRepository -> Customer #{id} not found"
                : $"SyncCustomerRepository -> Customer #{id} found ({customer.Name})");

            return customer;
        }
    }
}
