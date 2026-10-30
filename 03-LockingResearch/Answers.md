# Assignment 03 — Answers

Student name:  Amr Ahmed
Group / ID: G4

1.
(System.Threading.Monitor) its a mechanism to synch access to a block of code across multiple threads.
It gives lock to a single thread at a time,and preventing the race conditions and ensuring thread safety.

2.
lock is shortcut that the compiler turns it into Monitor.Enter and Monitor.Exit inside a try/finally. it's the same idea (lock, release)


3.

I => lock automatically writes the (try finally) block to ensure the lock is always released.

Calling Monitor manually requires you to call Exit yourself in a finally. Forgetting it or if exception happend before exit leaves the object locked forever.

II => lock has no signalling

Monitor has Wait, Pulse signals

III => lock is short and readable

Monitor is longer and more complex and may make errors



4.
Monitor.TryEnter()
when you don’t want to block forever,if the lock is busy skip the work 

Monitor.Wait()
when a thread holds the lock but the condition it needs isn’t true yet so it releases the lock and sleeps until signalled

Monitor.Pulse() / PulseAll()
when you changed the state and want to wake one (or all) the waiting threads



5. 

(i will use the last part example )

with LOCK (as what i solved)

public void Book(string customerName)
{
    lock (_sync)
    {
        if (TablesLeft <= 0)
            return;

        //Thread.Sleep(20);

        TablesLeft--;
        Booked++;
        Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
    }

}

with Monitor:

public void Book(string customerName)
{
    bool isLocked = false;
    try
    {
        Monitor.Enter(_sync, ref isLocked);

        if (TablesLeft <= 0)
            return;

        //Thread.Sleep(20);

        TablesLeft--;
        Booked++;
        Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
    }
    finally
    {
        if (isLocked)
            Monitor.Exit(_sync);
    }
}






Links:
https://www.geeksforgeeks.org/c-sharp/c-sharp-lock-in-thread/

(TBH, I got another more info with LLMs  |:  )

-
