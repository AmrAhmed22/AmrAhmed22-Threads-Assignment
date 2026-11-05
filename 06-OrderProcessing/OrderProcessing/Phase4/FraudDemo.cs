using OrderProcessing.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public static class FraudDemo
    {
        static int Score(int orderId)
        {
            long h = orderId;
            for (int i = 0; i < 100_000; i++)
                h = (h * 31 + i) % 1_000_000_007;
            return (int)(h % 100);
        }

        public static void Run(CancellationToken ct)
        {
            Console.WriteLine("\n===== 10.4 A: Fraud scoring =====");

            List<Order> orders = Enumerable.Range(1, 10_000).Select(id => new Order { Id = id }).ToList();

            var watch = Stopwatch.StartNew();
            var sequentialFlagged = new List<int>();
            foreach (Order order in orders)
            {
                ct.ThrowIfCancellationRequested();
                if (Score(order.Id) >= 90)
                    sequentialFlagged.Add(order.Id);
            }
            double sequentialSeconds = watch.Elapsed.TotalSeconds;

            var parallelFlagged = new ConcurrentBag<int>();           
            var threads = new ConcurrentDictionary<int, byte>();       
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct                               
            };

            watch.Restart();
            Parallel.ForEach(orders, options, order =>
            {
                threads[Environment.CurrentManagedThreadId] = 0;
                if (Score(order.Id) >= 90)
                    parallelFlagged.Add(order.Id);
            });
            double parallelSeconds = watch.Elapsed.TotalSeconds;

            List<int> parallelSorted = parallelFlagged.OrderBy(id => id).ToList();
            bool same = sequentialFlagged.SequenceEqual(parallelSorted);

            Console.WriteLine($"Flagged orders: {parallelSorted.Count} (first five: {string.Join(", ", parallelSorted.Take(5))})");
            Console.WriteLine($"Same ids in both runs: {same}");
            Console.WriteLine($"Sequential: {sequentialSeconds:F1} s");
            Console.WriteLine($"Parallel:   {parallelSeconds:F1} s");
            Console.WriteLine($"Speed-up:   {sequentialSeconds / parallelSeconds:F1}x");
            Console.WriteLine($"Distinct threads used: {threads.Count} | Environment.ProcessorCount: {Environment.ProcessorCount} | ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");
        }
    }
}
