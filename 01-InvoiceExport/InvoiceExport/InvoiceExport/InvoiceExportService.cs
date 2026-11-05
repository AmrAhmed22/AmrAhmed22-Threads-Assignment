using System.Diagnostics;

namespace InvoiceExport;

public sealed class InvoiceExportService
{
    readonly OrderDatabase _db;
    readonly CustomerFileStore _customers;
    readonly InvoiceFileWriter _invoices;

    public InvoiceExportService(OrderDatabase db, CustomerFileStore customers, InvoiceFileWriter invoices)
    {
        _db = db;
        _customers = customers;
        _invoices = invoices;
    }


    public async Task ExportAllPendingAsync(CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        List<Order> orders = await _db.GetPendingOrdersAsync(ct);

        var tasks = new List<Task<string>>();

        //concurrent
        foreach (Order order in orders)
            tasks.Add(ExportOrderAsync(order.Id, ct));

        int failed = 0;
        foreach (Task<string> task in tasks)
        {
            try
            {
                Console.WriteLine($"Wrote {await task}");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                Console.WriteLine($"Export failed: {ex.Message}");
            }
        }

        Console.WriteLine($"Export finished in {watch.Elapsed.TotalSeconds:F1} s");
    }

    async Task<string> ExportOrderAsync(int orderId, CancellationToken ct)
    {
        Order fresh = await _db.GetOrderByIdAsync(orderId, ct)
            ?? throw new InvalidOperationException($"Order {orderId} missing");

        CustomerProfile customer = await _customers.ReadProfileAsync(fresh.CustomerId, ct);
        return await _invoices.WriteInvoiceAsync(fresh, customer, ct);
    }
}
