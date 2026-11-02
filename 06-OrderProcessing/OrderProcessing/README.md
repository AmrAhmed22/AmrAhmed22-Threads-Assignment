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