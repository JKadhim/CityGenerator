using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavedCell
{
    public Dictionary<Vector3Int, ModuleSet> removedModules;

    public readonly Cell cell;

    public SavedCell(Cell cell)
    {
        this.removedModules = new Dictionary<Vector3Int, ModuleSet>();
        this.cell = cell;
    }
}
