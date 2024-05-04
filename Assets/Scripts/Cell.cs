using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cell
{
    public Vector3Int position;

    // List of modules that can still be placed here
    public ModuleSet modules;

    // direction -> moduleLocal -> Number of items in this.getneighbor(direction).modules that allow this moduleLocal as a neighbour
    public short[][] moduleHealth;

    private readonly MapBase map;

    public Module module;

    public GameObject gameObject;

    public bool Collapsed
    {
        get
        {
            return this.module != null;
        }
    }

    public bool ConstructionComplete
    {
        get
        {
            return this.gameObject != null || (this.Collapsed && !this.module.prefab.spawn);
        }
    }

    public Cell(Vector3Int position, MapBase map)
    {
        this.position = position;
        this.map = map;
        this.moduleHealth = map.CopyInititalEntropy();
        this.modules = new ModuleSet(initializeFull: true);
    }

    public Cell(Vector3Int position, MapBase map, Cell prototype)
    {
        this.position = position;
        this.map = map;
        this.moduleHealth = prototype.moduleHealth.Select(a => a.ToArray()).ToArray();
        this.modules = new ModuleSet(prototype.modules);
    }

    public Cell GetNeighbour(int direction)
    {
        return this.map.GetCell(this.position + Directions.Direction[direction]);
    }

    public void Collapse(Module module)
    {
        if (this.Collapsed)
        {
            Debug.LogWarning("Cell already collapsed");
            return;
        }

        this.map.history.Push(new SavedCell(this));

        this.module = module;
        var remove = new ModuleSet(this.modules);
        remove.Remove(module);
        this.RemoveModules(remove);

        this.map.NotifyCollapsed(this);
    }

    public void CollapseRandom()
    {
        if (!this.modules.Any())
        {
            throw new Exception(this);
        }
        if (this.Collapsed)
        {
            throw new System.Exception("Cell already collapsed.");
        }

        float max = this.modules.Select(module => module.prefab.probability).Sum();
        float rand = (float)(InfiniteMap.random.NextDouble() * max);
        float probability = 0;
        foreach (var candidate in this.modules)
        {
            probability += candidate.prefab.probability;
            if (probability >= rand)
            {
                this.Collapse(candidate);
                return;
            }
        }
        this.Collapse(this.modules.First());
    }

    // This modifies the supplied ModuleSet as a side effect
    public void RemoveModules(ModuleSet modulesToRemove, bool recursive = true)
    {
        modulesToRemove.Intersect(this.modules);

        if (this.map.history != null && this.map.history.Any())
        {
            var item = this.map.history.Peek();
            if (!item.removedModules.ContainsKey(this.position))
            {
                item.removedModules[this.position] = new ModuleSet();
            }
            item.removedModules[this.position].Add(modulesToRemove);
        }

        for (int i = 0; i < 6; i++)
        {
            int opposite = (i + 3) % 6;
            var neighbour = this.GetNeighbour(i);
            if (neighbour == null || neighbour.Forgotten)
            {
#if UNITY_EDITOR
                if (this.map is InfiniteMap && (this.map as InfiniteMap).IsOutOfRange(this.position + Directions.Direction[i]))
                {
                    (this.map as InfiniteMap).OnHitRangeLimit(this.position + Directions.Direction[i], modulesToRemove);
                }
#endif
                continue;
            }

            foreach (var moduleLocal in modulesToRemove)
            {
                for (int j = 0; j < moduleLocal.possibleNeighboursArray[i].Length; j++)
                {
                    var possibleNeighbour = moduleLocal.possibleNeighboursArray[i][j];
                    if (neighbour.moduleHealth[opposite][possibleNeighbour.index] == 1 && neighbour.modules.Contains(possibleNeighbour))
                    {
                        this.map.removalQueue[neighbour.position].Add(possibleNeighbour);
                    }
#if UNITY_EDITOR
                    if (neighbour.moduleHealth[opposite][possibleNeighbour.index] < 1)
                    {
                        throw new System.InvalidOperationException("ModuleHealth must not be negative. " + this.position + " d: " + i);
                    }
#endif
                    neighbour.moduleHealth[opposite][possibleNeighbour.index]--;
                }
            }
        }

        this.modules.Remove(modulesToRemove);

        if (this.modules.Empty)
        {
            throw new Exception(this);
        }

        if (recursive)
        {
            this.map.FinishRemovalQueue();
        }
    }

    public void AddModules(ModuleSet modulesToAdd)
    {
        foreach (var moduleLocal in modulesToAdd)
        {
            if (this.modules.Contains(moduleLocal) || moduleLocal == this.module)
            {
                continue;
            }
            for (int i = 0; i < 6; i++)
            {
                int opposite = (i + 3) % 6;
                var neighbour = this.GetNeighbour(i);
                if (neighbour == null || neighbour.Forgotten)
                {
                    continue;
                }

                foreach (var possibleNeighbour in moduleLocal.possibleNeighbours[i])
                {
                    neighbour.moduleHealth[opposite][possibleNeighbour.index]++;
                }
            }
            this.modules.Add(moduleLocal);
        }

        if (this.Collapsed && !this.modules.Empty)
        {
            this.module = null;
            this.map.NotifyCollapseUndone(this);
        }
    }

    public void EnforceConnector(int direction, int connector)
    {
        var remove = this.modules.Where(module => !module.Fits(direction, connector));
        this.RemoveModules(ModuleSet.FromEnumerable(remove));
    }

    public void ExcludeConnector(int direction, int connector)
    {
        var remove = this.modules.Where(module => module.Fits(direction, connector));
        this.RemoveModules(ModuleSet.FromEnumerable(remove));
    }

    public void Forget()
    {
        this.moduleHealth = null;
        this.modules = null;
    }

    public bool Forgotten
    {
        get
        {
            return this.modules == null;
        }
    }
}