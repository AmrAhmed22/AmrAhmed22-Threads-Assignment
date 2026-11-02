using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{

    public static class PricingRules
    {
        // every money should enter her first
        public static decimal Round(decimal x) =>
            Math.Round(x, 2, MidpointRounding.AwayFromZero);

        public static decimal SubTotal(Order order) =>
            Round(order.Items.Sum(i => i.UnitPrice * i.Quantity));

        public static int DiscountPercent(LoyaltyTier tier, int couponPercent)
        {
            int loyalty = tier switch
            {
                LoyaltyTier.Silver => 5,
                LoyaltyTier.Gold => 10,
                _ => 0
            };

            return Math.Min(30, loyalty + couponPercent); //capped at 30
        }

        public static decimal Discount(decimal subTotal, int discountPercent) =>
            Round(subTotal * discountPercent / 100m);

        public static decimal Shipping(decimal subTotal, int totalQuantity, decimal totalWeightKg)
        {
            if (subTotal >= 1000m)
                return 0m;

            return Round(5.00m + 1.50m * totalQuantity + 2.00m * totalWeightKg);
        }

        public static decimal Total(decimal subTotal, decimal discount, decimal shipping) =>
            Round(subTotal - discount + shipping);
    }
}


// Finally , an easy class (: 