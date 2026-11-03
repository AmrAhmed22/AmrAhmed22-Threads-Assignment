using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderProcessing.Phase2
{
    public sealed class ReportGenerator
    {
        readonly OrderRepository _orders;
        readonly ProductRepository _products;

        public ReportGenerator(OrderRepository orders, ProductRepository products)
        {
            _orders = orders;
            _products = products;
        }

        public async Task<Report> GenerateAsync(CancellationToken ct)
        {
            List<Order> orders = await _orders.GetAllAsync(ct);
            List<Order> completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

            var top = completed
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .ThenBy(x => x.ProductId)
                .FirstOrDefault();

            string topName = "";
            if (top is not null)
            {
                Product? product = await _products.GetByIdAsync(top.ProductId, ct);
                topName = product?.Name ?? "";
            }

            decimal revenue = completed.Sum(o => o.TotalAmount);

            var report = new Report
            {
                TotalOrders = orders.Count,
                TotalRevenue = revenue,
                CompletedCount = completed.Count,
                RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
                FailedCount = orders.Count(o => o.Status == OrderStatus.Failed),
                PaymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed),
                TopSellingProduct = topName,
                TopSellingQuantity = top?.Quantity ?? 0,
                AverageOrderValue = completed.Count == 0 ? 0 : PricingRules.Round(revenue / completed.Count),
                GeneratedAt = DateTime.Now
            };

            Directory.CreateDirectory(DataFiles.ReportsFolder);
            string path = Path.Combine(DataFiles.ReportsFolder, $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json");
            string json = JsonSerializer.Serialize(report, JsonOptions.Default);
            await File.WriteAllTextAsync(path, json, ct);

            return report;
        }
    }
}
