using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingBuffer<T>
{
    public readonly int size;

    public Action<T> onOverflow;

    public int Count
    {
        get;
        private set;
    }

    // Includes discarded items
    public int TotalCount
    {
        get;
        private set;
    }

    private readonly T[] buffer;
    private int position;

    public RingBuffer(int size)
    {
        this.size = size;
        this.buffer = new T[size];
        this.Count = 0;
        this.position = 0;
    }

    public void Push(T item)
    {
        this.position = (this.position + 1) % this.size;
        if (!object.Equals(this.buffer[this.position], default(T)) && this.onOverflow != null)
        {
            this.onOverflow(this.buffer[this.position]);
        }
        this.buffer[this.position] = item;
        this.Count++;
        if (this.Count > this.size)
        {
            this.Count = this.size;
        }
        this.TotalCount++;
    }

    public T Peek()
    {
        if (this.Count == 0)
        {
            throw new System.InvalidOperationException();
        }
        return this.buffer[this.position];
    }

    public T Pop()
    {
        if (this.Count == 0)
        {
            throw new System.InvalidOperationException();
        }
        T result = this.buffer[this.position];
        this.buffer[this.position] = default;

        this.position = (this.position + this.size - 1) % this.size;
        this.Count--;
        this.TotalCount--;

        return result;
    }

    public bool Any()
    {
        return this.Count != 0;
    }
}
