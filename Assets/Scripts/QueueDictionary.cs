using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueueDictionary<TKey, TValue>
{
    private readonly Queue<TKey> queue;
    private readonly Dictionary<TKey, TValue> dict;
    private readonly Func<TValue> generator;

    public QueueDictionary(Func<TValue> generator)
    {
        this.generator = generator;
        this.queue = new Queue<TKey>();
        this.dict = new Dictionary<TKey, TValue>();
    }

    public TValue this[TKey key]
    {
        get
        {
            if (!this.dict.ContainsKey(key))
            {
                this.dict[key] = this.generator.Invoke();
                this.queue.Enqueue(key);
            }
            return this.dict[key];
        }
        set
        {
            if (!this.dict.ContainsKey(key))
            {
                this.queue.Enqueue(key);
            }
            this.dict[key] = value;
        }
    }

    public KeyValuePair<TKey, TValue> Peek()
    {
        return new KeyValuePair<TKey, TValue>(this.queue.Peek(), this.dict[this.queue.Peek()]);
    }

    public KeyValuePair<TKey, TValue> Dequeue()
    {
        var key = this.queue.Dequeue();
        var result = new KeyValuePair<TKey, TValue>(key, this.dict[key]);
        this.dict.Remove(key);
        return result;
    }

    public bool Any()
    {
        return this.queue.Count != 0;
    }

    public void Clear()
    {
        this.dict.Clear();
        this.queue.Clear();
    }
}
