using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

public abstract class MapBase
{
    public const float BLOCK_SIZE = 8f;
    public const int HISTORY_SIZE = 10000;

    public static System.Random random;

    public readonly RingBuffer<SavedCell> history;
    public readonly QueueDictionary<Vector3Int, ModuleSet> removalQueue;
    private HashSet<Cell> workArea;
    public readonly Queue<Cell> buildQueue;

    private int backtrackBarrier;
    private int backtrack = 0;

    public readonly short[][] initialEntropy;

    protected MapBase()
    {
        InfiniteMap.random = new System.Random();

        this.history = new RingBuffer<SavedCell>(MapBase.HISTORY_SIZE)
        {
            onOverflow = item => item.cell.Forget()
        };
        this.removalQueue = new QueueDictionary<Vector3Int, ModuleSet>(() => new ModuleSet());
        this.buildQueue = new Queue<Cell>();

        this.initialEntropy = this.CreateEntropy(ModuleData.current);

        this.backtrackBarrier = 0;
    }

    public abstract Cell GetCell(Vector3Int position);

    public abstract IEnumerable<Cell> GetAllCells();

    public abstract void ApplyConstraints(IEnumerable<Constraints> constraints);

    public void NotifyCollapsed(Cell cell)
    {
        this.workArea?.Remove(cell);
        this.buildQueue.Enqueue(cell);
    }

    public void NotifyCollapseUndone(Cell cell)
    {
        this.workArea?.Add(cell);
    }

    public void FinishRemovalQueue()
    {
        while (this.removalQueue.Any())
        {
            var keyValPair = this.removalQueue.Dequeue();
            var cell = this.GetCell(keyValPair.Key);
            if (!cell.Collapsed)
            {
                cell.RemoveModules(keyValPair.Value, false);
            }
        }
    }

    public void Enforce(Vector3Int start, int direction)
    {
        var cell = this.GetCell(start);
        var remove = cell.modules.Where(module => !module.GetFace(direction).walkable);
        cell.RemoveModules(ModuleSet.FromEnumerable(remove));
    }

    public void Enforce(Vector3Int start, Vector3Int destination)
    {
        int direction = Directions.GetIndex((Vector3)(destination - start));
        this.Enforce(start, direction);
        this.Enforce(destination, (direction + 3) % 6);
    }

    public void Collapse(IEnumerable<Vector3Int> targets)
    {
#if UNITY_EDITOR
        try
        {
#endif
            this.removalQueue.Clear();
            this.workArea = new HashSet<Cell>(targets.Select(target => this.GetCell(target)).Where(slot => slot != null && !slot.Collapsed));

            while (this.workArea.Any())
            {
                float minEntropy = float.PositiveInfinity;
                Cell selected = null;

                foreach (var cell in workArea)
                {
                    float entropy = cell.modules.Entropy;
                    if (entropy < minEntropy)
                    {
                        selected = cell;
                        minEntropy = entropy;
                    }
                }
                try
                {
                    selected.CollapseRandom();
                }
                catch (Exception)
                {
                    this.removalQueue.Clear();
                    if (this.history.TotalCount > this.backtrackBarrier)
                    {
                        this.backtrackBarrier = this.history.TotalCount;
                        this.backtrack = 2;
                    }
                    else
                    {
                        this.backtrack *= 2;
                    }
                    if (this.backtrack > 0)
                    {
                        Debug.Log(this.history.Count + " Backtracking " + this.backtrack + " steps...");
                    }
                    this.Undo(this.backtrack);
                }
            }

#if UNITY_EDITOR      
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("Exception in world generation thread at" + exception.StackTrace);
            throw exception;
        }
#endif
    }

    public void Collapse(Vector3Int start, Vector3Int size)
    {
        var toCollapse = new List<Vector3Int>();
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    toCollapse.Add(start + new Vector3Int(x, y, z));
                }
            }
        }
        this.Collapse(toCollapse);
    }

    public void Undo(int steps)
    {
        while (steps > 0 && this.history.Any())
        {
            var item = this.history.Pop();

            foreach (var cellAddress in item.removedModules.Keys)
            {
                this.GetCell(cellAddress).AddModules(item.removedModules[cellAddress]);
            }

            item.cell.module = null;
            this.NotifyCollapseUndone(item.cell);
            steps--;
        }
        if (this.history.Count == 0)
        {
            this.backtrackBarrier = 0;
        }
    }

    private short[][] CreateEntropy(Module[] modules)
    {
        var initialEntropyLocal = new short[6][];
        for (int i = 0; i < 6; i++)
        {
            initialEntropyLocal[i] = new short[modules.Length];
            foreach (var module in modules)
            {
                foreach (var possibleNeighbour in module.possibleNeighbours[(i + 3) % 6])
                {
                    initialEntropyLocal[i][possibleNeighbour.index]++;
                }
            }
        }

#if UNITY_EDITOR
        for (int i = 0; i < modules.Length; i++)
        {
            for (int d = 0; d < 6; d++)
            {
                if (initialEntropyLocal[d][i] == 0)
                {
                    Debug.LogError("Module " + modules[i].name + " cannot be reached from direction " + d + " (" + modules[i].GetFace(d).ToString() + ")!", modules[i].prefabObject);
                    throw new System.Exception("Unreachable");
                }
            }
        }
#endif

        return initialEntropyLocal;
    }

    public short[][] CopyInititalEntropy()
    {
        return this.initialEntropy.Select(a => a.ToArray()).ToArray();
    }
}
