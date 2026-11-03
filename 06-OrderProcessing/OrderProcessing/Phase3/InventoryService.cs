using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class InventoryService
    {
        readonly ProductRepository _products;

        public InventoryService(ProductRepository products) => _products = products;

        public async Task<bool> ReserveStockAsync(Order order, CancellationToken ct)
        {
            var reserved = new List<OrderItem>();
            try
            {
                foreach (OrderItem item in order.Items)
                {
                    bool ok = await _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);
                    if (!ok)
                    {
                        Product? product = await _products.GetByIdAsync(item.ProductId, ct);
                        order.FailureReason = $"Insufficient stock for '{product?.Name}'";

                        await ReleaseItemsAsync(reserved, CancellationToken.None);   // roll back
                        return false;
                    }

                    reserved.Add(item);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                await ReleaseItemsAsync(reserved, CancellationToken.None);           // roll back
                throw;
            }
        }

        public Task ReleaseStockAsync(Order order, CancellationToken ct) =>
            ReleaseItemsAsync(order.Items, ct);

        async Task ReleaseItemsAsync(List<OrderItem> items, CancellationToken ct)
        {
            foreach (OrderItem item in items)
                await _products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
        }
    }
}
