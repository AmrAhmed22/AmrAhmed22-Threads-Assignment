using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class IncomingOrderRepository
    {
        readonly JsonFileStore _store;

        public IncomingOrderRepository(JsonFileStore store) => _store = store;

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("IncomingOrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("IncomingOrderRepository -> Executing query for incoming orders...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> all = await _store.ReadAllAsync<Order>(DataFiles.IncomingOrders, ct);

            Log.Info($"IncomingOrderRepository -> {all.Count} incoming orders found");
            return all;
        }
    }
}
