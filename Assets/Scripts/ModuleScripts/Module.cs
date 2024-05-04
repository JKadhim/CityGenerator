using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

[System.Serializable]
public class Module
{
    public string name;

    public ModulePrefab prefab;
    public GameObject prefabObject;

    public int rotation;

    public ModuleSet[] possibleNeighbours;
    public Module[][] possibleNeighboursArray;

    [HideInInspector]
    public int index;

    // This is precomputed to make entropy calculation faster
    public float log;

    public Module(GameObject prefab, int rotation, int index)
    {
        this.rotation = rotation;
        this.index = index;
        this.prefabObject = prefab;
        this.prefab = this.prefabObject.GetComponent<ModulePrefab>();
        this.name = this.prefab.gameObject.name + " R" + rotation;
        this.log = this.prefab.probability * Mathf.Log(this.prefab.probability);
    }

    public bool Fits(int direction, Module module)
    {
        int oppositeDirection = (direction + 3) % 6;

        if (Directions.IsHorizontal(direction))
        {
            var face1 = this.prefab.Faces[Directions.Rotate(direction, this.rotation)] as ModulePrefab.Horizontal;
            var face2 = module.prefab.Faces[Directions.Rotate(oppositeDirection, module.rotation)] as ModulePrefab.Horizontal;
            return face1.connector == face2.connector && (face1.symmetric || face1.flipped != face2.flipped);
        }
        else
        {
            var face1 = this.prefab.Faces[direction] as ModulePrefab.Vertical;
            var face2 = module.prefab.Faces[oppositeDirection] as ModulePrefab.Vertical;
            return face1.connector == face2.connector && (face1.@fixed || (face1.rotation + this.rotation) % 4 == (face2.rotation + module.rotation) % 4);
        }
    }

    public bool Fits(int direction, int connector)
    {
        if (Directions.IsHorizontal(direction))
        {
            var face = this.GetFace(direction) as ModulePrefab.Horizontal;
            return face.connector == connector;
        }
        else
        {
            var face = this.prefab.Faces[direction] as ModulePrefab.Vertical;
            return face.connector == connector;
        }
    }

    public ModulePrefab.FaceDetails GetFace(int direction)
    {
        return this.prefab.Faces[Directions.Rotate(direction, this.rotation)];
    }

    public override string ToString()
    {
        return this.name;
    }
}
