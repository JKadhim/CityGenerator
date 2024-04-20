using System.Collections.Generic;
using UnityEngine;

//Represents an item in the history of slot modifications
public class HistoryItem
{
    public Dictionary<Vector3Int, ModuleSet> removedModules;

    public readonly Slot slot;

    public HistoryItem(Slot slot)
    {
        this.removedModules = new Dictionary<Vector3Int, ModuleSet>();
        this.slot = slot;
    }
}