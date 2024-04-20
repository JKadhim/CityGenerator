using System;

// Class representing a ring buffer data structure with a fixed size,
// which overwrites old elements when the buffer is full.
public class RingBuffer<T>
{
    public readonly int Size;

    // Action delegate to handle overflow when the buffer is full.
    public Action<T> OnOverflow;

    public int Count
    {
        get;
        private set;
    }

    public int TotalCount
    {
        get;
        private set;
    }

    private readonly T[] buffer;
    private int position;

    public RingBuffer(int size)
    {
        Size = size;
        buffer = new T[size];
        Count = 0;
        position = 0;
    }

    // Method to push a new item into the buffer.
    public void Push(T item)
    {
        // Move to the next position in the circular buffer.
        position = (position + 1) % Size;

        // If the buffer at the new position is not null and an overflow handler is provided,
        // invoke the overflow handler with the evicted item.
        if (!object.Equals(buffer[position], default(T)) && OnOverflow != null)
        {
            OnOverflow(buffer[position]);
        }

        // Store the new item in the buffer at the current position.
        buffer[position] = item;
        Count++;

        // If the count exceeds the size, set it back to the size.
        if (Count > Size)
        {
            Count = Size;
        }
        TotalCount++;
    }

    // Method to peek at the item in the buffer without removing it.
    public T Peek()
    {
        // If the buffer is empty, throw an exception.
        if (Count == 0)
        {
            throw new InvalidOperationException();
        }
        // Return the item at the current position in the buffer.
        return buffer[position];
    }

    // Method to remove and return the last item pushed into the buffer.
    public T Pop()
    {
        // If the buffer is empty, throw an exception.
        if (Count == 0)
        {
            throw new InvalidOperationException();
        }
        T result = buffer[position];
        
        // Reset the buffer at the current position to the default value of its type.
        buffer[position] = default;

        // Move the position backwards in the circular buffer.
        position = (position + Size - 1) % Size;

        Count--;
        TotalCount--;

        // Return the removed item.
        return result;
    }

    // Method to check if the buffer contains any elements.
    public bool Any()
    {
        return Count != 0;
    }
}

