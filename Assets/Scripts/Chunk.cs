using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public readonly Bounds bounds;

    public List<Renderer> renderers;
    public List<GameObject> gameObjects;

    public bool cellsVisible = true;

    private readonly Dictionary<Vector3Int, Renderer[]> renderersByLocation;

    public bool InRenderRange
    {
        get;
        private set;
    }

    public Chunk(Bounds bounds)
    {
        this.bounds = bounds;
        this.renderers = new List<Renderer>();
        this.renderersByLocation = new Dictionary<Vector3Int, Renderer[]>();
        this.gameObjects = new List<GameObject>();
        this.InRenderRange = true;
    }

    public void SetInRange(bool value)
    {
        this.InRenderRange = value;
        foreach (var gameObject in this.gameObjects)
        {
            gameObject.SetActive(value);
        }
    }

    public void SetVisibility(bool value)
    {
        if (this.cellsVisible == value)
        {
            return;
        }

        foreach (var renderer in this.renderers)
        {
            renderer.shadowCastingMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        this.cellsVisible = value;
    }

    public void AddBlock(Cell cell)
    {
        if (this.renderersByLocation.ContainsKey(cell.position))
        {
            foreach (var renderer in this.renderersByLocation[cell.position])
            {
                this.renderers.Remove(renderer);
            }
        }
        var renderersLocal = cell.gameObject.GetComponentsInChildren<Renderer>();
        this.renderersByLocation[cell.position] = renderersLocal;
        this.renderers.AddRange(renderersLocal);
        this.cellsVisible = true;
        this.gameObjects.Add(cell.gameObject);
        cell.gameObject.SetActive(this.InRenderRange);
    }

    public void RemoveBlock(Cell cell)
    {
        if (!this.renderersByLocation.ContainsKey(cell.position))
        {
            return;
        }
        foreach (var renderer in this.renderersByLocation[cell.position])
        {
            this.renderers.Remove(renderer);
        }
        this.gameObjects.Remove(cell.gameObject);
    }
}
