using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public static class DashboardDemo
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== 10.2 Customer dashboard =====");

            var service = new CustomerDashboardService(new JsonFileStore(), [99]);

            Log.ResetThreads();
            var watch = Stopwatch.StartNew();
            CustomerDashboard d = await service.LoadAsync(3, ct);
            double seconds = watch.Elapsed.TotalSeconds;

            Console.WriteLine();
            Console.WriteLine($"Customer #{d.Customer.Id} {d.Customer.Name} — " +
                $"{d.Orders.Count} orders ({string.Join(", ", d.Orders.Select(o => $"#{o.Id} {o.Status}"))}), " +
                $"{d.Payments.Count} payments ({string.Join(", ", d.Payments.Select(p => $"#{p.OrderId} {p.Status}"))}), " +
                $"{d.Emails.Count} email ({string.Join(", ", d.Emails.Select(e => $"#{e.OrderId}"))})");
            Console.WriteLine($"Total time: {seconds:F1} s | distinct threads: {Log.DistinctThreads} | ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");

            Console.WriteLine();
            watch.Restart();
            try
            {
                await service.LoadAsync(99, ct);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine($"Customer #99 failed with {ex.InnerExceptions.Count} errors:");
                foreach (Exception inner in ex.InnerExceptions)
                    Console.WriteLine($"  - {inner.Message}");
            }
            Console.WriteLine($"Total time: {watch.Elapsed.TotalSeconds:F1} s");
        }
    }
}
