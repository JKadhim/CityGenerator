using UnityEngine;

//Defines a callback method for map generation events
public interface InfiniteCallbackReciever
{
    void OnGenerateChunk(Vector3Int chunkAddress, GenerateAroundPlayer source);
}
