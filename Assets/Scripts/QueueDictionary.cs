using System;
using System.Collections.Generic;

// Class representing a combination of a queue and a dictionary,
// where each item in the queue has an associated value in the dictionary.
public class QueueDictionary<TKey, TValue>
{
    private readonly Queue<TKey> queue;
    private readonly Dictionary<TKey, TValue> dict;

    private readonly Func<TValue> generator;

    public QueueDictionary(Func<TValue> generator)
    {
        this.generator = generator;
        queue = new Queue<TKey>();
        dict = new Dictionary<TKey, TValue>();
    }

    // Peek at the first item in the queue and its associated value in the dictionary.
    public KeyValuePair<TKey, TValue> Peek()
    {
        return new KeyValuePair<TKey, TValue>(queue.Peek(), dict[queue.Peek()]);
    }

    // Dequeue an item from the queue along with its associated value from the dictionary.
    public KeyValuePair<TKey, TValue> Dequeue()
    {
        var key = queue.Dequeue();
        var result = new KeyValuePair<TKey, TValue>(key, dict[key]);
        dict.Remove(key);
        return result;
    }

    // Check if the queue contains any items.
    public bool Any()
    {
        return queue.Count != 0;
    }

    // Indexer to access or modify the value associated with a key.
    public TValue this[TKey key]
    {
        get
        {
            if (!dict.ContainsKey(key))
            {
                // If the key doesn't exist in the dictionary, Generate a default value
                // using the provided generator function and add it to the dictionary.
                dict[key] = generator.Invoke();
                queue.Enqueue(key);
            }
            return dict[key];
        }
        set
        {
            if (!dict.ContainsKey(key))
            {
                // If the key doesn't exist in the dictionary, add it to the queue.
                queue.Enqueue(key);
            }
            // Update the value associated with the key in the dictionary.
            dict[key] = value;
        }
    }

    // Clear both the queue and the dictionary.
    public void Clear()
    {
        dict.Clear();
        queue.Clear();
    }
}

