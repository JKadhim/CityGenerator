using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MapBehaviour : MonoBehaviour
{
    public InfiniteMap map;

    public int mapHeight = 6;

    public BoundaryConstraint[] boundaryConstraints;

    public bool applyBoundaryConstraints = true;

    public ModuleData moduleData;

    private CullingData cullingData;

    // Method to get world space position from map position
    public Vector3 GetWorldSpacePosition(Vector3Int position)
    {
        return this.transform.position
            + Vector3.up * InfiniteMap.BLOCK_SIZE / 2f
            + position.ToVector3() * InfiniteMap.BLOCK_SIZE;
    }

    // Method to get map position from world space position
    public Vector3Int GetMapPosition(Vector3 worldSpacePosition)
    {
        var pos = (worldSpacePosition - this.transform.position) / InfiniteMap.BLOCK_SIZE;
        return Vector3Int.FloorToInt(pos + new Vector3(0.5f, 0, 0.5f));
    }

    // Method to clear the map and associated game objects
    public void ClearMap()
    {
        // Collect all child transforms
        var children = new List<Transform>();
        foreach (Transform child in this.transform)
        {
            children.Add(child);
        }
        // Destroy all child game objects
        foreach (var child in children)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
        // Reset map reference
        this.map = null;
    }

    // Method to initialize the map
    public void InitializeMap()
    {
        // Set current module data to module list
        ModuleData.current = this.moduleData.modules;
        
        // ClearMap existing map and create a new InfiniteMap instance
        this.ClearMap();

        this.map = new InfiniteMap(this.mapHeight);
        // Apply boundary constraints if specified
        if (this.applyBoundaryConstraints && this.boundaryConstraints != null && this.boundaryConstraints.Any())
        {
            this.map.ApplyBoundaryConstraints(this.boundaryConstraints);
        }
        // InitializeMap culling data component
        this.cullingData = this.GetComponent<CullingData>();
        this.cullingData.Initialize();
    }


    // Property to check if the map is initialized
    public bool Initialized
    {
        get
        {
            return this.map != null;
        }
    }

    public void Update()
    {
        // If map is not initialized or build queue is null, return
        if (this.map == null || this.map.buildQueue == null)
        {
            return;
        }

        // Process a limited number of items from the build queue each frame
        int itemsLeft = 50;
        while (this.map.buildQueue.Count != 0 && itemsLeft > 0)
        {
            var slot = this.map.buildQueue.Peek();
            if (slot == null)
            {
                return;
            }
            // Build the slot and decrement the items left counter
            if (this.BuildSlot(slot))
            {
                itemsLeft--;
            }
            this.map.buildQueue.Dequeue();
        }

        // ClearMap outdated slots in the culling data
        this.cullingData.ClearOutdatedSlots();
    }

    // Method to build a slot
    public bool BuildSlot(Slot slot)
    {
        // If slot's game object exists, remove it from culling data and destroy it
        if (slot.gameObject != null)
        {
            this.cullingData.RemoveSlot(slot);
#if UNITY_EDITOR
            GameObject.DestroyImmediate(slot.gameObject);
#else
            GameObject.Destroy(slot.GameObject);
#endif
        }

        // If slot is not collapsed or its module should not spawn, return false
        if (!slot.Collapsed || !slot.module.prefab.spawn)
        {
            return false;
        }

        // Instantiate the module's game object, set its position and rotation, and add it to culling data
        var module = slot.module;
        if (module == null)
        {
            return false;
        }
        var gameObject = GameObject.Instantiate(module.prefab.gameObject);
        gameObject.name = module.prefab.gameObject.name + " " + slot.position;
        GameObject.DestroyImmediate(gameObject.GetComponent<ModulePrefab>());
        gameObject.transform.parent = this.transform;
        gameObject.transform.SetPositionAndRotation(this.GetWorldSpacePosition(slot.position), Quaternion.Euler(90f * module.rotation * Vector3.up));
        slot.gameObject = gameObject;
        this.cullingData.AddSlot(slot);
        return true;
    }

    // Method to build all slots in the build queue
    public void BuildAllSlots()
    {
        while (this.map.buildQueue.Count != 0)
        {
            this.BuildSlot(this.map.buildQueue.Dequeue());
        }
    }


    public bool VisualizeSlots = false;

#if UNITY_EDITOR
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawGizmo(MapBehaviour mapBehaviour, GizmoType gizmoType)
    {
        if (!mapBehaviour.VisualizeSlots)
        {
            return;
        }
        if (mapBehaviour.map == null)
        {
            return;
        }
        foreach (var slot in mapBehaviour.map.GetAllSlots())
        {
            if (slot.Collapsed || slot.modules.Count == ModuleData.current.Length)
            {
                continue;
            }
            Handles.Label(mapBehaviour.GetWorldSpacePosition(slot.position), slot.modules.Count.ToString());
        }
    }
#endif
}
