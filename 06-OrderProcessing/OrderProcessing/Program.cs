using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase4;
using OrderProcessing.Shared;

string mode = "";         
int? cancelAfter = null;

if (mode == "")
{
    Console.WriteLine("1) Phase 1 - async data access");
    Console.WriteLine("2) Phase 2 - concurrent processing");
    Console.WriteLine("3) Phase 3 - full checkout");
    Console.WriteLine("4) Phase 4 - scale-up");
    Console.WriteLine("5) All phases");
    Console.Write("Choose: ");

    mode = Console.ReadLine() switch
    {
        "1" => "phase1",
        "2" => "phase2",
        "3" => "phase3",
        "4" => "phase4",
        "5" => "all",
        _ => ""
    };
}

switch (mode)
{
    case "phase1":
        await RunPhaseAsync(Phase1Runner.RunAsync);
        break;

    case "phase2":
        await RunPhaseAsync(Phase2Runner.RunAsync);
        break;

    case "phase3":
        await RunPhaseAsync(Phase3Runner.RunAsync);
        break;

    case "phase4":
        await RunPhaseAsync(Phase4Runner.RunAsync);
        break;

    case "all":
        await RunPhaseAsync(Phase1Runner.RunAsync);
        await RunPhaseAsync(Phase2Runner.RunAsync);
        await RunPhaseAsync(Phase3Runner.RunAsync);
        await RunPhaseAsync(Phase4Runner.RunAsync);
        break;

    default:
        Console.WriteLine("Unknown mode. Use: phase1 | phase2 | phase3 | phase4 | all");
        break;
}

async Task RunPhaseAsync(Func<CancellationToken, Task> phase)
{
    using var run = new RunCancellation(cancelAfter);
    await phase(run.Token);
}