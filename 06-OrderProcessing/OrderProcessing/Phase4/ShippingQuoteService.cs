using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderProcessing.Phase4
{
    public sealed class ShippingQuoteService
    {
        readonly List<IShippingCarrier> _carriers;

        public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers) => _carriers = carriers.ToList();

        public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct)
        {
            var watch = Stopwatch.StartNew();

           
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMilliseconds(1200));

            var running = new Dictionary<Task<decimal>, IShippingCarrier>();
            foreach (IShippingCarrier carrier in _carriers)
                running[carrier.GetQuoteAsync(order, cts.Token)] = carrier;

            Task<decimal>? winnerTask = null;
            long elapsedMs = 0;

            await foreach (Task<decimal> task in Task.WhenEach(running.Keys))
            {
                if (task.IsCompletedSuccessfully)
                {
                    winnerTask = task;
                    elapsedMs = watch.ElapsedMilliseconds;
                    break;
                }

                if (task.IsFaulted)
                    _ = task.Exception; 
            }

            if (winnerTask is not null)
            {
                cts.Cancel();

                var quote = new ShippingQuote
                {
                    Carrier = running[winnerTask].Name,
                    Price = await winnerTask,
                    ElapsedMs = elapsedMs
                };

                foreach (var (task, carrier) in running)
                {
                    if (task == winnerTask)
                        continue;

                    try { await task; }
                    catch (OperationCanceledException) { quote.Cancelled.Add(carrier.Name); }
                    catch (ShippingProviderUnavailableException) { }  
                }

                return quote;
            }

        
            ct.ThrowIfCancellationRequested();   

            if (running.Keys.All(t => t.IsFaulted))
                throw new ShippingProviderUnavailableException("All carriers failed");

            return new ShippingQuote
            {
                Carrier = "Flat rate",
                Price = 25.00m,
                ElapsedMs = watch.ElapsedMilliseconds,
                Cancelled = running.Where(kv => kv.Key.IsCanceled).Select(kv => kv.Value.Name).ToList()
            };
        }
    }
}