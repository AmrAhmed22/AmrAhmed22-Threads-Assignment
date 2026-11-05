using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{

    public sealed class OrderValidator
    {
        readonly CustomerRepository _customers;
        readonly ProductRepository _products;
        readonly CouponRepository _coupons;

        public OrderValidator(CustomerRepository customers, ProductRepository products, CouponRepository coupons)
        {
            _customers = customers;
            _products = products;
            _coupons = coupons;
        }



        public async Task<bool> ValidateOrderAsync(Order order, CancellationToken ct)
        {
            Customer? customer = await _customers.GetByIdAsync(order.CustomerId, ct);
            if (customer is null)
                return Reject(order, $"Customer #{order.CustomerId} not found");

            if (!customer.IsActive)
                return Reject(order, "Customer is inactive");

            var products = new List<Product>();
            foreach (OrderItem item in order.Items)
            {
                Product? product = await _products.GetByIdAsync(item.ProductId, ct);
                if (product is null)
                    return Reject(order, $"Product #{item.ProductId} not found");

                products.Add(product);
            }

            for (int i = 0; i < order.Items.Count; i++)
            {
                if (products[i].StockQuantity < order.Items[i].Quantity)
                    return Reject(order, $"Insufficient stock for '{products[i].Name}'");
            }

            if (order.CouponCode is not null)
            {
                Coupon? coupon = await _coupons.GetByCodeAsync(order.CouponCode, ct);
                if (coupon is null)
                    return Reject(order, $"Coupon '{order.CouponCode}' does not exist");

                if (coupon.ExpiryDate < DateTime.Today)
                    return Reject(order, $"Coupon '{order.CouponCode}' has expired");
            }

            order.Status = OrderStatus.Validated;
            return true;
        }

        static bool Reject(Order order, string reason)
        {
            order.Status = OrderStatus.Rejected;
            order.FailureReason = reason;
            return false;
        }


    }
}
