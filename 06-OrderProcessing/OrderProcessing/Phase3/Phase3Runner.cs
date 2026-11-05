using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public static class Phase3Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            List<Order> orders = [];

            try
            {
                await ComparisonRunner.RunAsync(ct);
                await FireAndForgetRunner.RunAsync(ct);
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

                orders = await new IncomingOrderRepository(store).GetAllAsync(ct);

                var tasks = new List<Task<bool>>();
                foreach (Order order in orders)
                    tasks.Add(processor.ProcessOrderAsync(order, ct));

                foreach (Task<bool> task in tasks)
                    await task;

                if (ct.IsCancellationRequested)
                {
                    PrintCancelled(orders);
                    return;
                }

                List<Order> all = await orderRepo.GetAllAsync(ct);

                Console.WriteLine();
                Console.WriteLine($"{"Order",-6}{"Status",-15}{"Total",10}  Reason");
                foreach (Order o in all.OrderBy(o => o.Id))
                    Console.WriteLine($"#{o.Id,-5}{o.Status,-15}{o.TotalAmount,10:F2}  {o.FailureReason}");

                var payments = await store.ReadAllAsync<PaymentTransaction>(DataFiles.Payments, ct);
                var emails = await store.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct);
                var retries = await store.ReadAllAsync<EmailMessage>(DataFiles.EmailRetry, ct);

                Console.WriteLine();
                Console.WriteLine($"orders.json: {all.Count} records");
                foreach (var group in all.GroupBy(o => o.Status).OrderBy(g => g.Key))
                    Console.WriteLine($"  {group.Key}: {group.Count()}");
                Console.WriteLine($"payments.json: {payments.Count} records " +
                    $"({payments.Count(p => p.Status == PaymentStatus.Captured)} Captured, " +
                    $"{payments.Count(p => p.Status == PaymentStatus.Declined)} Declined, " +
                    $"{payments.Count(p => p.Status == PaymentStatus.Refunded)} Refunded)");
                Console.WriteLine($"emails.json: {emails.Count} records");
                Console.WriteLine($"email_retry.json: {retries.Count} records");
            }
            catch (OperationCanceledException)
            {
                PrintCancelled(orders);
            }
        }

        static void PrintCancelled(List<Order> orders)
        {
            int cancelled = orders.Count(o => o.FailureReason == "Cancelled");
            Console.WriteLine($"Run cancelled — {orders.Count - cancelled} orders finished, {cancelled} cancelled.");
        }
    }
}
