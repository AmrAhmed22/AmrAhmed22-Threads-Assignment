using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase1
{
    public static class Phase1Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            var store = new JsonFileStore();
            var incoming = new IncomingOrderRepository(store);
            var orderRepo = new OrderRepository(store);

            var syncValidator = new SyncOrderValidator(
                new SyncCustomerRepository(store), new SyncProductRepository(store), new SyncCouponRepository(store));
            var validator = new OrderValidator(
                new CustomerRepository(store), new ProductRepository(store), new CouponRepository(store));

            List<Order> orders = [];
            Order? current = null;      
            int finished = 0;

            try
            {
                await DataSeeder.ResetAsync(ct);
                orders = await incoming.GetAllAsync(ct);

                Console.WriteLine("\n--- Sync run ---");
                var watch = Stopwatch.StartNew();
                foreach (Order order in orders)
                {
                    ct.ThrowIfCancellationRequested();     
                    syncValidator.ValidateOrder(order);
                }
                double syncSeconds = watch.Elapsed.TotalSeconds;

                await DataSeeder.ResetAsync(ct);
                orders = await incoming.GetAllAsync(ct);

                Console.WriteLine("\n--- Async run ---");
                watch.Restart();
                foreach (Order order in orders)
                {
                    current = order;
                    await validator.ValidateOrderAsync(order, ct);
                    await orderRepo.SaveAsync(order, ct);
                    finished++;
                }
                current = null;
                double asyncSeconds = watch.Elapsed.TotalSeconds;

                List<Order> all = await orderRepo.GetAllAsync(ct);
                List<Order> validated = await orderRepo.GetByStatusAsync(OrderStatus.Validated, ct);
                List<Order> rejected = await orderRepo.GetByStatusAsync(OrderStatus.Rejected, ct);

                Console.WriteLine();
                Console.WriteLine("===== Validation Summary =====");
                Console.WriteLine($"Total Orders: {all.Count}");
                Console.WriteLine($"Validated: {validated.Count}");
                Console.WriteLine($"Rejected: {rejected.Count}");
                foreach (Order o in rejected.OrderBy(o => o.Id))
                    Console.WriteLine($" - Order #{o.Id}: {o.FailureReason}");
                Console.WriteLine($"Sync Time: {syncSeconds:F1} seconds");
                Console.WriteLine($"Async Time: {asyncSeconds:F1} seconds");
                Console.WriteLine($"orders.json now contains {all.Count} records.");
            }
            catch (OperationCanceledException)
            {
                if (current is not null)
                {
                    current.Status = OrderStatus.Pending;
                    current.FailureReason = "Cancelled";
                    await orderRepo.SaveAsync(current, CancellationToken.None);
                }

                Console.WriteLine($"Run cancelled — {finished} orders finished, {orders.Count - finished} cancelled.");
            }
        }
    }
}
