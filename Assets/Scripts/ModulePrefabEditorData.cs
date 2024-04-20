using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This class manages data required for editing module prefabs.
public class ModulePrefabEditorData
{
    public readonly ModulePrefab modulePrefab;

    private readonly ModulePrefab[] prefabs;

    private readonly Dictionary<ModulePrefab, Mesh> meshes;

    public readonly struct ConnectorHint
    {
        public readonly Mesh mesh;
        public readonly int rotation;

        public ConnectorHint(int rotation, Mesh mesh)
        {
            this.rotation = rotation;
            this.mesh = mesh;
        }
    }

    public ModulePrefabEditorData(ModulePrefab modulePrefab)
    {
        this.modulePrefab = modulePrefab;
        this.prefabs = modulePrefab.transform.parent.GetComponentsInChildren<ModulePrefab>();
        this.meshes = new Dictionary<ModulePrefab, Mesh>();
    }

    // Retrieves the mesh for a given module prefabObject, caching it if necessary.
    private Mesh GetMesh(ModulePrefab modulePrefab)
    {
        if (this.meshes.ContainsKey(modulePrefab))
        {
            return this.meshes[modulePrefab];
        }
        var mesh = modulePrefab.GetMesh(false);
        this.meshes[modulePrefab] = mesh;
        return mesh;
    }

    // Retrieves the connector hint for a given direction.
    public ConnectorHint GetConnectorHint(int direction)
    {
        var face = this.modulePrefab.Faces[direction];

        // Check if the face is a horizontal face.
        if (face is ModulePrefab.HorizontalFaceDetails)
        {
            var horizontalFace = face as ModulePrefab.HorizontalFaceDetails;

            // Iterate through all module prefabs in the scene.
            foreach (var prefab in this.prefabs)
            {
                // Skip the current module prefabObject or if it's excluded from being a neighbor.
                if (prefab == this.modulePrefab || face.excludedNeighbours.Contains(prefab))
                {
                    continue;
                }

                // Check rotations of the other module prefabObject to find a matching connector.
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    // Get the face details of the other module prefabObject at the rotated direction.
                    var otherFace = prefab.Faces[Orientations.Rotate(direction, rotation + 2)] as ModulePrefab.HorizontalFaceDetails;

                    // Skip if the other module prefabObject excludes the current module prefabObject.
                    if (otherFace.excludedNeighbours.Contains(this.modulePrefab))
                    {
                        continue;
                    }

                    // Check if the connectors match and if the faces are compatible.
                    if (otherFace.connector == face.connector && ((horizontalFace.symmetric && otherFace.symmetric) || otherFace.flipped != horizontalFace.flipped))
                    {
                        // Return the connector hint with the rotation and associated mesh.
                        return new ConnectorHint(rotation, this.GetMesh(prefab));
                    }
                }
            }
        }
        // If no suitable connector hint is found, return an empty hint.
        return new ConnectorHint();
    }

}

