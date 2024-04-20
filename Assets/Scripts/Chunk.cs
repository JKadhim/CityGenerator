using System.Collections.Generic;
using UnityEngine;


//Represents a chunk of the map

public class Chunk
{
    public readonly Bounds bounds;

    public List<Renderer> renderers;
    public List<GameObject> gameObjects;
    public List<Room> rooms;

    // Dictionary to store renderers by position for efficient access
    private readonly Dictionary<Vector3Int, Renderer[]> renderersByPosition;

    // Indicates whether exterior blocks are visible
    public bool exteriorBlocksVisible = true;

    // Indicates whether the chunk is in render range
    public bool InRenderRange { get; private set; }

    public Chunk(Bounds bounds)
    {
        this.bounds = bounds;
        renderers = new List<Renderer>();
        rooms = new List<Room>();
        renderersByPosition = new Dictionary<Vector3Int, Renderer[]>();
        gameObjects = new List<GameObject>();
        InRenderRange = true;
    }

    // Sets the in render range flag and updates the visibility of game objects and slots accordingly
    public void SetInRenderRange(bool value)
    {
        InRenderRange = value;
        foreach (var gameObject in gameObjects)
        {
            gameObject.SetActive(value);
        }
        foreach (var room in rooms)
        {
            foreach (var slot in room.Slots)
            {
                slot.gameObject.SetActive(value);
            }
        }
    }

    // Sets the visibility of exterior blocks
    public void SetExteriorVisibility(bool value)
    {
        if (exteriorBlocksVisible == value)
        {
            return;
        }
        foreach (var renderer in renderers)
        {
            renderer.shadowCastingMode = value ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
        exteriorBlocksVisible = value;
    }

    // Sets the visibility of rooms
    public void SetRoomVisibility(bool value)
    {
        foreach (var room in rooms)
        {
            room.SetVisibility(value);
        }
    }

    // Adds a block (slot) to the chunk
    public void AddBlock(Slot slot)
    {
        if (renderersByPosition.ContainsKey(slot.position))
        {
            foreach (var renderer in renderersByPosition[slot.position])
            {
                renderers.Remove(renderer);
            }
        }
        var renderersVar = slot.gameObject.GetComponentsInChildren<Renderer>();
        renderersByPosition[slot.position] = renderersVar;
        renderers.AddRange(renderersVar);
        exteriorBlocksVisible = true;
        gameObjects.Add(slot.gameObject);
        slot.gameObject.SetActive(InRenderRange);
    }

    // Removes a block (slot) from the chunk
    public void RemoveBlock(Slot slot)
    {
        if (!renderersByPosition.ContainsKey(slot.position))
        {
            return;
        }
        foreach (var renderer in renderersByPosition[slot.position])
        {
            renderers.Remove(renderer);
        }
        gameObjects.Remove(slot.gameObject);
    }
}

