using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase3
{
    public static class ComparisonRunner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== Sync vs Async: orders #1-#10, failures disabled =====");

            var store = new JsonFileStore();

            await DataSeeder.ResetAsync(ct);
            List<Order> syncOrders = (await new IncomingOrderRepository(store).GetAllAsync(ct)).Take(10).ToList();

            var customersSync = new SyncCustomerRepository(store);
            var syncProcessor = new SyncOrderProcessor(
                customersSync,
                new SyncInventoryService(new SyncProductRepository(store)),
                new SyncPricingService(store, customersSync, new SyncCouponRepository(store)),
                new SyncPaymentGateway(store),
                new SyncOrderRepository(store),
                new SyncEmailService(store));

            Log.ResetThreads();
            var watch = Stopwatch.StartNew();

            var threads = new List<Thread>();
            foreach (Order order in syncOrders)
                threads.Add(new Thread(() => syncProcessor.ProcessOrder(order)));

            foreach (Thread t in threads)
                t.Start();
            foreach (Thread t in threads)
                t.Join();

            double syncSeconds = watch.Elapsed.TotalSeconds;
            int syncThreads = Log.DistinctThreads;
            int syncPool = ThreadPool.ThreadCount;
            string syncResult = Summary(syncOrders);

            await DataSeeder.ResetAsync(ct);
            List<Order> asyncOrders = (await new IncomingOrderRepository(store).GetAllAsync(ct)).Take(10).ToList();

            var customers = new CustomerRepository(store);
            var products = new ProductRepository(store);
            var coupons = new CouponRepository(store);
            var asyncProcessor = new OrderProcessor(
                new CustomerService(customers),
                new InventoryService(products),
                new Phase2.PricingService(store, customers, coupons, []),
                new PaymentGateway(store),
                new OrderRepository(store),
                new EmailService(store),
                store);

            Log.ResetThreads();
            watch.Restart();

            var tasks = new List<Task<bool>>();
            foreach (Order order in asyncOrders)
                tasks.Add(asyncProcessor.ProcessOrderAsync(order, ct));

            foreach (Task<bool> task in tasks)
                await task;

            double asyncSeconds = watch.Elapsed.TotalSeconds;
            int asyncThreads = Log.DistinctThreads;
            int asyncPool = ThreadPool.ThreadCount;
            string asyncResult = Summary(asyncOrders);

            Console.WriteLine();
            Console.WriteLine($"{"Version",-22}{"Time",8}{"Threads",9}{"Pool",6}  Result");
            Console.WriteLine($"{"Sync (10 threads)",-22}{syncSeconds,7:F1}s{syncThreads,9}{syncPool,6}  {syncResult}");
            Console.WriteLine($"{"Async (10 tasks)",-22}{asyncSeconds,7:F1}s{asyncThreads,9}{asyncPool,6}  {asyncResult}");
        }

        static string Summary(List<Order> orders) =>
            $"{orders.Count(o => o.Status == OrderStatus.Completed)} Completed, " +
            $"{orders.Count(o => o.Status == OrderStatus.Rejected)} Rejected";
    }
}
