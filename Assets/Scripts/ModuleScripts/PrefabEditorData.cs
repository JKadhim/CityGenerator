using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PrefabEditorData
{
    public readonly ModulePrefab ModulePrefab;

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

    public PrefabEditorData(ModulePrefab prefab)
    {
        this.ModulePrefab = prefab;
        this.prefabs = prefab.transform.parent.GetComponentsInChildren<ModulePrefab>();
        this.meshes = new Dictionary<ModulePrefab, Mesh>();
    }

    private Mesh GetMesh(ModulePrefab prefab)
    {
        if (this.meshes.ContainsKey(prefab))
        {
            return this.meshes[prefab];
        }
        var mesh = prefab.GetMesh(false);
        this.meshes[prefab] = mesh;
        return mesh;
    }

    public ConnectorHint GetConnectorHint(int direction)
    {
        var face = this.ModulePrefab.Faces[direction];
        if (face is ModulePrefab.Horizontal)
        {
            var horizontalFace = face as ModulePrefab.Horizontal;

            foreach (var prefab in this.prefabs)
            {
                if (prefab == this.ModulePrefab || face.excludedNeighbours.Contains(prefab))
                {
                    continue;
                }
                for (int rotation = 0; rotation < 4; rotation++)
                {
                    var otherFace = prefab.Faces[Directions.Rotate(direction, rotation + 2)] as ModulePrefab.Horizontal;
                    if (otherFace.excludedNeighbours.Contains(this.ModulePrefab))
                    {
                        continue;
                    }
                    if (otherFace.connector == face.connector && ((horizontalFace.symmetric && otherFace.symmetric) || otherFace.flipped != horizontalFace.flipped))
                    {
                        return new ConnectorHint(rotation, this.GetMesh(prefab));
                    }
                }
            }
        }

        if (face is ModulePrefab.Vertical)
        {
            var verticalFace = face as ModulePrefab.Vertical;

            foreach (var prefab in this.prefabs)
            {
                if (prefab == this.ModulePrefab || face.excludedNeighbours.Contains(prefab))
                {
                    continue;
                }
                var otherFace = prefab.Faces[(direction + 3) % 6] as ModulePrefab.Vertical;
                if (otherFace.excludedNeighbours.Contains(this.ModulePrefab) || otherFace.connector != face.connector)
                {
                    continue;
                }

                return new ConnectorHint(verticalFace.rotation - otherFace.rotation, this.GetMesh(prefab));
            }
        }

        return new ConnectorHint();
    }
}
