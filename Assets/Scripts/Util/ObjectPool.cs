using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic pool for reference-type instances. Reused instances mean runtime
/// code paths that spawn/despawn frequently (grid cells, tray pieces,
/// particles) never call Instantiate/Destroy and never allocate per call.
/// </summary>
public sealed class ObjectPool<T> where T : class
{
    private readonly Stack<T> _inactive;
    private readonly HashSet<T> _active;
    private readonly Func<T> _factory;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onReturn;

    public int CountActive => _active.Count;
    public int CountInactive => _inactive.Count;
    public int CountAll => CountActive + CountInactive;

    public ObjectPool(Func<T> factory, Action<T> onGet = null, Action<T> onReturn = null, int initialCapacity = 0)
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _factory = factory;
        _onGet = onGet;
        _onReturn = onReturn;
        _inactive = new Stack<T>(initialCapacity);
        _active = new HashSet<T>();

        for (int i = 0; i < initialCapacity; i++)
        {
            _inactive.Push(_factory());
        }
    }

    public T Get()
    {
        T item = _inactive.Count > 0 ? _inactive.Pop() : _factory();
        _active.Add(item);
        _onGet?.Invoke(item);
        return item;
    }

    public void Return(T item)
    {
        if (!_active.Remove(item))
        {
            Debug.LogError("ObjectPool: attempted to return an item that is not currently active in this pool.");
            return;
        }

        _onReturn?.Invoke(item);
        _inactive.Push(item);
    }

    public void Clear()
    {
        _inactive.Clear();
        _active.Clear();
    }
}
