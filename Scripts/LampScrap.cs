using Unity.Netcode;
using UnityEngine;

namespace ScaryLamps.Scripts;

public class LampScrap: PhysicsProp
{

    public Light Light;

    public override void Start()
    {
        Light.enabled = false;
        base.Start();
        if (IsServer)
        {
            SetValueClientRpc(Random.Range(50,90));
        }
    }

    public override void UseUpBatteries()
    {
        base.UseUpBatteries();
        Light.enabled = false;
    }
    

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        Light.enabled = used;
    }

    public override void PocketItem()
    {
        base.PocketItem();
        Light.enabled = false;
        isBeingUsed = false;
    }

    [ClientRpc]
    private void SetValueClientRpc(int value)
    {
        SetScrapValue(value);
    }
}