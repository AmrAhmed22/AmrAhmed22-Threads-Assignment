using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{

    public static class FireAndForgetRunner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== Fire-and-forget investigation =====");

            var store = new JsonFileStore();
            await DataSeeder.ResetAsync(ct);

            List<Order> orders = await new IncomingOrderRepository(store).GetAllAsync(ct);
            List<Customer> customers = await store.ReadAllAsync<Customer>(DataFiles.Customers, ct);

            Order order1 = orders[0];
            Order order2 = orders[1];
            Order order3 = orders[2];
            Customer Customer(Order o) => customers.First(c => c.Id == o.CustomerId);

            async Task<int> EmailCountAsync(int orderId) =>
                (await store.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct)).Count(e => e.OrderId == orderId);

            // ---- BEFORE: broken method, the email works ----
            Console.WriteLine("\n--- BROKEN: the email is not awaited ---");
            var broken = new FireAndForgetDemo(new OrderRepository(store), new EmailService(store));
            await broken.ProcessOrderAsync(order1, Customer(order1));
            Console.WriteLine($"Emails saved for order #1 right after the method returned: {await EmailCountAsync(1)}");
            await Task.Delay(1500, ct);
            Console.WriteLine($"Emails saved for order #1, 1.5 s later: {await EmailCountAsync(1)}   (it was still sending)");

            // ---- BEFORE: broken method, the email throws ----
            Console.WriteLine("\n--- BROKEN: the email throws ---");
            void OnUnobserved(object? s, UnobservedTaskExceptionEventArgs e)
            {
                Console.WriteLine($"UnobservedTaskException (found only by the garbage collector): {e.Exception.InnerException?.Message}");
                e.SetObserved();
            }
            TaskScheduler.UnobservedTaskException += OnUnobserved;

            var brokenFailing = new FireAndForgetDemo(
                new OrderRepository(store), new EmailService(store, new Dictionary<int, int> { [2] = 1 }));
            await brokenFailing.ProcessOrderAsync(order2, Customer(order2));
            Console.WriteLine("The caller saw no error at all.");
            await Task.Delay(1500, ct);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            TaskScheduler.UnobservedTaskException -= OnUnobserved;

            // ---- AFTER: fixed method, the email throws ----
            Console.WriteLine("\n--- FIXED: the email is awaited, and it throws ---");
            var fixedFailing = new FireAndForgetDemo(
                new OrderRepository(store), new EmailService(store, new Dictionary<int, int> { [2] = 1 }));
            try
            {
                await fixedFailing.ProcessOrderFixedAsync(order2, Customer(order2), ct);
            }
            catch (SmtpUnavailableException ex)
            {
                Console.WriteLine($"The caller sees the error and can handle it: {ex.Message}");
            }

            // ---- AFTER: fixed method, the email works ----
            Console.WriteLine("\n--- FIXED: the email is awaited ---");
            var fixedOk = new FireAndForgetDemo(new OrderRepository(store), new EmailService(store));
            await fixedOk.ProcessOrderFixedAsync(order3, Customer(order3), ct);
            Console.WriteLine($"Emails saved for order #3 right after the method returned: {await EmailCountAsync(3)}");
        }
    }
}
