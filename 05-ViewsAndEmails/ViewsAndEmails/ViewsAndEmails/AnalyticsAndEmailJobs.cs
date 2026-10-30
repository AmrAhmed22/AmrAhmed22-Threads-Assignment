using System.Collections.Concurrent;

namespace ViewsAndEmails;

public sealed class AnalyticsAndEmailJobs
{
    public readonly ConcurrentDictionary<string, int> Views = new();
    public readonly ConcurrentQueue<string> EmailJobs = new();
    public readonly List<string> SentEmails = new();

    readonly object _sentSync = new();
    public volatile bool ProducersDone;

    public void RecordView(string product)
    {
        Views.AddOrUpdate(product, 1, (_, old) => old + 1); //one atomic operation
    }


    public void PlaceOrder(int orderId)
    {
        EmailJobs.Enqueue($"Email: order {orderId}");//one atomic operation
    }


    public void EmailWorker()
    {
        while (true)
        {
            try
            {
                if (EmailJobs.TryDequeue(out string? job))//one atomic operation check and dequeu
                {
                    lock (_sentSync)
                        SentEmails.Add(job);
                    continue;
                }

                if (ProducersDone && EmailJobs.IsEmpty)
                    return;

                //Thread.Sleep(1);     // i hate thread sleep  •? ? •?
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("InvalidOperationException");
                if (ProducersDone)
                    return;
            }

    
        }

    
    }


}

