using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "CityGenerator/ModuleData", fileName = "modules.asset")]
public class ModuleData : ScriptableObject, ISerializationCallbackReceiver
{
    public static Module[] current;

    public GameObject prefabs;

    public Module[] modules;

#if UNITY_EDITOR
    // Method to simplify neighbour data of modules
    public void SimplifyNeighbourData()
    {
        ModuleData.current = this.modules;
        const int height = 12;
        int count = 0;
        var center = new Vector3Int(0, height / 2, 0);

        int p = 0;

        // Iterate over modules
        foreach (var module in this.modules)
        {
            // Create a new InfiniteMap for each module
            var map = new InfiniteMap(height);
            var slot = map.GetSlot(center);
            try
            {
                // Attempt to collapse the module into the slot
                slot.Collapse(module);
            }
            catch (CollapseFailedException exception)
            {
                // Throw an exception if module collapse fails
                throw new InvalidOperationException("Module " + module.name + " creates a failure at relative position " + (exception.slot.position - center) + ".");
            }
            // Iterate over directions and simplify possible neighbours
            for (int direction = 0; direction < 4; direction++)
            {
                var neighbour = slot.GetNeighbour(direction);
                int unoptimizedNeighbourCount = module.possibleNeighbours[direction].Count;
                module.possibleNeighbours[direction].Intersect(neighbour.modules);
                count += unoptimizedNeighbourCount - module.possibleNeighbours[direction].Count;
            }
            // Convert possible neighbours to arrays
            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
            p++;
            // Display progress bar
            EditorUtility.DisplayProgressBar("Simplifying... " + count, module.name, (float)p / this.modules.Length);
        }
        // Log the number of removed impossible neighbours
        Debug.Log("Removed " + count + " impossible neighbours.");

        // Clear progress bar and mark scene as dirty
        EditorUtility.ClearProgressBar();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    // Method to get module prefabs from the prefabs transform
    private IEnumerable<ModulePrefab> GetPrefabs()
    {
        // Iterate over child transforms of prefabs
        foreach (Transform transform in this.prefabs.transform)
        {
            var item = transform.GetComponent<ModulePrefab>();
            // Yield enabled module prefabs
            if (item != null && item.enabled)
            {
                Debug.Log("got");
                yield return item;
            }
        }
    }


    // Method to create modules based on prefabs
    public void CreateModules(bool respectNeigbourExclusions = true)
    {
        int count = 0;
        var modulesLocal = new List<Module>();

        // Get module prefabs from the prefabs transform
        var prefabsLocal = this.GetPrefabs().ToArray();

        // Dictionary to map modules to their prefabs in the scene
        var scenePrefab = new Dictionary<Module, ModulePrefab>();
        // Iterate over module prefabs
        for (int i = 0; i < prefabsLocal.Length; i++)
        {
            var prefab = prefabsLocal[i];
            // Initialize excluded neighbours for each face
            for (int face = 0; face < 4; face++)
            {
                if (prefab.Faces[face].excludedNeighbours == null)
                {
                    prefab.Faces[face].excludedNeighbours = new ModulePrefab[0];
                }
            }

            // Create module instances for each rotation variant
            for (int rotation = 0; rotation < 4; rotation++)
            {
                if (rotation == 0 || !prefab.CompareRotatedVariants(0, rotation))
                {
                    var module = new Module(prefab.gameObject, rotation, count);
                    modulesLocal.Add(module);
                    scenePrefab[module] = prefab;
                    count++;
                }
            }

            EditorUtility.DisplayProgressBar("Creating module prefabs...", prefab.gameObject.name, (float)i / prefabsLocal.Length);
        }

        // Set current module data to the created modules
        ModuleData.current = modulesLocal.ToArray();

        if (ModuleData.current != null) { Debug.Log("exists 1"); }

        // Populate possible neighbours for each module
        foreach (var module in modulesLocal)
        {
            module.possibleNeighbours = new ModuleSet[4];
            for (int direction = 0; direction < 4; direction++)
            {
                var face = scenePrefab[module].Faces[Orientations.Rotate(direction, module.rotation)];
                module.possibleNeighbours[direction] = new ModuleSet(modulesLocal
                    .Where(neighbor => module.Fits(direction, neighbor)
                        && (!respectNeigbourExclusions || (
                            !face.excludedNeighbours.Contains(scenePrefab[neighbor])
                            && !scenePrefab[neighbor].Faces[Orientations.Rotate((direction + 2) % 4, neighbor.rotation)].excludedNeighbours.Contains(scenePrefab[module]))
                            && (!face.enforceWalkableNeighbor || scenePrefab[neighbor].Faces[Orientations.Rotate((direction + 2) % 4, neighbor.rotation)].walkable)
                            && (face.walkable || !scenePrefab[neighbor].Faces[Orientations.Rotate((direction + 2) % 4, neighbor.rotation)].enforceWalkableNeighbor))
                    ));
            }

            // Convert possible neighbours to arrays
            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
        }
        Debug.Log("exit");
        // Clear progress bar
        EditorUtility.ClearProgressBar();

        // Set modules array and mark scene as dirty
        this.modules = modulesLocal.ToArray();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

#endif

    public void OnBeforeSerialize() { }


    // Method invoked after deserialization
    public void OnAfterDeserialize()
    {
        // Set current module data to deserialized modules
        ModuleData.current = this.modules;
        // Iterate over modules and convert possible neighbours to arrays
        foreach (var module in this.modules)
        {
            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
        }
    }

    // Method to save module prefabs (Editor only)
    public void SavePrefabs()
    {
#if UNITY_EDITOR
        // Mark prefabs as dirty and save assets
        EditorUtility.SetDirty(this.prefabs);
        AssetDatabase.SaveAssets();
        // Set module prefabs based on prefabObject components
        foreach (var module in this.modules)
        {
            module.prefab = module.prefabObject.GetComponent<ModulePrefab>();
        }
#endif
    }

}
