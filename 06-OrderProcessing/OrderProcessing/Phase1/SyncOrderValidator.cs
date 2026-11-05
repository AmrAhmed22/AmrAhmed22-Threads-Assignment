using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase1
{
    public sealed class SyncOrderValidator
    {
        readonly SyncCustomerRepository _customers;
        readonly SyncProductRepository _products;
        readonly SyncCouponRepository _coupons;

        public SyncOrderValidator(SyncCustomerRepository customers, SyncProductRepository products, SyncCouponRepository coupons)
        {
            _customers = customers;
            _products = products;
            _coupons = coupons;
        }

        public bool ValidateOrder(Order order)
        {
            Customer? customer = _customers.GetById(order.CustomerId);
            if (customer is null)
                return Reject(order, $"Customer #{order.CustomerId} not found");

            if (!customer.IsActive)
                return Reject(order, "Customer is inactive");

            var products = new List<Product>();
            foreach (OrderItem item in order.Items)
            {
                Product? product = _products.GetById(item.ProductId);
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
                Coupon? coupon = _coupons.GetByCode(order.CouponCode);
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
