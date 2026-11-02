using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class ProductRepository
    {
        readonly JsonFileStore _store;

        public ProductRepository(JsonFileStore store) => _store = store;

        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"ProductRepository -> Executing query for Product #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Product> all = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
            Product? product = all.FirstOrDefault(p => p.Id == id);

            Log.Info(product is null
                ? $"ProductRepository -> Product #{id} not found"
                : $"ProductRepository -> Product #{id} found ({product.Name}, stock {product.StockQuantity})");

            return product;
        }

        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
        {
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("ProductRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"ProductRepository -> Reserving {qty} x Product #{productId}...");
            await Task.Delay(400, ct);

            bool reserved = await _store.UpdateAsync<Product, bool>(DataFiles.Products, list =>
            {
                Product? product = list.FirstOrDefault(p => p.Id == productId);
                if (product is null || product.StockQuantity < qty)
                    return false;

                product.StockQuantity -= qty;
                return true;
            }, ct);

            Log.Info("ProductRepository -> Committing...");
            await Task.Delay(200, ct);

            Log.Info(reserved
                ? $"ProductRepository -> Reserved {qty} x Product #{productId}"
                : $"ProductRepository -> Could not reserve {qty} x Product #{productId}");

            return reserved;
        }
    }
}
