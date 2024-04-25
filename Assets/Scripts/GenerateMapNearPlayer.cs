using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;

[RequireComponent(typeof(MapBehaviour))]
public class GenerateMapNearPlayer : MonoBehaviour {
    
	// Private class for handling chunk events
    private class ChunkEvents
    {
        public readonly ConcurrentQueue<Vector3Int> CompletedChunks;

        public readonly List<IMapGenerationCallbackReceiver> MapGenerationCallbackReceivers;

        private readonly GenerateMapNearPlayer source;

        public ChunkEvents(GenerateMapNearPlayer source)
        {
            // InitializeMap collections and source reference
            this.CompletedChunks = new ConcurrentQueue<Vector3Int>();
            this.MapGenerationCallbackReceivers = new List<IMapGenerationCallbackReceiver>();
            this.source = source;
        }

        public void Update()
        {
            // Check if any completed chunks are in the queue
            if (!this.CompletedChunks.Any())
            {
                return;
            }
            // Try to dequeue a completed chunk and notify subscribers
            if (this.CompletedChunks.TryDequeue(out Vector3Int completedChunk))
            {
                foreach (var subscriber in this.MapGenerationCallbackReceivers)
                {
                    subscriber.OnGenerateChunk(completedChunk, this.source);
                }
            }
        }
    }


    private MapBehaviour mapBehaviour;
	private InfiniteMap map;

	public Transform Target;

	public int ChunkSize = 4;

	public float Range = 30;

	private Vector3 targetPosition;
	private Vector3 mapPosition;

	private HashSet<Vector3Int> generatedChunks;

	private Thread thread;

	private ChunkEvents chunkEventManager;

    void Start()
    {
        // InitializeMap data structures and references
        this.generatedChunks = new HashSet<Vector3Int>();
        this.mapBehaviour = this.GetComponent<MapBehaviour>();
        this.mapBehaviour.InitializeMap();
        this.map = this.mapBehaviour.map;

        // Generate initial chunks and build slots
        this.Generate();
        this.mapBehaviour.BuildAllSlots();

        // Start the generator thread
        this.thread = new Thread(this.GeneratorThread);
        this.thread.Start();
    }

    public void OnDisable()
    {
        // Abort the generator thread
        this.thread.Abort();
    }

    // Method to Generate chunks
    private void Generate()
    {
        // Calculate chunk size
        float chunkSize = InfiniteMap.BLOCK_SIZE * this.ChunkSize;

        // Calculate target position relative to map position
        float targetX = this.targetPosition.x - this.mapPosition.x + InfiniteMap.BLOCK_SIZE / 2;
        float targetZ = this.targetPosition.z - this.mapPosition.z + InfiniteMap.BLOCK_SIZE / 2;

        // Calculate chunk indices for the target position
        int chunkX = Mathf.FloorToInt(targetX / chunkSize);
        int chunkZ = Mathf.FloorToInt(targetZ / chunkSize);

        // InitializeMap variables for finding the closest missing chunk
        Vector3Int closestMissingChunk = Vector3Int.zero;
        float closestDistance = this.Range;
        bool any = false;

        // Iterate over chunks within the range
        for (int x = Mathf.FloorToInt(chunkX - this.Range / chunkSize); x < chunkX + this.Range / chunkSize; x++)
        {
            for (int z = Mathf.FloorToInt(chunkZ - this.Range / chunkSize); z < chunkZ + this.Range / chunkSize; z++)
            {
                var chunk = new Vector3Int(x, 0, z);
                // If chunk is already generated, skip
                if (this.generatedChunks.Contains(chunk))
                {
                    continue;
                }
                // Calculate center of the chunk and distance to target position
                var center = (chunk.ToVector3() + new Vector3(0.5f, 0f, 0.5f)) * chunkSize - new Vector3(1f, 0f, 1f) * InfiniteMap.BLOCK_SIZE / 2;
                float distance = Vector3.Distance(center, this.targetPosition + Vector3.down * this.targetPosition.y);

                // Update closest missing chunk if closer chunk is found
                if (distance < closestDistance)
                {
                    closestMissingChunk = chunk;
                    any = true;
                    closestDistance = distance;
                }
            }
        }

        // If any missing chunk is found, create it
        if (any)
        {
            this.CreateChunk(closestMissingChunk);
        }
    }


    // Method to create a chunk at the given chunk address
    private void CreateChunk(Vector3Int chunkAddress)
    {
        // Set range limit center and range for the map
        this.map.rangeLimitCenter = chunkAddress * this.ChunkSize + new Vector3Int(this.ChunkSize / 2, 0, this.ChunkSize / 2);
        this.map.RangeLimit = this.ChunkSize + 20;

        // Collapse the chunk
        this.map.Collapse(chunkAddress * this.ChunkSize, new Vector3Int(this.ChunkSize, this.map.Height, this.ChunkSize));

        // Add the generated chunk to the list
        this.generatedChunks.Add(chunkAddress);

        // Enqueue the completed chunk event
        this.chunkEventManager?.CompletedChunks.Enqueue(chunkAddress);
    }

    private void GeneratorThread()
    {
        try
        {
            // Continuously Generate chunks with a short delay
            while (true)
            {
                this.Generate();
                Thread.Sleep(50);
            }
        }
        catch (Exception exception)
        {
            // Handle exceptions
            if (exception is ThreadAbortException)
            {
                return;
            }
            Debug.LogError(exception);
        }
    }

    void Update()
    {
        // Update target and map positions
        this.targetPosition = this.Target.position;
        this.mapPosition = this.mapBehaviour.transform.position;

        // Update chunk events manager if it exists
        this.chunkEventManager?.Update();
    }

    // Method to register a map generation callback receiver
    public void RegisterMapGenerationCallbackReceiver(IMapGenerationCallbackReceiver receiver)
    {
        // If chunk events manager is not initialized, initialize it
        this.chunkEventManager ??= new ChunkEvents(this);
        // Add the receiver to the list of receivers
        this.chunkEventManager.MapGenerationCallbackReceivers.Add(receiver);
    }

    // Method to unregister a map generation callback receiver
    public void UnregisterMapGenerationCallbackReceiver(IMapGenerationCallbackReceiver receiver)
    {
        // If chunk events manager is not initialized, return
        if (this.chunkEventManager == null)
        {
            return;
        }
        // Remove the receiver from the list of receivers
        this.chunkEventManager.MapGenerationCallbackReceivers.Remove(receiver);
    }

    // Method to check if a chunk is already generated
    public bool IsGenerated(Vector3Int chunkAddress)
    {
        // Ensure the chunk address is at ground level and check if it exists in the generated chunks
        Debug.Assert(chunkAddress.y == 0);
        return this.generatedChunks.Contains(chunkAddress);
    }

}
