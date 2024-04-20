using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ModuleSet : ICollection<Module>
{
    private const int bitsPerItem = 64;

    [SerializeField]
    private long[] data;

    private float entropy;
    private bool entropyOutdated = true;

    //Count of modules in the module set
    public int Count
    {
        get
        {
            int result = 0;
            for (int i = 0; i < this.data.Length - 1; i++)
            {
                result += CountBits(this.data[i]);
            }
            return result + CountBits(this.data[data.Length - 1] & LastItemUsageMask);
        }
    }

    private long LastItemUsageMask
    {
        get
        {
            return ((long)1 << (ModuleData.current.Length % 64)) - 1;
        }
    }

    //Checks if the set is full
    public bool Full
    {
        get
        {
            for (int i = 0; i < this.data.Length - 1; i++)
            {
                if (this.data[i] != ~0)
                {
                    return false;
                }
            }
            return (~this.data[data.Length - 1] & this.LastItemUsageMask) == 0;
        }
    }

    //Checks if the set is empty
    public bool Empty
    {
        get
        {
            for (int i = 0; i < this.data.Length - 1; i++)
            {
                if (this.data[i] != 0)
                {
                    return false;
                }
            }
            return (this.data[data.Length - 1] & this.LastItemUsageMask) == 0;
        }
    }

    public float Entropy
    {
        get
        {
            if (this.entropyOutdated)
            {
                this.entropy = this.CalculateEntropy();
                this.entropyOutdated = false;
            }
            return this.entropy;
        }
    }

    //Initializes a new instance of the ModuleSet class.
    public ModuleSet(bool initializeFull = false)
    {
        // Calculate the number of long integers needed to store the module data
        int dataSize = ModuleData.current.Length / bitsPerItem + (ModuleData.current.Length % bitsPerItem == 0 ? 0 : 1);
        this.data = new long[dataSize];

        // If initializeFull is true, set all bits in the data to 1 (indicating all modules are present)
        if (initializeFull)
        {
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] = ~0;
            }
        }
    }



    //Initializes a new instance of the ModuleSet class from an enumerable collection of modules.
    public ModuleSet(IEnumerable<Module> source) : this()
    {
        // Add modules from the source collection to the ModuleSet
        foreach (var module in source)
        {
            this.Add(module);
        }
    }


    //Initializes a new instance of the ModuleSet class from an existing ModuleSet.
    public ModuleSet(ModuleSet source)
    {
        // Copy data from the source ModuleSet
        this.data = source.data.ToArray();
        this.entropy = source.Entropy;
        this.entropyOutdated = false;
    }

    //Creates a new ModuleSet from an enumerable collection of modules.
    public static ModuleSet FromEnumerable(IEnumerable<Module> source)
    {
        var result = new ModuleSet();
        foreach (var module in source)
        {
            result.Add(module);
        }
        return result;
    }

    // Adds the module to the ModuleSet.
    public void Add(Module module)
    {
        int i = module.index / bitsPerItem;
        long mask = (long)1 << (module.index % bitsPerItem);

        long value = this.data[i];

        if ((value & mask) == 0)
        {
            this.data[i] = value | mask;
            this.entropyOutdated = true;
        }
    }


    // Adds all modules from set2 to set1.
    public void Add(ModuleSet set)
    {
        for (int i = 0; i < this.data.Length; i++)
        {
            long current = this.data[i];
            long updated = current | set.data[i];

            if (current != updated)
            {
                this.data[i] = updated;
                this.entropyOutdated = true;
            }
        }
    }

    //Removes a module from the ModuleSet.
    public bool Remove(Module module)
    {
        int i = module.index / bitsPerItem;
        long mask = (long)1 << (module.index % bitsPerItem);

        long value = this.data[i];

        if ((value & mask) != 0)
        {
            this.data[i] = value & ~mask;
            this.entropyOutdated = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    //Removes all modules from this ModuleSet that are also present in another ModuleSet.
    public void Remove(ModuleSet set)
    {
        for (int i = 0; i < this.data.Length; i++)
        {
            long current = this.data[i];
            long updated = current & ~set.data[i];

            if (current != updated)
            {
                this.data[i] = updated;
                this.entropyOutdated = true;
            }
        }
    }

    //Checks if the ModuleSet contains a specific module
    public bool Contains(Module module)
    {
        int i = module.index / bitsPerItem;
        long mask = (long)1 << (module.index % bitsPerItem);
        return (this.data[i] & mask) != 0;
    }

    //Checks if the ModuleSet contains a specific module at a specific index
    public bool Contains(int index)
    {
        int i = index / bitsPerItem;
        long mask = (long)1 << (index % bitsPerItem);
        return (this.data[i] & mask) != 0;
    }

    //Clears all modules from the ModuleSet
    public void Clear()
    {
        this.entropyOutdated = true;
        for (int i = 0; i < this.data.Length; i++)
        {
            this.data[i] = 0;
        }
    }

    //Intersects the current ModuleSet with another ModuleSet by
    //keeping only the elements that are in both sets.
    public void Intersect(ModuleSet moduleSet)
    {
        for (int i = 0; i < this.data.Length; i++)
        {
            long current = this.data[i];
            long mask = moduleSet.data[i];
            long updated = current & mask;

            if (current != updated)
            {
                this.data[i] = updated;
                this.entropyOutdated = true;
            }
        }
    }


    //Counts the number of set bits (1s) in a 64-bit integer using bitwise operations.
    //https://stackoverflow.com/questions/2709430/count-number-of-bits-in-a-64-bit-long-big-integer/2709523#2709523
    private static int CountBits(long i)
    {
        // Apply bit manipulation operations to count the set bits
        i -= ((i >> 1) & 0x5555555555555555);
        i = (i & 0x3333333333333333) + ((i >> 2) & 0x3333333333333333);
        return (int)((((i + (i >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
    }

    //indicating whether the ModuleSet is read-only
    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    //Copies the elements of the ModuleSet to an array, starting at a particular index
    public void CopyTo(Module[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex] = item;
            arrayIndex++;
        }
    }


    //Provides an enumerator for iterating over the modules represented by the bit-packed data
    public IEnumerator<Module> GetEnumerator()
    {
        int index = 0;
        for (int i = 0; i < this.data.Length; i++)
        {
            long value = this.data[i];

            if (value == 0)
            {
                index += bitsPerItem;
                continue;
            }
            for (int j = 0; j < bitsPerItem; j++)
            {
                if ((value & ((long)1 << j)) != 0) // Check if the bit is set
                {
                    yield return ModuleData.current[index]; // Yield return the corresponding module
                }
                index++; // Move to the next index
                if (index >= ModuleData.current.Length)
                {
                    yield break; // Exit the loop if the end of the module data is reached
                }
            }
        }
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    //Calculate the entropy of a collection of modules.
    private float CalculateEntropy()
    {
        float total = 0;
        float entropySum = 0;
        foreach (var module in this)
        {
            total += module.prefab.probability;
            entropySum += module.PLogP;
        }
        return -1f / total * entropySum + Mathf.Log(total);
    }
}