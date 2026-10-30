# Assignment 05 — Answers

Student name:  
Group / ID:

## Failures you observed

Unhandled exception. System.InvalidOperationException: Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.


Unhandled exception. System.ArgumentException: An item with the same key has already been added. Key: koshary-bowl
-

## What you changed

make the collection concurrent to handle the operations in atomic processes


Dictionary → ConcurrentDictionary

Queue → ConcurrentQueue	

ContainsKey + Add + Views[product] = Views[product] + 1 → one AddOrUpdate	

Count > 0 then Dequeue() → TryDequeue()


EmailJobs.Count == 0 → IsEmpty	

-

## Collection safety vs business-rule safety

safe collection not gurantee to be business safe, if we make conditions then changes values we could have problems again!
it just gave us methods to ensure the atomicity,but we must use it right to not fell into threads problems.
-
