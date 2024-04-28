using UnityEngine;

[System.Serializable]
//Represents a module, which is a piece of content used in map generation.
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

    // The product of the module's probability and the logarithm of its probability
    public float PLogP;

    public Module(GameObject prefab, int rotation, int index)
    {
        this.rotation = rotation;
        this.index = index;
        this.prefabObject = prefab;
        this.prefab = this.prefabObject.GetComponent<ModulePrefab>();
        this.name = this.prefab.gameObject.name + " R" + rotation;
        this.PLogP = this.prefab.probability * Mathf.Log(this.prefab.probability);
    }

    // Checks if this module fits with another module based on a specified direction and the other module
    public bool Fits(int direction, Module module)
    {
        int otherDirection = (direction + 2) % 4;

        var f1 = this.prefab.Faces[Directions.Rotate(direction, this.rotation)] as ModulePrefab.HorizontalFaceDetails;
        var f2 = module.prefab.Faces[Directions.Rotate(otherDirection, module.rotation)] as ModulePrefab.HorizontalFaceDetails;
        return f1.connector == f2.connector && (f1.symmetric || f1.flipped != f2.flipped);
    }

    // Checks if this module fits with another module based on a specified direction and connector
    public bool Fits(int direction, int connector)
    {
        var f = this.GetFace(direction) as ModulePrefab.HorizontalFaceDetails;
        return f.connector == connector;
    }

    // Gets the face details of the module for a specified direction
    public ModulePrefab.FaceDetails GetFace(int direction)
    {
        return this.prefab.Faces[Directions.Rotate(direction, this.rotation)];
    }

    // Overrides the ToString method to return the name of the module
    public override string ToString()
    {
        return this.name;
    }
}
