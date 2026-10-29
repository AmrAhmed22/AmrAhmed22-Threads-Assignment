Problems:

1- using Thread.sleep()
like in OrderDatabase class it blocks the thread from working which is bad, Replace it with Task.Delay() it make the task waits but the thread is free. 

2- no Task in the classes, they return normal lists

3- No Cancellation in any class

4- File funcs not Async as(File.ReadAllText())

5-if one export failed the next ones never exxported(no error handling)

6- in ExportAllPending, GetOrderById get the order again after GetPendingOrders for each order sequentially, we can make it concurrent.



Before:
Export finished in 5.9 s

After:
Export finished in 1.9 s
