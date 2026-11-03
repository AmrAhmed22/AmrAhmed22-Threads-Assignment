6.2:

## JsonFileStore API

| Method | Purpose |
|---|---|
| `ReadAllAsync<T>(file, ct)` | read the whole file as a list |
| `WriteAllAsync<T>(file, items, ct)` | replace the whole file |
| `UpdateAsync<T,TResult>(file, update, ct)` | read, change and write as one atomic step, returns a result |
| `ModifyAsync<T>(file, modify, ct)` | same, when nothing is returned |
| `ReadAll / WriteAll / Update / Modify` | sync versions, used only by the sync comparison classes |



SemaphoreSlim(1,1)
bec i want one task only at a time,and not lock bec i want to use await and lock cant contain await

one generic class serves every collection, callers pass the file name

make the `UpdateAsync` atomic so the read and the write happen as one step, and the sync methods exist bec the assignment requires them for the sync comparison.



Phase 1:

===== Validation Summary =====
Total Orders: 20
Validated: 15
Rejected: 5
 - Order #4: Customer is inactive
 - Order #9: Insufficient stock for 'Wireless Mouse'
 - Order #12: Coupon 'OLD20' has expired
 - Order #15: Customer #99 not found
 - Order #18: Coupon 'FAKE50' does not exist
Sync Time: 34.3 seconds
Async Time: 52.6 seconds
orders.json now contains 20 records.


Phase 1 questions:

1. bec await on every order will block the task too as sync

2. NO,await free the thread and return it to the pool so the thread can run another work and when the task return it will work on any free thread in the pool at that time .
in the logs here show Thread 12 then Thread 11 inside one order

[04:26:44.106] [Thread 12] ProductRepository -> Executing query for Product #2...
[04:26:44.685] [Thread 11] ProductRepository -> Product #2 found (Smartphone, stock 15)


3. Task.Delay(): the thread will be free and return to the pool and can run another work. 
Thread.Sleep(): the thread will be blocked and does nothing .

4. bec the method returns a promise of the result

5. Sync blocks the thread during the disk read. 
async doesn't block the thread for the read

Phase 2 :

8.4:

### Phase 2 compared with Phase 1

Phase 1 handled the 20 orders one after another: <53> s
Phase 2 starts all validated orders together: <9> s
The waits now overlap, so the total is close to the slowest order, not the sum of all orders.



8.5:

### Why Task.Run fits the invoice but not the database calls

The invoice is CPU bound: a core calculates the whole time, so `Task.Run` moves that work to a pool thread and the caller is not blocked.

The database calls are I/O waits, and `await` already frees the thread while waiting, so `Task.Run` would only occupy a pool thread that do nothing.


### Phase 2 questions

1. **Starting vs awaiting a task:** Starting a task begins the work and gives me the Task object now. Awaiting it waits for the result without blocking the thread 

2. **Does every Task create a thread? Does Task.Delay use a thread?**No, a Task does not always create a new thread it is simply a promise of work it reuse existing threads from the ThreadPool. `Task.Delay` uses a timer, so no thread is held while it waits.

3. **.Result or .Wait() instead of await:** The calling thread is blocked until the task finishes. It is dangerous because it wastes a thread

public void BlockingTaskDemo()
{
     Task<string> dataTask = Task.Run(() => "Data Loaded");

    string result = dataTask.Result; //thread will blocked here 

    Console.WriteLine(result);
}

4. **Exception in a task that is never awaited:** It stays inside the task and nobody sees it. The program continues as if nothing happened, so the error is lost.

5. **Task.Run:** Use it for CPU heavy work like big loop, so it runs on a pool thread. Avoid it for I/O waits like database calls, because `await` frees the thread.

6. **Task vs Task<T>:** Task represents an async operation that does not return a value (void). Task<T> represents an async operation that returns a value of type T .

7. **Why can a lock block not contain await:** A lock is owned by one thread. After an `await` the code may continue on a different thread, which could not release the lock. The compiler forbids it.

8. **Why the unprotected reservation oversold:** Both orders read stock 1 before either wrote, so both passed the check and both wrote 0. I prevented it with `UpdateAsync`, which checks and subtracts while holding the file's gate.



Phase 3 :

| Version | Time | Threads | Pool | Result |
|---|---|---|---|---|
| Sync (10 threads) | 11.3s | 10 | 4 | 8 Completed, 2 Rejected |
| Async (10 tasks) | 11.1s | 7 | 7 | 8 Completed, 2 Rejected |


### Phase 3: sync vs async

1. **Does async make one order's workflow faster?** No. One order still run the same steps and waits, so it takes about the same time. Async only helps when many orders run together.
2. **Why does async scale better for many orders?** A waiting order gives its thread back, so a few pool threads can run many orders. The sync version needs one blocked thread per order.
3. **What happens to the sync version with 1,000 orders?** It needs 1,000 threads, each reserving about 1 MB of stack and mostly sitting idle. Memory still growing, and the app becomes slow or runs out of resources.
4. **Why is async essential for apps that call databases, files and APIs?** bec most of their time is waiting for I/O. Async uses that waiting time for other work, instead of holding the thread.



### Fire-and-forget investigation

1. **The problem:** no await for the email task. `ProcessOrderAsync` prints "Order processing finished" while the email is still being sent, and no one will know if it succeeded or not.

2. **Method returns early / console exits:**
When the method returns: The caller assumes the entire process is complete, even though the email is still sending in the background.
When the app exits: The background email task is killed instantly. The email will not be sent.

3. **Exception:** 
The app will not crash, and a standard try/catch block will not catch the error. we will simply never know that the email failed.

4. **Fix:** `await _emailService.SendConfirmationAsync(order, customer, ct)`
Now the method waits for the email, the exception reaches the caller, and the method only finishes when the email is done.

**When await is necessary:** 
when the result, the success or the failure matters, or the work must finish before the program ends.

**When fire-and-forget is acceptable (if ever):** 
it's only acceptable for non critical tasks like background logging where you do not care when the task finishes or if it fails.

**Why `async void` is dangerous:** 
because you cannot await it to know when it finishes, and any unhandled errors inside it will crash the entire application.

**Why it is acceptable for event handlers:**
 event handler signatures return `void`, so `async void` is the only way to use `await` there. There is no caller to await it, and the handler built to handle the exceptions safely.
 