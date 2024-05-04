using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class ModuleSet : ICollection<Module>
{
    private const int bitsPerItem = 64;

    [SerializeField]
    private long[] data;

    private float entropy;
    private bool entropyOutdated = true;

    public int Count
    {
        get
        {
            int result = 0;
            for (int i = 0; i < this.data.Length - 1; i++)
            {
                result += CountBits(this.data[i]);
            }
            return result + CountBits(this.data[^1] & this.LastItemUsageMask);
        }
    }

    private long LastItemUsageMask
    {
        get
        {
            return ((long)1 << (ModuleData.current.Length % 64)) - 1;
        }
    }

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
            return (~this.data[^1] & this.LastItemUsageMask) == 0;
        }
    }

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
            return (this.data[^1] & this.LastItemUsageMask) == 0;
        }
    }

    public float Entropy
    {
        get
        {
            if (this.entropyOutdated)
            {
                this.entropy = this.FindEntropy();
                this.entropyOutdated = false;
            }
            return this.entropy;
        }
    }

    public ModuleSet(bool initializeFull = false)
    {
        this.data = new long[ModuleData.current.Length / bitsPerItem + (ModuleData.current.Length % bitsPerItem == 0 ? 0 : 1)];

        if (initializeFull)
        {
            for (int i = 0; i < this.data.Length; i++)
            {
                this.data[i] = ~0;
            }
        }
    }

    public ModuleSet(IEnumerable<Module> source) : this()
    {
        foreach (var module in source)
        {
            this.Add(module);
        }
    }

    public ModuleSet(ModuleSet source)
    {
        this.data = source.data.ToArray();
        this.entropy = source.Entropy;
        this.entropyOutdated = false;
    }

    public static ModuleSet FromEnumerable(IEnumerable<Module> source)
    {
        var result = new ModuleSet();
        foreach (var module in source)
        {
            result.Add(module);
        }
        return result;
    }

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

    public bool Contains(Module module)
    {
        int i = module.index / bitsPerItem;
        long mask = (long)1 << (module.index % bitsPerItem);
        return (this.data[i] & mask) != 0;
    }

    public bool Contains(int index)
    {
        int i = index / bitsPerItem;
        long mask = (long)1 << (index % bitsPerItem);
        return (this.data[i] & mask) != 0;
    }

    public void Clear()
    {
        this.entropyOutdated = true;
        for (int i = 0; i < this.data.Length; i++)
        {
            this.data[i] = 0;
        }
    }

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

    // https://stackoverflow.com/a/2709523/895589
    private static int CountBits(long i)
    {
        i -= ((i >> 1) & 0x5555555555555555);
        i = (i & 0x3333333333333333) + ((i >> 2) & 0x3333333333333333);
        return (int)((((i + (i >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
    }

    public bool IsReadOnly
    {
        get
        {
            return false;
        }
    }

    public void CopyTo(Module[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex] = item;
            arrayIndex++;
        }
    }

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
                if ((value & ((long)1 << j)) != 0)
                {
                    yield return ModuleData.current[index];
                }
                index++;
                if (index >= ModuleData.current.Length)
                {
                    yield break;
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)this.GetEnumerator();
    }

    private float FindEntropy()
    {
        float total = 0;
        float entropyTotal = 0;
        foreach (var module in this)
        {
            total += module.prefab.probability;
            entropyTotal += module.log;
        }
        return -1f / total * entropyTotal + Mathf.Log(total);
    }
}
