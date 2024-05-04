using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

[CreateAssetMenu(menuName = "City Generator/Module Data", fileName = "Module Data.asset")]
public class ModuleData : ScriptableObject, ISerializationCallbackReceiver
{
    public static Module[] current;

    public GameObject prefabs;

    public Module[] modules;

#if UNITY_EDITOR
    public void SimplifyNeighbour()
    {
        ModuleData.current = this.modules;
        const int height = 12;
        int count = 0;
        var centre = new Vector3Int(0, height / 2, 0);

        int p = 0;
        foreach (var module in this.modules)
        {
            var map = new InfiniteMap(height);
            var cell = map.GetCell(centre);
            try
            {
                cell.Collapse(module);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Module " + module.name + " creates a failure at " + (exception.cell.position - centre));
            }
            for (int direction = 0; direction < 6; direction++)
            {
                var neighbour = cell.GetNeighbour(direction);
                int unoptimizedNeighbourCount = module.possibleNeighbours[direction].Count;
                module.possibleNeighbours[direction].Intersect(neighbour.modules);
                count += unoptimizedNeighbourCount - module.possibleNeighbours[direction].Count;
            }
            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
            p++;
            EditorUtility.DisplayProgressBar("Simplifying... " + count, module.name, (float)p / this.modules.Length);
        }
        Debug.Log("Removed " + count + " impossible neighbours.");
        EditorUtility.ClearProgressBar();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }



    private IEnumerable<ModulePrefab> GetPrefabs()
    {
        foreach (Transform t in this.prefabs.transform)
        {
            var item = t.GetComponent<ModulePrefab>();
            if (item != null && item.enabled)
            {
                yield return item;
            }
        }
    }

    public void CreateModules(bool respectNeigbourExclusions = true)
    {
        int count = 0;
        var modulesLocal = new List<Module>();

        var prefabsLocal = this.GetPrefabs().ToArray();

        var prefabDict = new Dictionary<Module, ModulePrefab>();

        for (int i = 0; i < prefabsLocal.Length; i++)
        {
            var prefab = prefabsLocal[i];
            for (int face = 0; face < 6; face++)
            {
                if (prefab.Faces[face].excludedNeighbours == null)
                {
                    prefab.Faces[face].excludedNeighbours = new ModulePrefab[0];
                }
            }

            for (int rotation = 0; rotation < 4; rotation++)
            {
                if (rotation == 0 || !prefab.CompareRotated(0, rotation))
                {
                    var module = new Module(prefab.gameObject, rotation, count);
                    modulesLocal.Add(module);
                    prefabDict[module] = prefab;
                    count++;
                }
            }

            EditorUtility.DisplayProgressBar("Creating module prefabs...", prefab.gameObject.name, (float)i / prefabsLocal.Length);
        }

        ModuleData.current = modulesLocal.ToArray();

        foreach (var module in modulesLocal)
        {
            module.possibleNeighbours = new ModuleSet[6];
            for (int direction = 0; direction < 6; direction++)
            {
                var face = prefabDict[module].Faces[Directions.Rotate(direction, module.rotation)];
                module.possibleNeighbours[direction] = new ModuleSet(modulesLocal
                    .Where(neighbour => module.Fits(direction, neighbour)
                        && (!respectNeigbourExclusions || (
                            !face.excludedNeighbours.Contains(prefabDict[neighbour])
                            && !prefabDict[neighbour].Faces[Directions.Rotate((direction + 3) % 6, neighbour.rotation)].excludedNeighbours.Contains(prefabDict[module]))
                            && (!face.enforceWalkableNeighbor || prefabDict[neighbour].Faces[Directions.Rotate((direction + 3) % 6, neighbour.rotation)].walkable)
                            && (face.walkable || !prefabDict[neighbour].Faces[Directions.Rotate((direction + 3) % 6, neighbour.rotation)].enforceWalkableNeighbor))
                    ));
            }

            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
        }
        EditorUtility.ClearProgressBar();

        this.modules = modulesLocal.ToArray();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
#endif

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        ModuleData.current = this.modules;
        foreach (var module in this.modules)
        {
            module.possibleNeighboursArray = module.possibleNeighbours.Select(ms => ms.ToArray()).ToArray();
        }
    }
}
