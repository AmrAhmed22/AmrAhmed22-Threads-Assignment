using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase2
{

    public static class Phase2Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var customers = new CustomerRepository(store);
            var products = new ProductRepository(store);
            var coupons = new CouponRepository(store);
            var orderRepo = new OrderRepository(store);
            var incoming = new IncomingOrderRepository(store);

            var validator = new OrderValidator(customers, products, coupons);
            var pricer = new OrderPricer(new PricingService(store, customers, coupons, [7]));
            var processor = new OrderProcessorV2(products, pricer, new InvoiceGenerator(), orderRepo);

            List<Order> orders = [];
            int cancelled = 0;

            async Task MarkCancelledAsync(Order order)
            {
                await processor.ReleaseReservedAsync(order, CancellationToken.None);
                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";
                await orderRepo.SaveAsync(order, CancellationToken.None);
                cancelled++;
            }

            async Task ValidateAndSaveAsync(Order order)
            {
                await validator.ValidateOrderAsync(order, ct);
                await orderRepo.SaveAsync(order, ct);
            }

            try
            {
                await DataSeeder.ResetAsync(ct);
                await RaceConditionDemo.RunAsync(ct);
                await DataSeeder.ResetAsync(ct);

                orders = await incoming.GetAllAsync(ct);

                Console.WriteLine("\n--- Pricing comparison: order #1 ---");
                var watch = Stopwatch.StartNew();
                await pricer.PriceOrderSequentialAsync(orders[0], ct);
                Console.WriteLine($"Sequential: {watch.Elapsed.TotalSeconds:F1} s");

                watch.Restart();
                await pricer.PriceOrderAsync(orders[0], ct);
                Console.WriteLine($"Concurrent: {watch.Elapsed.TotalSeconds:F1} s");

                Console.WriteLine("\n--- Validating all orders concurrently ---");
                var validations = new List<Task>();
                foreach (Order order in orders)
                    validations.Add(ValidateAndSaveAsync(order));

                for (int i = 0; i < validations.Count; i++)
                {
                    try { await validations[i]; }
                    catch (OperationCanceledException) { await MarkCancelledAsync(orders[i]); }
                }
                ct.ThrowIfCancellationRequested();

                Console.WriteLine("\n--- Processing validated orders concurrently ---");
                List<Order> validated = orders.Where(o => o.Status == OrderStatus.Validated).ToList();
                var processing = Stopwatch.StartNew();

                var tasks = new List<Task<bool>>();
                foreach (Order order in validated)
                    tasks.Add(processor.ProcessValidatedOrderAsync(order, ct));

                Console.WriteLine($"task[0] status right after creation: {tasks[0].Status}");

                await Task.Delay(1000, ct);
                for (int i = 0; i < tasks.Count; i++)
                    Console.WriteLine($"Order #{validated[i].Id}: {tasks[i].Status}");

                for (int i = 0; i < tasks.Count; i++)
                {
                    Order order = validated[i];
                    try
                    {
                        await tasks[i];
                    }
                    catch (OperationCanceledException)
                    {
                        await MarkCancelledAsync(order);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Order #{order.Id} faulted: task status {tasks[i].Status}, {ex.Message}");

                        await processor.ReleaseReservedAsync(order, CancellationToken.None);
                        order.Status = OrderStatus.Failed;
                        order.FailureReason = ex.Message;
                        await orderRepo.SaveAsync(order, CancellationToken.None);
                    }
                }
                ct.ThrowIfCancellationRequested();

                Console.WriteLine();
                for (int i = 0; i < tasks.Count; i++)
                    Console.WriteLine($"Order #{validated[i].Id}: task {tasks[i].Status}");
                Console.WriteLine($"Processing time: {processing.Elapsed.TotalSeconds:F1} s");
                Console.WriteLine($"ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");

                List<Order> all = await orderRepo.GetAllAsync(ct);
                Console.WriteLine();
                Console.WriteLine($"orders.json contains {all.Count} records.");
                foreach (var group in all.GroupBy(o => o.Status).OrderBy(g => g.Key))
                    Console.WriteLine($" {group.Key}: {group.Count()}");

                var reportGenerator = new ReportGenerator(orderRepo, products);
                Task<Report> reportTask = reportGenerator.GenerateAsync(ct);
                Console.WriteLine("\nGenerating report...");
                Report report = await reportTask;

                Console.WriteLine($"Total orders: {report.TotalOrders}");
                Console.WriteLine($"Completed: {report.CompletedCount}, Rejected: {report.RejectedCount}, " +
                                  $"Failed: {report.FailedCount}, PaymentFailed: {report.PaymentFailedCount}");
                Console.WriteLine($"Total revenue: {report.TotalRevenue}");
                Console.WriteLine($"Average order value: {report.AverageOrderValue}");
                Console.WriteLine($"Top selling: {report.TopSellingProduct} ({report.TopSellingQuantity} units)");
            }
            catch (OperationCanceledException)
            {
                int finished = orders.Count(o => o.Status != OrderStatus.Pending);
                Console.WriteLine($"Run cancelled — {finished} orders finished, {cancelled} cancelled.");
            }
        }
    }
}
