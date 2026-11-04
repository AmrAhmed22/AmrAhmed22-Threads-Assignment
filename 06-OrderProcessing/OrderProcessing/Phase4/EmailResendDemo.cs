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
    public static class EmailResendDemo
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== 10.4 B: Email re-send =====");

            var store = new JsonFileStore();
            IEmailService emailService = new EmailService(store);  
            var customer = new Customer { Id = 1, Name = "Ahmed Ali", Email = "ahmed.ali@example.com" };

            List<Order> orders = Enumerable.Range(1001, 30)
                .Select(id => new Order { Id = id, CustomerId = 1, TotalAmount = 100m })
                .ToList();

            int running = 0;
            int highest = 0;
            object sync = new();

            Log.Quiet = true;       
            Log.ResetThreads();
            var watch = Stopwatch.StartNew();
            try
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct };

                await Parallel.ForEachAsync(orders, options, async (order, token) =>
                {
                    int now = Interlocked.Increment(ref running);
                    lock (sync)
                    {
                        if (now > highest)
                            highest = now;
                    }

                    try
                    {
                        await emailService.SendConfirmationAsync(order, customer, token);  
                    }
                    finally
                    {
                        Interlocked.Decrement(ref running);
                    }
                });
            }
            finally
            {
                Log.Quiet = false;
            }
            double seconds = watch.Elapsed.TotalSeconds;
            int threads = Log.DistinctThreads;

            List<EmailMessage> emails = await store.ReadAllAsync<EmailMessage>(DataFiles.Emails, ct);
            List<int> resent = emails.Where(e => e.OrderId >= 1001 && e.OrderId <= 1030).Select(e => e.OrderId).ToList();

            Console.WriteLine($"Highest number of sends at the same moment: {highest}");
            Console.WriteLine($"Emails in emails.json for orders 1001-1030: {resent.Count} ({resent.Distinct().Count()} distinct)");
            Console.WriteLine($"Total time: {seconds:F1} s | distinct threads: {threads} | ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
        }
    }
}
