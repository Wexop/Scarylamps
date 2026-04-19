using Unity.Netcode;
using UnityEngine;

namespace ScaryLamps.Scripts;

public class SpawnLampScrap: NetworkBehaviour
{
    
    public GameObject lampScrap;

    public Transform ScrapSpawnPos;
    
    
    public void Spawn()
    {
        if(!IsServer) return;
        var lamp = Instantiate(lampScrap, ScrapSpawnPos.position, ScrapSpawnPos.rotation);
        lamp.GetComponent<NetworkObject>().Spawn();
    }
}