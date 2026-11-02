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