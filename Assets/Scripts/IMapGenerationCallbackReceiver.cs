using UnityEngine;

//Defines a callback method for map generation events
public interface IMapGenerationCallbackReceiver
{
    void OnGenerateChunk(Vector3Int chunkAddress, GenerateMapNearPlayer source);
}
