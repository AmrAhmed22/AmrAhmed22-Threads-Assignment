using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{

    public static class RaceConditionDemo
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var products = new ProductRepository(store);

            //  unsafe 
            Console.WriteLine("\n--- Race demo: UNSAFE reservation ---");
            await SetKeyboardStockAsync(store, 1, ct);

            Task<bool> unsafe10 = products.ReserveStockUnsafeAsync(4, 1, ct);   // order #10
            Task<bool> unsafe11 = products.ReserveStockUnsafeAsync(4, 1, ct);   // order #11
            bool result10 = await unsafe10;
            bool result11 = await unsafe11;

            Console.WriteLine($"Order #10 got the keyboard: {result10}");
            Console.WriteLine($"Order #11 got the keyboard: {result11}");
            Console.WriteLine($"Stock read back from the file: {await ReadKeyboardStockAsync(store, ct)}");
            if (result10 && result11)
                Console.WriteLine("Both orders got the last keyboard: oversold!");

            //  safe 
            Console.WriteLine("\n--- Race demo: SAFE reservation ---");
            await SetKeyboardStockAsync(store, 1, ct);

            Task<bool> safe10 = products.ReserveStockAsync(4, 1, ct);
            Task<bool> safe11 = products.ReserveStockAsync(4, 1, ct);
            bool safeResult10 = await safe10;
            bool safeResult11 = await safe11;

            Console.WriteLine($"Order #10 got the keyboard: {safeResult10}");
            Console.WriteLine($"Order #11 got the keyboard: {safeResult11}");
            Console.WriteLine($"Stock read back from the file: {await ReadKeyboardStockAsync(store, ct)}");
        }

        static Task SetKeyboardStockAsync(JsonFileStore store, int stock, CancellationToken ct) =>
            store.ModifyAsync<Product>(DataFiles.Products, list =>
                list.First(p => p.Id == 4).StockQuantity = stock, ct);

        static async Task<int> ReadKeyboardStockAsync(JsonFileStore store, CancellationToken ct)
        {
            List<Product> all = await store.ReadAllAsync<Product>(DataFiles.Products, ct);
            return all.First(p => p.Id == 4).StockQuantity;
        }
    }
}
