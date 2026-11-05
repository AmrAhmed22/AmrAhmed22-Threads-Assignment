using OrderProcessing.Phase2;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public abstract class ShippingCarrierBase : IShippingCarrier
    {
        readonly JsonFileStore _store;
        readonly HashSet<int> _failForOrderIds;
        readonly int _minMs;
        readonly int _maxMs;
        readonly decimal _factor;

        public string Name { get; }

        protected ShippingCarrierBase(string name, int minMs, int maxMs, decimal factor,
            JsonFileStore store, IEnumerable<int>? failForOrderIds)
        {
            Name = name;
            _minMs = minMs;
            _maxMs = maxMs;
            _factor = factor;
            _store = store;
            _failForOrderIds = (failForOrderIds ?? Array.Empty<int>()).ToHashSet();
        }

        protected virtual bool ShouldFail(Order order) => _failForOrderIds.Contains(order.Id);

        public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        {
            try
            {
                Log.Info($"[{Name}] order #{order.Id}: handshake...");
                await Task.Delay(150, ct);

                if (ShouldFail(order))
                {
                    Log.Error($"[{Name}] order #{order.Id}: unavailable");
                    throw new ShippingProviderUnavailableException($"{Name} unavailable");
                }

                Log.Info($"[{Name}] order #{order.Id}: waiting for response...");
                await Task.Delay(Random.Shared.Next(_minMs, _maxMs + 1), ct);

                List<Product> products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

                decimal subTotal = order.Items.Sum(i => products.First(p => p.Id == i.ProductId).Price * i.Quantity);
                int quantity = order.Items.Sum(i => i.Quantity);
                decimal weight = order.Items.Sum(i => products.First(p => p.Id == i.ProductId).WeightKg * i.Quantity);

                decimal price = PricingRules.Round(PricingRules.Shipping(subTotal, quantity, weight) * _factor);
                Log.Info($"[{Name}] order #{order.Id}: quote {price}");
                return price;
            }
            catch (OperationCanceledException)
            {
                Log.Info($"[{Name}] cancelled");
                throw;
            }
        }
    }
}
