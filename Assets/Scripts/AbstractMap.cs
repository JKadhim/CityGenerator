using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class AbstractMap
{
    public const float BLOCK_SIZE = 2f;
    public const int HISTORY_SIZE = 3000;

    public static System.Random random;

    public readonly RingBuffer<HistoryItem> history;
    public readonly QueueDictionary<Vector3Int, ModuleSet> removalQueue;
    private HashSet<Slot> workArea;
    public readonly Queue<Slot> buildQueue;

    private int backtrackBarrier;
    private int backtrackAmount = 0;

    private readonly short[][] initialModuleHealth;

    public short[][] InitialModuleHealth => initialModuleHealth;

    protected AbstractMap()
    {
        random = new System.Random();

        history = new RingBuffer<HistoryItem>(HISTORY_SIZE)
        {
            OnOverflow = item => item.slot.Forget()
        };
        removalQueue = new QueueDictionary<Vector3Int, ModuleSet>(() => new ModuleSet());
        buildQueue = new Queue<Slot>();

        initialModuleHealth = CreateInitialModuleHealth(ModuleData.current);

        backtrackBarrier = 0;
    }

    public abstract Slot GetSlot(Vector3Int position);

    public abstract IEnumerable<Slot> GetAllSlots();

    public abstract void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints);

    // Method to notify that a slot has collapsed
    public void NotifySlotCollapsed(Slot slot)
    {
        workArea?.Remove(slot);
        buildQueue.Enqueue(slot);
    }

    // Method to notify that the collapse of a slot has been undone
    public void NotifySlotCollapseUndone(Slot slot)
    {
        workArea?.Add(slot);
    }

    // Method to finish the removal queue
    public void FinishRemovalQueue()
    {
        while (removalQueue.Any())
        {
            var kvp = removalQueue.Dequeue();
            var slot = GetSlot(kvp.Key);
            if (!slot.Collapsed)
            {
                slot.RemoveModules(kvp.Value, false);
            }
        }
    }

    // Method to enforce a walkway in a specific direction from a start position
    public void EnforceWalkway(Vector3Int start, int direction)
    {
        var slot = GetSlot(start);
        var toRemove = slot.modules.Where(module => !module.GetFace(direction).walkable);
        slot.RemoveModules(ModuleSet.FromEnumerable(toRemove));
    }

    // Method to enforce a walkway between two positions
    public void EnforceWalkway(Vector3Int start, Vector3Int destination)
    {
        int direction = Orientations.GetIndex((Vector3)(destination - start));
        EnforceWalkway(start, direction);
        EnforceWalkway(destination, (direction + 2) % 4);
    }



    // Collapses a set of targets in the map, simulating the process of procedural generation.
    public void Collapse(IEnumerable<Vector3Int> targets, bool showProgress = false)
    {
#if UNITY_EDITOR
        try
        {
#endif
            // Clear the removal queue and initialize the work area with non-collapsed slots from the targets
            removalQueue.Clear();
            workArea = new HashSet<Slot>(targets.Select(target => GetSlot(target)).Where(slot => slot != null && !slot.Collapsed));

            // Collapse slots until work area is empty
            while (workArea.Any())
            {
                float minEntropy = float.PositiveInfinity;
                Slot selected = null;

                // Find the slot with the minimum entropy among the work area
                foreach (var slot in workArea)
                {
                    float entropy = slot.modules.Entropy;
                    if (entropy < minEntropy)
                    {
                        selected = slot;
                        minEntropy = entropy;
                    }
                }

                try
                {
                    // Attempt to collapse the selected slot randomly
                    selected.CollapseRandom();
                }
                catch (CollapseFailedException)
                {
                    // If collapse fails, clear the removal queue and attempt to backtrack
                    removalQueue.Clear();
                    if (history.TotalCount > backtrackBarrier)
                    {
                        backtrackBarrier = history.TotalCount;
                        backtrackAmount = 2;
                    }
                    else
                    {
                        backtrackAmount *= 2;
                    }
                    if (backtrackAmount > 0)
                    {
                        Debug.Log(history.Count + " Backtracking " + backtrackAmount + " steps...");
                    }
                    Undo(backtrackAmount);
                }

#if UNITY_EDITOR
                // If showProgress is true, update progress bar in Unity Editor
                if (showProgress && workArea.Count % 20 == 0 && EditorUtility.DisplayCancelableProgressBar("Collapsing area... ", workArea.Count +
                    " left...", 1f - (float)workArea.Count / targets.Count()))
                {
                    EditorUtility.ClearProgressBar();
                    throw new Exception("Map generation cancelled.");
                }
#endif
            }

#if UNITY_EDITOR
            // If showProgress is true, clear progress bar in Unity Editor after completion
            if (showProgress)
            {
                EditorUtility.ClearProgressBar();
            }
        }
        catch (Exception exception)
        {
            // Catch and handle exceptions, clear progress bar if necessary, and log warning
            if (showProgress)
            {
                EditorUtility.ClearProgressBar();
            }
            Debug.LogWarning("Exception in world generation thread at" + exception.StackTrace);
            throw exception;
        }
#endif
    }

    // Collapses a rectangular area starting from the specified position with the given size.
    public void Collapse(Vector3Int start, Vector3Int size, bool showProgress = false)
    {
        var targets = new List<Vector3Int>();

        // Iterate over each coordinate within the specified size
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    // Add the current position to the targets list
                    targets.Add(start + new Vector3Int(x, y, z));
                }
            }
        }

        // Collapse the targets using the Collapse method
        Collapse(targets, showProgress);
    }

    // Undoes a specified number of steps in the history of collapsed slots.
    public void Undo(int steps)
    {
        // Iterate until the specified number of steps is reached or history is empty
        while (steps > 0 && history.Any())
        {
            // Pop the latest history item
            var item = history.Pop();

            // Restore the removed modules to their respective slots
            foreach (var slotAddress in item.removedModules.Keys)
            {
                GetSlot(slotAddress).AddModules(item.removedModules[slotAddress]);
            }

            // Reset the module reference of the slot and notify collapse undone
            item.slot.module = null;
            NotifySlotCollapseUndone(item.slot);
            steps--;
        }

        // If history is empty, reset the backtrack barrier
        if (history.Count == 0)
        {
            backtrackBarrier = 0;
        }
    }

    // Creates an initial health for modules based on their possible neighbors.
    private short[][] CreateInitialModuleHealth(Module[] modules)
    {
        var initModuleHealth = new short[4][];

        // Iterate over each direction
        for (int i = 0; i < 4; i++)
        {
            initModuleHealth[i] = new short[modules.Length];

            // Iterate over each module
            foreach (var module in modules)
            {
                // Count the number of possible neighbors in the opposite direction
                foreach (var possibleNeighbor in module.possibleNeighbours[(i + 2) % 4])
                {
                    initModuleHealth[i][possibleNeighbor.index]++;
                }
            }
        }

#if UNITY_EDITOR
        // Validate the initial health matrix
        for (int i = 0; i < modules.Length; i++)
        {
            for (int d = 0; d < 4; d++)
            {
                if (initModuleHealth[d][i] == 0)
                {
                    // Log error if a module cannot be reached from a direction
                    Debug.LogError("Module " + modules[i].name + " cannot be reached from direction " + d + " (" + modules[i].GetFace(d).ToString() + ")!", modules[i].prefabObject);
                    throw new Exception("Unreachable module.");
                }
            }
        }
#endif

        return initModuleHealth;
    }

    // Creates a copy of the initial module health matrix.
    public short[][] CopyInitialModuleHealth()
    {
        return InitialModuleHealth.Select(a => a.ToArray()).ToArray();
    }

}