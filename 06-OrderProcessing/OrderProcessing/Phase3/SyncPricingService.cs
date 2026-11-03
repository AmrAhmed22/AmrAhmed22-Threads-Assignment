using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class SyncPricingService
    {
        readonly JsonFileStore _store;
        readonly SyncCustomerRepository _customers;
        readonly SyncCouponRepository _coupons;

        public SyncPricingService(JsonFileStore store, SyncCustomerRepository customers, SyncCouponRepository coupons)
        {
            _store = store;
            _customers = customers;
            _coupons = coupons;
        }

        public decimal CalculateTotal(Order order)
        {
            Log.Info($"SyncPricingService -> Order #{order.Id}: opening connection...");
            Thread.Sleep(200);
            Log.Info($"SyncPricingService -> Order #{order.Id}: executing query for product prices...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            List<Product> products = _store.ReadAll<Product>(DataFiles.Products);
            foreach (OrderItem item in order.Items)
                item.UnitPrice = products.First(p => p.Id == item.ProductId).Price;
            order.SubTotal = PricingRules.SubTotal(order);

            // discount API
            Log.Info($"SyncPricingService -> Order #{order.Id}: discount API...");
            Thread.Sleep(150);
            Thread.Sleep(300);
            Thread.Sleep(Random.Shared.Next(500, 1501));

            Customer customer = _customers.GetById(order.CustomerId)!;
            int couponPercent = 0;
            if (order.CouponCode is not null)
                couponPercent = _coupons.GetByCode(order.CouponCode)?.DiscountPercent ?? 0;

            int percent = PricingRules.DiscountPercent(customer.LoyaltyTier, couponPercent);
            order.Discount = PricingRules.Discount(order.SubTotal, percent);

            // shipping API
            Log.Info($"SyncPricingService -> Order #{order.Id}: shipping API...");
            Thread.Sleep(150);
            Thread.Sleep(300);
            Thread.Sleep(Random.Shared.Next(500, 1501));

            int totalQuantity = order.Items.Sum(i => i.Quantity);
            decimal totalWeight = order.Items.Sum(i => products.First(p => p.Id == i.ProductId).WeightKg * i.Quantity);
            order.ShippingCost = PricingRules.Shipping(order.SubTotal, totalQuantity, totalWeight);

            order.TotalAmount = PricingRules.Total(order.SubTotal, order.Discount, order.ShippingCost);
            return order.TotalAmount;
        }
    }
}
