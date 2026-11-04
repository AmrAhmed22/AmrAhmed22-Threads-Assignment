using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public static class ShippingDemo
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("\n===== 10.3 Fastest shipping quote =====");

            var store = new JsonFileStore();
            List<Order> orders = await store.ReadAllAsync<Order>(DataFiles.IncomingOrders, ct);

            var service = new ShippingQuoteService([new FastShip(store), new CairoExpress(store), new NileCourier(store)]);

            Log.ResetThreads();
            var watch = Stopwatch.StartNew();

            foreach (Order order in orders.Where(o => o.Id <= 5))
            {
                ShippingQuote quote = await service.GetFastestQuoteAsync(order, ct);
                string cancelled = quote.Cancelled.Count == 0 ? "none" : string.Join(", ", quote.Cancelled);

                Console.WriteLine($"Order #{order.Id} -> {quote.Carrier} | price {quote.Price:F2} | {quote.ElapsedMs} ms | cancelled: {cancelled}");
                Console.WriteLine();
            }

            Console.WriteLine($"Total time: {watch.Elapsed.TotalSeconds:F1} s | distinct threads: {Log.DistinctThreads} | ThreadPool.ThreadCount: {ThreadPool.ThreadCount}");

            Console.WriteLine("\n--- every carrier fails for order #6 ---");
            var failing = new ShippingQuoteService(
                [new FastShip(store, [6]), new CairoExpress(store, [6]), new NileCourier(store, [6])]);

            try
            {
                await failing.GetFastestQuoteAsync(orders.First(o => o.Id == 6), ct);
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Order #6 -> {ex.Message}");
            }
        }
    }
}
