using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class SyncOrderRepository
    {
        readonly JsonFileStore _store;

        public SyncOrderRepository(JsonFileStore store) => _store = store;

        public void Save(Order order)
        {
            Log.Info("SyncOrderRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info("SyncOrderRepository -> Beginning transaction...");
            Thread.Sleep(100);

            Log.Info($"SyncOrderRepository -> Saving Order #{order.Id} ({order.Status})...");
            Thread.Sleep(400);

            _store.Modify<Order>(DataFiles.Orders, list =>
            {
                int index = list.FindIndex(o => o.Id == order.Id);
                if (index >= 0)
                    list[index] = order;
                else
                    list.Add(order);
            });

            Log.Info("SyncOrderRepository -> Committing...");
            Thread.Sleep(200);
        }
    }
}
