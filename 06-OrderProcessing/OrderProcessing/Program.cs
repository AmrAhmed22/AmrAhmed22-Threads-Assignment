using OrderProcessing.Phase2;
using OrderProcessing.Shared;

using var run = new RunCancellation();
await Phase2Runner.RunAsync(run.Token);