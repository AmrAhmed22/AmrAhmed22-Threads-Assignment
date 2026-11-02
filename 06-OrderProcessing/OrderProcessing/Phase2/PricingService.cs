using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{
    public sealed class PricingService
    {
        readonly JsonFileStore _store;
        readonly CustomerRepository _customers;
        readonly CouponRepository _coupons;
        readonly HashSet<int> _failShippingForOrderIds;

        public PricingService(JsonFileStore store, CustomerRepository customers,
            CouponRepository coupons, IEnumerable<int> failShippingForOrderIds)
        {
            _store = store;
            _customers = customers;
            _coupons = coupons;
            _failShippingForOrderIds = failShippingForOrderIds.ToHashSet();
        }

        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"PricingService -> Order #{order.Id}: executing query for product prices...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Product> products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);
            foreach (OrderItem item in order.Items)
                item.UnitPrice = products.First(p => p.Id == item.ProductId).Price;

            decimal subTotal = PricingRules.SubTotal(order);
            Log.Info($"PricingService -> Order #{order.Id}: SubTotal = {subTotal}");
            return subTotal;
        }

        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: discount API, DNS/handshake...");
            await Task.Delay(150, ct);

            Log.Info($"PricingService -> Order #{order.Id}: discount API, sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PricingService -> Order #{order.Id}: discount API, waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            Customer customer = await _customers.GetByIdAsync(order.CustomerId, ct)
                ?? throw new InvalidOperationException($"Customer #{order.CustomerId} not found");

            int couponPercent = 0;
            if (order.CouponCode is not null)
            {
                Coupon? coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
                couponPercent = coupon?.DiscountPercent ?? 0;
            }

            int percent = PricingRules.DiscountPercent(customer.LoyaltyTier, couponPercent);
            decimal discount = PricingRules.Discount(order.SubTotal, percent);

            Log.Info($"PricingService -> Order #{order.Id}: discount {percent}% = {discount}");
            return discount;
        }

        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            Log.Info($"PricingService -> Order #{order.Id}: shipping API, DNS/handshake...");
            await Task.Delay(150, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                Log.Error($"PricingService -> Order #{order.Id}: shipping provider unavailable");
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            Log.Info($"PricingService -> Order #{order.Id}: shipping API, sending request...");
            await Task.Delay(300, ct);

            Log.Info($"PricingService -> Order #{order.Id}: shipping API, waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            List<Product> products = await _store.ReadAllAsync<Product>(DataFiles.Products, ct);

            int totalQuantity = order.Items.Sum(i => i.Quantity);
            decimal totalWeight = order.Items.Sum(i => products.First(p => p.Id == i.ProductId).WeightKg * i.Quantity);

            decimal shipping = PricingRules.Shipping(order.SubTotal, totalQuantity, totalWeight);
            Log.Info($"PricingService -> Order #{order.Id}: shipping = {shipping}");
            return shipping;

        }
    }
}
