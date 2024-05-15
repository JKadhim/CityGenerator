using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;

[RequireComponent(typeof(MapBehaviour))]
public class GenerateAroundPlayer : MonoBehaviour
{
    private class ChunkEvents
    {
        public readonly ConcurrentQueue<Vector3Int> finishedChunks;
        public readonly List<InfiniteCallbackReciever> callbackReceivers;
        private readonly GenerateAroundPlayer source;

        public ChunkEvents(GenerateAroundPlayer source)
        {
            this.finishedChunks = new ConcurrentQueue<Vector3Int>();
            this.callbackReceivers = new List<InfiniteCallbackReciever>();
            this.source = source;
        }

        public void Update()
        {
            if (!this.finishedChunks.Any())
            {
                return;
            }
            if (this.finishedChunks.TryDequeue(out Vector3Int finishedChunk))
            {
                foreach (var subscriber in this.callbackReceivers)
                {
                    subscriber.OnGenerateChunk(finishedChunk, this.source);
                }
            }
        }
    }

    private MapBehaviour mapBehaviour;
    private InfiniteMap map;
    private CullingData cullingData;

    public Transform target;

    public int chunkSize = 4;

    public float range = 30;

    private Vector3 targetPosition;
    private Vector3 mapPosition;

    private HashSet<Vector3Int> generatedChunks;

    private Thread thread;

    private ChunkEvents chunkManager;

    void Start()
    {
        this.generatedChunks = new HashSet<Vector3Int>();
        this.mapBehaviour = this.GetComponent<MapBehaviour>();
        this.mapBehaviour.Initialize();
        this.cullingData = this.GetComponent<CullingData>();
        this.map = this.mapBehaviour.map;
        this.Generate();
        this.mapBehaviour.BuildAllCells();

        this.thread = new Thread(this.GeneratorThread);
        this.thread.Start();
    }

    public void OnDisable()
    {
        this.thread.Abort();
    }

    private void Generate()
    {
        float chunkSizeLocal = InfiniteMap.BLOCK_SIZE * this.chunkSize;

        float xTarget = this.targetPosition.x - this.mapPosition.x + InfiniteMap.BLOCK_SIZE / 2;
        float zTarget = this.targetPosition.z - this.mapPosition.z + InfiniteMap.BLOCK_SIZE / 2;

        int xChunk = Mathf.FloorToInt(xTarget / chunkSizeLocal);
        int zChunk = Mathf.FloorToInt(zTarget / chunkSizeLocal);

        Vector3Int closestMissingChunk = Vector3Int.zero;
        float closestDistance = this.range;
        bool any = false;

        for (int x = Mathf.FloorToInt(xChunk - this.range / chunkSizeLocal); x < xChunk + this.range / chunkSizeLocal; x++)
        {
            for (int z = Mathf.FloorToInt(zChunk - this.range / chunkSizeLocal); z < zChunk + this.range / chunkSizeLocal; z++)
            {
                var chunk = new Vector3Int(x, 0, z);
                if (this.generatedChunks.Contains(chunk))
                {
                    continue;
                }
                var centre = (chunk.ToVector3() + new Vector3(0.5f, 0f, 0.5f)) * chunkSizeLocal - new Vector3(1f, 0f, 1f) * InfiniteMap.BLOCK_SIZE / 2;
                float distance = Vector3.Distance(centre, this.targetPosition + Vector3.down * this.targetPosition.y);

                if (distance < closestDistance)
                {
                    closestMissingChunk = chunk;
                    any = true;
                    closestDistance = distance;
                }
            }
        }

        if (any)
        {
            this.CreateChunk(closestMissingChunk);
        }
    }

    private void CreateChunk(Vector3Int chunkAddress)
    {
        this.map.rangeLimitCentre = chunkAddress * this.chunkSize + new Vector3Int(this.chunkSize / 2, 0, this.chunkSize / 2);
        this.map.rangeLimit = this.chunkSize + 20;
        this.map.Collapse(chunkAddress * this.chunkSize, new Vector3Int(this.chunkSize, this.map.height, this.chunkSize));
        this.generatedChunks.Add(chunkAddress);
        this.chunkManager?.finishedChunks.Enqueue(chunkAddress);
    }

    private void GeneratorThread()
    {
        try
        {
            while (true)
            {
                this.Generate();
                Thread.Sleep(50);
            }
        }
        catch (System.Exception exception)
        {
            if (exception is System.Threading.ThreadAbortException)
            {
                return;
            }
            Debug.LogError(exception);
        }

    }

    void Update()
    {
        this.targetPosition = this.target.position;
        this.mapPosition = this.mapBehaviour.transform.position;

        this.chunkManager?.Update();
    }

    public void RegisterCallbackReceiver(InfiniteCallbackReciever receiver)
    {
        this.chunkManager ??= new ChunkEvents(this);
        this.chunkManager.callbackReceivers.Add(receiver);
    }

    public void UnregisterCallbackReceiver(InfiniteCallbackReciever receiver)
    {
        if (this.chunkManager == null)
        {
            return;
        }
        this.chunkManager.callbackReceivers.Remove(receiver);
    }

    public bool IsGenerated(Vector3Int chunkAddress)
    {
        Debug.Assert(chunkAddress.y == 0);
        return this.generatedChunks.Contains(chunkAddress);
    }
}
