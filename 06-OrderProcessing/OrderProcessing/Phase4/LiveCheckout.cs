using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public static class LiveCheckout
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== 10.1 Live checkout progress =====");

            await DataSeeder.ResetAsync(ct);

            var store = new JsonFileStore();
            var customers = new CustomerRepository(store);
            var products = new ProductRepository(store);
            var coupons = new CouponRepository(store);
            var orderRepo = new OrderRepository(store, [13]);

            var processor = new OrderProcessor(
                new CustomerService(customers),
                new InventoryService(products),
                new PricingService(store, customers, coupons, []),
                new PaymentGateway(store, [5], [8]),
                orderRepo,
                new EmailService(store, new Dictionary<int, int> { [3] = 2, [17] = int.MaxValue }),
                store);

            List<Order> orders = await new IncomingOrderRepository(store).GetAllAsync(ct);

            Log.Quiet = true;   
            try
            {
                var watch = Stopwatch.StartNew();

                var running = new Dictionary<Task<bool>, Order>();
                foreach (Order order in orders)
                    running[processor.ProcessOrderAsync(order, ct)] = order;

                int done = 0;
                await foreach (Task<bool> task in Task.WhenEach(running.Keys))
                {
                    Order order = running[task];
                    done++;

                    string result;
                    if (task.IsFaulted)
                        result = $"Faulted ({task.Exception!.InnerException!.Message})";
                    else if (order.Status == OrderStatus.Completed)
                        result = "Completed";
                    else
                        result = $"{order.Status} ({order.FailureReason})";

                    Console.WriteLine($"[{done,2}/{orders.Count}] Order #{order.Id} -> {result} after {watch.Elapsed.TotalSeconds:F1} s");
                }

                List<Order> all = await orderRepo.GetAllAsync(ct);
                Console.WriteLine();
                Console.WriteLine($"orders.json: {all.Count} records");
                foreach (var group in all.GroupBy(o => o.Status).OrderBy(g => g.Key))
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Log.Quiet = false;
            }

            if (ct.IsCancellationRequested)
            {
                int cancelled = orders.Count(o => o.FailureReason == "Cancelled");
                Console.WriteLine($"Run cancelled — {orders.Count - cancelled} orders finished, {cancelled} cancelled.");
            }
        }
    }
}
