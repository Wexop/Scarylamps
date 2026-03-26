using System;
using System.Linq;
using UnityEngine;

namespace ScaryLamps.Scripts;

public class MonsterDetectorLamp: MonoBehaviour
{
    public GameObject greenObject;
    public GameObject orangeObject;
    public GameObject RedObject;


    private int currentState = 0;

    private void OnChangeState(int newState)
    {
        greenObject.SetActive(newState == 0);
        orangeObject.SetActive(newState == 1);
        RedObject.SetActive(newState == 2);
        currentState = newState;
    }

    private int StateFromDistance(float distance)
    {
        if (distance > ScaryLampsPlugin.instance.MonsterDetectorRange.Value) return 0;
        if (distance < ScaryLampsPlugin.instance.MonsterDetectorRange.Value / 2) return 2;
        return 1;
    }

    private void Start()
    {
        OnChangeState(0);
    }

    private void Update()
    {
        float distance = ScaryLampsPlugin.instance.MonsterDetectorRange.Value + 1;
        FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).ToList().ForEach(ai =>
        {
            float monsterDistance = Vector3.Distance(ai.transform.position, transform.position);
            if ( monsterDistance < distance && !ai.isEnemyDead)
            {
                distance = monsterDistance;
            }
        } );
        
        int newState = StateFromDistance(distance);
        if(newState != currentState)
        {
            OnChangeState(newState);
        }
    }
}