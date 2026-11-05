using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Storage
{

    public static class DataSeeder
    {
        public static async Task ResetAsync(CancellationToken ct)
        {
            if (Directory.Exists(DataFiles.Folder))
                Directory.Delete(DataFiles.Folder, recursive: true);

            Directory.CreateDirectory(DataFiles.Folder);
            Directory.CreateDirectory(Path.Combine(DataFiles.Folder, "invoices"));
            Directory.CreateDirectory(Path.Combine(DataFiles.Folder, "reports"));

            var store = new JsonFileStore();

            await SeedAsync(store, DataFiles.Customers, SeedData.Customers(), ct);
            await SeedAsync(store, DataFiles.Products, SeedData.Products(), ct);
            await SeedAsync(store, DataFiles.Coupons, SeedData.Coupons(), ct);
            await SeedAsync(store, DataFiles.IncomingOrders, SeedData.IncomingOrders(), ct);


            await SeedAsync(store, DataFiles.Orders, new List<object>(), ct);
            await SeedAsync(store, DataFiles.Payments, new List<object>(), ct);
            await SeedAsync(store, DataFiles.Emails, new List<object>(), ct);
            await SeedAsync(store, DataFiles.EmailRetry, new List<object>(), ct);
        }

        static async Task SeedAsync<T>(JsonFileStore store, string fileName, List<T> records, CancellationToken ct)
        {
            await store.WriteAllAsync(fileName, records, ct);
           
            Console.WriteLine($"[Seeder] {fileName} -> {records.Count} records.");
        }
    }
}
