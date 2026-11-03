using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{

    public sealed class OrderProcessorV2
    {
        readonly ProductRepository _products;
        readonly OrderPricer _pricer;
        readonly InvoiceGenerator _invoices;
        readonly OrderRepository _orders;

        readonly ConcurrentDictionary<int, List<OrderItem>> _reserved = new();

        public OrderProcessorV2(ProductRepository products, OrderPricer pricer,
            InvoiceGenerator invoices, OrderRepository orders)
        {
            _products = products;
            _pricer = pricer;
            _invoices = invoices;
            _orders = orders;
        }

        public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
        {
            List<OrderItem> reserved = _reserved.GetOrAdd(order.Id, _ => new List<OrderItem>());

            foreach (OrderItem item in order.Items)
            {
                bool ok = await _products.ReserveStockAsync(item.ProductId, item.Quantity, ct);
                if (!ok)
                {
                    await ReleaseReservedAsync(order, ct);

                    Product? product = await _products.GetByIdAsync(item.ProductId, ct);
                    order.Status = OrderStatus.Rejected;
                    order.FailureReason = $"Insufficient stock for '{product?.Name ?? $"#{item.ProductId}"}'";
                    await _orders.SaveAsync(order, ct);
                    return false;
                }

                reserved.Add(item);
            }

            await _pricer.PriceOrderAsync(order, ct);
            await _invoices.GenerateAsync(order, ct);

            order.Status = OrderStatus.Completed;
            await _orders.SaveAsync(order, ct);
            return true;
        }

        public async Task ReleaseReservedAsync(Order order, CancellationToken ct)
        {
            if (!_reserved.TryRemove(order.Id, out List<OrderItem>? items))
                return;

            foreach (OrderItem item in items)
                await _products.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
        }
    }
}
