using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class OrderRepository
    {
        readonly JsonFileStore _store;

        public OrderRepository(JsonFileStore store) => _store = store;

        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"OrderRepository -> Saving Order #{order.Id} ({order.Status})...");
            await Task.Delay(400, ct);

            await _store.ModifyAsync<Order>(DataFiles.Orders, list =>
            {
                int index = list.FindIndex(o => o.Id == order.Id);
                if (index >= 0)
                    list[index] = order;
                else
                    list.Add(order);
            }, ct);

            Log.Info("OrderRepository -> Committing...");
            await Task.Delay(200, ct);

            Log.Info($"OrderRepository -> Order #{order.Id} saved");
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Executing query for all orders...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> all = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);

            Log.Info($"OrderRepository -> {all.Count} orders found");
            return all;
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"OrderRepository -> Executing query for orders with status {status}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> all = await _store.ReadAllAsync<Order>(DataFiles.Orders, ct);
            List<Order> filtered = all.Where(o => o.Status == status).ToList();

            Log.Info($"OrderRepository -> {filtered.Count} orders with status {status}");
            return filtered;
        }
    }
}
