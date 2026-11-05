using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase2
{

    public sealed class InvoiceGenerator
    {
        public async Task GenerateAsync(Order order, CancellationToken ct)
        {
            Log.Info($"InvoiceGenerator -> Order #{order.Id}: rendering invoice PDF...");

            await Task.Run(() =>
            {
                double result = 0;
                for (int i = 1; i <= 20_000_000; i++)
                    result += Math.Sqrt(i) / (i + 1);
            }, ct);


            string text = "SWIFT INVOICE\n" +
                          $"Order: {order.Id}\n" +
                          $"Customer: {order.CustomerId}\n" +
                          $"Subtotal: {order.SubTotal}\n" +
                          $"Discount: -{order.Discount}\n" +
                          $"Shipping: +{order.ShippingCost}\n" +
                          $"TOTAL: {order.TotalAmount}";

            Directory.CreateDirectory(DataFiles.InvoicesFolder);
            string path = Path.Combine(DataFiles.InvoicesFolder, $"invoice_{order.Id}.txt");
            await File.WriteAllTextAsync(path, text, ct);

            Log.Info($"InvoiceGenerator -> Order #{order.Id}: invoice written");
        }
    }

}
