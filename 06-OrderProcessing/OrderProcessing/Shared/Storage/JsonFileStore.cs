using OrderProcessing.Shared.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace OrderProcessing.Shared.Storage
{
    public sealed class JsonFileStore
    {
        static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

        static SemaphoreSlim GateFor(string fileName) =>
            Gates.GetOrAdd(fileName, _ => new SemaphoreSlim(1, 1));

        static string PathOf(string fileName) => Path.Combine(DataFiles.Folder, fileName);


        public async Task<List<T>> ReadAllAsync<T>(string fileName, CancellationToken ct)
        {
            SemaphoreSlim gate = GateFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                return await ReadAsync<T>(fileName, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task WriteAllAsync<T>(string fileName, List<T> items, CancellationToken ct)
        {
            SemaphoreSlim gate = GateFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                await WriteAsync(fileName, items, ct);
            }
            finally
            {
                gate.Release();
            }
        }

        public async Task<TResult> UpdateAsync<T, TResult>(string fileName, Func<List<T>, TResult> update, CancellationToken ct)
        {
            SemaphoreSlim gate = GateFor(fileName);
            await gate.WaitAsync(ct);
            try
            {
                List<T> items = await ReadAsync<T>(fileName, ct);
                TResult result = update(items);
                await WriteAsync(fileName, items, ct);
                return result;
            }
            finally
            {
                gate.Release();
            }
        }

        public Task ModifyAsync<T>(string fileName, Action<List<T>> modify, CancellationToken ct) =>
            UpdateAsync<T, bool>(fileName, items => { modify(items); return true; }, ct);


        public List<T> ReadAll<T>(string fileName)
        {
            SemaphoreSlim gate = GateFor(fileName);
            gate.Wait();
            try
            {
                return Read<T>(fileName);
            }
            finally
            {
                gate.Release();
            }
        }

        public void WriteAll<T>(string fileName, List<T> items)
        {
            SemaphoreSlim gate = GateFor(fileName);
            gate.Wait();
            try
            {
                Write(fileName, items);
            }
            finally
            {
                gate.Release();
            }
        }

        public TResult Update<T, TResult>(string fileName, Func<List<T>, TResult> update)
        {
            SemaphoreSlim gate = GateFor(fileName);
            gate.Wait();
            try
            {
                List<T> items = Read<T>(fileName);
                TResult result = update(items);
                Write(fileName, items);
                return result;
            }
            finally
            {
                gate.Release();
            }
        }

        public void Modify<T>(string fileName, Action<List<T>> modify) =>
            Update<T, bool>(fileName, items => { modify(items); return true; });


        static async Task<List<T>> ReadAsync<T>(string fileName, CancellationToken ct)
        {
            string path = PathOf(fileName);
            if (!File.Exists(path))
                return new List<T>();                      

            string json = await File.ReadAllTextAsync(path, ct);
            return Parse<T>(json, fileName);
        }

        static List<T> Read<T>(string fileName)
        {
            string path = PathOf(fileName);
            if (!File.Exists(path))
                return new List<T>();

            string json = File.ReadAllText(path);
            return Parse<T>(json, fileName);
        }

        static List<T> Parse<T>(string json, string fileName)
        {
            try
            {
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions.Default)
                       ?? throw new DataFileCorruptedException(fileName);
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);   
            }
        }

        static async Task WriteAsync<T>(string fileName, List<T> items, CancellationToken ct)
        {
            string path = PathOf(fileName);
            string temp = path + ".tmp";
            Directory.CreateDirectory(DataFiles.Folder);

            try
            {
                string json = JsonSerializer.Serialize(items, JsonOptions.Default);
                await File.WriteAllTextAsync(temp, json, ct);
                File.Move(temp, path, overwrite: true);
            }
            catch
            {
                File.Delete(temp);     
                throw;
            }
        }

        static void Write<T>(string fileName, List<T> items)
        {
            string path = PathOf(fileName);
            string temp = path + ".tmp";
            Directory.CreateDirectory(DataFiles.Folder);

            try
            {
                string json = JsonSerializer.Serialize(items, JsonOptions.Default);
                File.WriteAllText(temp, json);
                File.Move(temp, path, overwrite: true);
            }
            catch
            {
                File.Delete(temp);
                throw;
            }
        }
    }
}