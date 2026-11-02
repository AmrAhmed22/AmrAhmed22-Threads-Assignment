using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class SyncProductRepository
    {
        readonly JsonFileStore _store;

        public SyncProductRepository(JsonFileStore store) => _store = store;

        public Product? GetById(int id)
        {
            Log.Info("SyncProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncProductRepository -> Executing query for Product #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            Product? product = _store.ReadAll<Product>(DataFiles.Products).FirstOrDefault(p => p.Id == id);

            Log.Info(product is null
                ? $"SyncProductRepository -> Product #{id} not found"
                : $"SyncProductRepository -> Product #{id} found ({product.Name}, stock {product.StockQuantity})");

            return product;
        }
    }
}
