using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{

    public static class Phase4Runner
    {
        public static async Task RunAsync(CancellationToken ct)
        {
            await LiveCheckout.RunAsync(ct);

            if (ct.IsCancellationRequested)
                return;

            try
            {
                await DashboardDemo.RunAsync(ct);
                await ShippingDemo.RunAsync(ct);
                FraudDemo.Run(ct);
                await EmailResendDemo.RunAsync(ct);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Run cancelled.");
            }
        }
    }
}
