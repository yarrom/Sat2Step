
using System;
using System.Collections.Generic;

public class Registry
{
    private readonly Dictionary<int, object> built = new();
    private readonly Dictionary<int, List<Action<object>>> waiting = new();

    public void Add(int id, object obj)
    {
        built[id] = obj;
        if (waiting.TryGetValue(id, out var list))
        {
            foreach (var cb in list) cb(obj);
            waiting.Remove(id);
        }
    }

    public T? Get<T>(int id) where T : class
    {
        if (built.TryGetValue(id, out var o)) return o as T;
        return null;
    }

    public void WhenAvailable(int id, Action<object> callback)
    {
        if (id == 0) return;
        if (built.TryGetValue(id, out var o))
        {
            callback(o);
            return;
        }

        if (!waiting.TryGetValue(id, out var list))
        {
            list = new List<Action<object>>();
            waiting[id] = list;
        }
        list.Add(callback);
    }

    public IEnumerable<int> PendingIds() => waiting.Keys;
}
