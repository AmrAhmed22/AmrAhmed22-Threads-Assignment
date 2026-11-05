using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class SyncInventoryService
    {
        readonly SyncProductRepository _products;

        public SyncInventoryService(SyncProductRepository products) => _products = products;

        public bool ReserveStock(Order order)
        {
            var reserved = new List<OrderItem>();
            foreach (OrderItem item in order.Items)
            {
                if (!_products.ReserveStock(item.ProductId, item.Quantity))
                {
                    Product? product = _products.GetById(item.ProductId);
                    order.FailureReason = $"Insufficient stock for '{product?.Name}'";
                    ReleaseItems(reserved);
                    return false;
                }

                reserved.Add(item);
            }

            return true;
        }

        public void ReleaseStock(Order order) => ReleaseItems(order.Items);

        void ReleaseItems(List<OrderItem> items)
        {
            foreach (OrderItem item in items)
                _products.ReleaseStock(item.ProductId, item.Quantity);
        }
    }
}
