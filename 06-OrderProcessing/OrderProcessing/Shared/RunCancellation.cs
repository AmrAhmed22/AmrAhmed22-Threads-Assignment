using OrderProcessing.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared
{
    public sealed class RunCancellation : IDisposable
    {
        readonly CancellationTokenSource _source = new();
        int _requested;   

        public CancellationToken Token => _source.Token;

        public RunCancellation(int? cancelAfterMilliseconds = null)
        {
            if (cancelAfterMilliseconds is < 0)
                throw new ArgumentOutOfRangeException(nameof(cancelAfterMilliseconds));

            Console.CancelKeyPress += OnCancelKeyPress;

            if (cancelAfterMilliseconds is { } timeout)
                _source.CancelAfter(timeout);   
        }

        void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;    
            RequestCancellation();
        }

        public void RequestCancellation()
        {
            if (Interlocked.Exchange(ref _requested, 1) == 1)
                return;

            Log.Info("Cancellation requested.");
            _source.Cancel();
        }

        public void Dispose()
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            _source.Dispose();
        }
    }

}