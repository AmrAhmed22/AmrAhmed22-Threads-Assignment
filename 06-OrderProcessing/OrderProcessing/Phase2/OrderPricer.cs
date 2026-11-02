using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{

    public sealed class OrderPricer
    {
        readonly PricingService _pricing;

        public OrderPricer(PricingService pricing) => _pricing = pricing;

        public async Task PriceOrderAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await _pricing.CalculateSubTotalAsync(order, ct);


            //runs at same time
            Task<decimal> discountTask = _pricing.GetDiscountAsync(order, ct);
            Task<decimal> shippingTask = _pricing.CalculateShippingCostAsync(order, ct);

            order.Discount = await discountTask;
            order.ShippingCost = await shippingTask;

            order.TotalAmount = PricingRules.Total(order.SubTotal, order.Discount, order.ShippingCost);
        }

        public async Task PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            order.SubTotal = await _pricing.CalculateSubTotalAsync(order, ct);
            order.Discount = await _pricing.GetDiscountAsync(order, ct);
            order.ShippingCost = await _pricing.CalculateShippingCostAsync(order, ct);

            order.TotalAmount = PricingRules.Total(order.SubTotal, order.Discount, order.ShippingCost);
        }
    }
}
