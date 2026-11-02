using OrderProcessing.Phase1;
using OrderProcessing.Shared;

using var run = new RunCancellation();     
await Phase1Runner.RunAsync(run.Token);