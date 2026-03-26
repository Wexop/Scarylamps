using UnityEngine;

namespace ScaryLamps.Scripts;

public class StreetLampExplosion: MonoBehaviour
{
    public StreetLampEnemyAI creature;
    
    
    public void Explode()
    {
        creature.Explode();
    }
}