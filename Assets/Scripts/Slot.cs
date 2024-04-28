using System.Linq;
using UnityEngine;


//Slot class represents a slot within a map grid.
public class Slot
{
    public readonly Vector3Int position;
    public ModuleSet modules;
    public short[][] moduleHealth;

    private readonly Map map;

    public Module module;
    public GameObject gameObject;

    // Indicates whether the slot is collapsed (contains a module)
    public bool Collapsed { get { return module != null; } }
    // Indicates whether the slot is constructed (has a GameObject or a collapsed module)
    public bool Constructed { get { return gameObject != null || (Collapsed && !module.prefab.spawn); } }

    public Slot(Vector3Int position, Map map)
    {
        this.position = position;
        this.map = map;
        moduleHealth = map.CopyInitialModuleHealth();
        modules = new ModuleSet(initializeFull: true);
    }

    // Constructor to InitializeMap a new instance of the Slot class as a copy of the prefabObject
    public Slot(Vector3Int position, Map map, Slot prefab)
    {
        this.position = position;
        this.map = map;
        moduleHealth = prefab.moduleHealth.Select(a => a.ToArray()).ToArray();
        modules = new ModuleSet(prefab.modules);
    }

    //retrieves the neighbouring slot in the specified direction.
    public Slot GetNeighbour(int direction)
    {
        return map.GetSlot(position + Directions.Direction[direction]);
    }

    public void Collapse(Module module)
    {
        // Checks if the slot is already collapsed.
        if (Collapsed)
        {
            Debug.LogWarning("already collapsed");
            return;
        }

        // Records the collapse action in the map's history.
        map.history.Push(new HistoryItem(this));

        this.module = module;

        // Creates a set of modules to remove and removes the specified module from it.
        var toRemove = new ModuleSet(modules);
        toRemove.Remove(module);
        RemoveModules(toRemove);

        // Notifies the map that the slot has collapsed.
        map.NotifySlotCollapsed(this);
    }

    //randomly collapses the slot with one of its modules
    public void CollapseRandom()
    {
        // Throws a CollapseFailedException if there are no modules in the slot.
        if (!modules.Any())
        {
            throw new CollapseFailedException(this);
        }

        // Throws a System.Exception if the slot is already collapsed.
        if (Collapsed)
        {
            throw new System.Exception("already collapsed");
        }

        // Calculates the maximum probability by summing the probabilities of all modules.
        float max = modules.Select(module => module.prefab.probability).Sum();

        // Generates a random roll within the range of module probabilities.
        float roll = (float)(Map.random.NextDouble() * max);

        // Accumulates probabilities until the roll is reached and collapses the slot with the selected module.
        float p = 0;
        foreach (var candidate in modules)
        {
            p += candidate.prefab.probability;
            if (p >= roll)
            {
                Collapse(candidate);
                return;
            }
        }

        // If no module is selected, collapses the slot with the first module.
        Collapse(modules.First());
    }


    //RemoveModules method removes specified modules from the slot.
    public void RemoveModules(ModuleSet modulesToRemove, bool recursive = true)
    {
        // Intersect modulesToRemove with the slot's current modules.
        modulesToRemove.Intersect(modules);

        // Records the removal action if there is a history of map modifications.
        if (map.history != null && map.history.Any())
        {
            var item = map.history.Peek();
            if (!item.removedModules.ContainsKey(position))
            {
                item.removedModules[position] = new ModuleSet();
            }
            item.removedModules[position].Add(modulesToRemove);
        }

        // Iterate through each direction to check neighboring slots for possible module removals.
        for (int i = 0; i < 4; i++)
        {
            int inverse = (i + 2) % 4;
            var neighbour = GetNeighbour(i);
            if (neighbour == null || neighbour.Forgotten)
            {
#if UNITY_EDITOR
                // Check if the neighbor is outside of the map's range limit
                if ((map as InfiniteMap).IsOutsideOfRangeLimit(position + Directions.Direction[i]))
                {
                    (map as InfiniteMap).OnHitRangeLimit(position + Directions.Direction[i], modulesToRemove);
                }
#endif
                continue;
            }

            // Iterate through modulesToRemove to adjust neighboring slots' moduleHealth and removalQueue.
            foreach (var mod in modulesToRemove)
            {
                for (int j = 0; j < mod.possibleNeighbours[i].Count; j++)
                {
                    var possibleNeighbour = mod.possibleNeighboursArray[i][j];
                    if (neighbour.moduleHealth[inverse][possibleNeighbour.index] == 1 && neighbour.modules.Contains(possibleNeighbour))
                    {
                        map.removalQueue[neighbour.position].Add(possibleNeighbour);
                    }

#if UNITY_EDITOR
                    // Throw an exception if the neighbor's moduleHealth becomes negative.
                    if (neighbour.moduleHealth[inverse][possibleNeighbour.index] < 1)
                    {
                        throw new System.InvalidOperationException("ModuleHealth must not be negative. " + position + "d: " + i);
                    }
#endif
                    neighbour.moduleHealth[inverse][possibleNeighbour.index]--;
                }
            }
        }

        // Remove modulesToRemove from the slot.
        modules.Remove(modulesToRemove);

        // Throw a CollapseFailedException if the slot becomes empty after removal.
        if (modules.Empty)
        {
            throw new CollapseFailedException(this);
        }

        // If recursive is true, finish removal actions in neighboring slots.
        if (recursive)
        {
            map.FinishRemovalQueue();
        }
    }



    //adds specified modules to the slot.
    public void AddModules(ModuleSet modulesToAdd)
    {
        foreach (var mod in modulesToAdd)
        {
            // Skip adding if the module already exists in the slot or if it is the collapsed module.
            if (modules.Contains(mod) || mod == this.module)
            {
                continue;
            }

            // Adjusts neighboring slots' moduleHealth based on possible connections for the module.
            for (int i = 0; i < 4; i++)
            {
                int inverse = (i + 2) % 4;
                var neighbour = GetNeighbour(i);
                if (neighbour == null || neighbour.Forgotten)
                {
                    continue;
                }
                foreach (var possibleNeighbour in mod.possibleNeighbours[i])
                {
                    neighbour.moduleHealth[inverse][possibleNeighbour.index]++;
                }
            }

            // Adds the module to the slot's modules set.
            modules.Add(mod);
        }

        // If the slot was collapsed and becomes non-empty, notifies the map that the collapse has been undone.
        if (Collapsed && !modules.Empty)
        {
            module = null;
            map.NotifySlotCollapseUndone(this);
        }
    }

    //Removes modules that do not fit the specified connector in the given direction.
    public void EnforceConnector(int direction, int connector)
    {
        var toRemove = modules.Where(module => !module.Fits(direction, connector));
        RemoveModules(ModuleSet.FromEnumerable(toRemove));
    }


    //Removes modules that fit the specified connector in the given direction
    public void ExcludeConnector(int direction, int connector)
    {
        var toRemove = modules.Where(module => module.Fits(direction, connector));
        RemoveModules(ModuleSet.FromEnumerable(toRemove));
    }

    //Clears moduleHealth and modules to mark the slot as forgotten.
    public void Forget()
    {
        moduleHealth = null;
        modules = null;
    }

    //Indicates whether the slot is forgotten (modules and moduleHealth are null).    
    public bool Forgotten
    {
        get
        {
            return modules == null;
        }
    }

}
