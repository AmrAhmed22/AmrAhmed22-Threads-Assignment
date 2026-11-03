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

        public bool ReserveStock(int productId, int qty)
        {
            Log.Info("SyncProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info("SyncProductRepository -> Beginning transaction...");
            Thread.Sleep(100);

            Log.Info($"SyncProductRepository -> Reserving {qty} x Product #{productId}...");
            Thread.Sleep(400);

            bool reserved = _store.Update<Product, bool>(DataFiles.Products, list =>
            {
                Product? product = list.FirstOrDefault(p => p.Id == productId);
                if (product is null || product.StockQuantity < qty)
                    return false;

                product.StockQuantity -= qty;
                return true;
            });

            Log.Info("SyncProductRepository -> Committing...");
            Thread.Sleep(200);
            return reserved;
        }

        public void ReleaseStock(int productId, int qty)
        {
            Log.Info("SyncProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info("SyncProductRepository -> Beginning transaction...");
            Thread.Sleep(100);

            Log.Info($"SyncProductRepository -> Releasing {qty} x Product #{productId}...");
            Thread.Sleep(400);

            _store.Modify<Product>(DataFiles.Products, list =>
            {
                Product? product = list.FirstOrDefault(p => p.Id == productId);
                if (product is not null)
                    product.StockQuantity += qty;
            });

            Log.Info("SyncProductRepository -> Committing...");
            Thread.Sleep(200);
        }

    }
}
