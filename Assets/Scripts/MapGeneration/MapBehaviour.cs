using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class MapBehaviour : MonoBehaviour
{
    public InfiniteMap map;

    public int mapHeight = 6;

    public Constraints[] constraints;

    public bool applyConstraints = true;

    public ModuleData moduleData;

    private CullingData cullingData;

    public Vector3 GetWorldPosition(Vector3Int position)
    {
        return this.transform.position
            + Vector3.up * InfiniteMap.BLOCK_SIZE / 2f
            + position.ToVector3() * InfiniteMap.BLOCK_SIZE;
    }

    public Vector3Int GetMapPosition(Vector3 worldPosition)
    {
        var pos = (worldPosition - this.transform.position) / InfiniteMap.BLOCK_SIZE;
        return Vector3Int.FloorToInt(pos + new Vector3(0.5f, 0, 0.5f));
    }

    public void Clear()
    {
        var children = new List<Transform>();
        foreach (Transform child in this.transform)
        {
            children.Add(child);
        }
        foreach (var child in children)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
        this.map = null;
    }

    public void Initialize()
    {
        ModuleData.current = this.moduleData.modules;
        this.Clear();
        this.map = new InfiniteMap(this.mapHeight);
        if (this.applyConstraints && this.constraints != null && this.constraints.Any())
        {
            this.map.ApplyConstraints(this.constraints);
        }
        this.cullingData = this.GetComponent<CullingData>();
        this.cullingData.Initialize();
    }

    public bool Initialized
    {
        get
        {
            return this.map != null;
        }
    }

    public void Update()
    {
        if (this.map == null || this.map.buildQueue == null)
        {
            return;
        }

        int itemsLeft = 50;

        while (this.map.buildQueue.Count != 0 && itemsLeft > 0)
        {
            var cell = this.map.buildQueue.Peek();
            if (cell == null)
            {
                return;
            }
            if (this.BuildCell(cell))
            {
                itemsLeft--;
            }
            this.map.buildQueue.Dequeue();
        }
        this.cullingData.ClearOldCells();
    }

    public bool BuildCell(Cell cell)
    {
        if (cell.gameObject != null)
        {
            this.cullingData.RemoveCell(cell);
#if UNITY_EDITOR
            GameObject.DestroyImmediate(cell.gameObject);
#endif
        }

        if (!cell.Collapsed || !cell.module.prefab.spawn)
        {
            return false;
        }
        var module = cell.module;
        if (module == null)
        {
            return false;
        }

        var obj = GameObject.Instantiate(module.prefab.gameObject);
        
        obj.name = module.prefab.gameObject.name + " " + cell.position;
        GameObject.DestroyImmediate(obj.GetComponent<ModulePrefab>());
        obj.transform.parent = this.transform;
        obj.transform.SetPositionAndRotation(this.GetWorldPosition(cell.position),
            Quaternion.Euler(90f * module.rotation * Vector3.up));
        
        cell.gameObject = obj;
        this.cullingData.AddCell(cell);
        return true;
    }

    public void BuildAllCells()
    {
        while (this.map.buildQueue.Count != 0)
        {
            this.BuildCell(this.map.buildQueue.Dequeue());
        }
    }
}
