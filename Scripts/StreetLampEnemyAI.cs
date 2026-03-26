using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace ScaryLamps.Scripts;

public class StreetLampEnemyAI : EnemyAI
{
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Run = Animator.StringToHash("run");
    private static readonly int Jump = Animator.StringToHash("Jump");
    public List<AudioClip> walkSounds;
    public AudioClip explodeSound;
    public ParticleSystem explosionParticles;
    
    private readonly float walkSpeed = 3.5f;
    private readonly float runSpeed = 8f;

    private float aiInterval = 0.2f;
    private int lastBehaviorState;
    private readonly float walkSoundDelayRun = 0.5f;
    private readonly float walkSoundDelayWalk = 0.9f;

    private float walkSoundTimer;
    private float visionWidth = 100f;

    private float runAfterPlayerTimer;
    private float runAfterPlayerDelay = 3f;

    private float explosionRange = 8f;
    private float jumpRange = 6f;

    public override void Start()
    {
        base.Start();

        agent.speed = walkSpeed;
        agent.acceleration = 255f;
        agent.angularSpeed = 900f;
    }

    public override void Update()
    {

        if(isEnemyDead) return;
        base.Update();
        
        runAfterPlayerTimer -= Time.deltaTime;
        
        if (lastBehaviorState != currentBehaviourStateIndex)
        {
            
            Debug.Log($"New behavior state : {currentBehaviourStateIndex} last : {lastBehaviorState}");
            lastBehaviorState = currentBehaviourStateIndex;
            AllClientOnSwitchBehaviorState();
        }

        walkSoundTimer -= Time.deltaTime;

        //WALKSOUNDS
        if (walkSoundTimer <= 0f && currentBehaviourStateIndex != 2)
        {
            var randomSound = walkSounds[Random.Range(0, walkSounds.Count)];
            creatureSFX.PlayOneShot(randomSound);
            walkSoundTimer = currentBehaviourStateIndex == 1 ? walkSoundDelayRun : walkSoundDelayWalk;
        }

        if (!IsServer) return;

        if (aiInterval <= 0)
        {
            aiInterval = AIIntervalTime;
            DoAIInterval();
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        switch (currentBehaviourStateIndex)
        {
            //walking
            case 0:
            {
                TargetClosestPlayer(requireLineOfSight: true, viewWidth: visionWidth);
                if (targetPlayer == null)
                {
                    if (currentSearch.inProgress) break;
                    var aiSearchRoutine = new AISearchRoutine();
                    aiSearchRoutine.searchWidth = 100f;
                    aiSearchRoutine.searchPrecision = 8f;
                    StartSearch(ChooseFarthestNodeFromPosition(transform.position, true).position, aiSearchRoutine);
                }

                if (targetPlayer && PlayerIsTargetable(targetPlayer))
                {
                    runAfterPlayerTimer = runAfterPlayerDelay;
                    SwitchToBehaviourState(1);
                }

                break;
            }
            //run
            case 1:
            {
                
                if (targetPlayer)
                {
                    SetMovingTowardsTargetPlayer(targetPlayer);

                    if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= jumpRange)
                    {
                        transform.LookAt(targetPlayer.gameplayCamera.transform);
                        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                        TurnOnPlayerTargetClientRpc(transform.rotation);
                        SwitchToBehaviourState(2);
                    }
                }
                
                if (runAfterPlayerTimer <= 0)
                {
                    TargetClosestPlayer(requireLineOfSight: true, viewWidth: visionWidth);
                    if (targetPlayer)
                    {
                        runAfterPlayerTimer =  runAfterPlayerDelay / 2;
                    }
                    else
                    {
                        SwitchToBehaviourState(0);
                    }
                }



                break;
            }
            //jump and die
            case 2:
            {
                KillEnemyServerRpc(false);
                break;
            }
        }
    }

    [ClientRpc]
    private void TurnOnPlayerTargetClientRpc(Quaternion value)
    {
        transform.rotation = value;
    }

    public void Explode()
    {
        creatureVoice.PlayOneShot(explodeSound);
        explosionParticles.Play();
        float playerDistance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position,
            transform.position);
        if ( playerDistance <= explosionRange)
        {
            GameNetworkManager.Instance.localPlayerController.KillPlayer(transform.forward);
        }
        else if ( playerDistance <= explosionRange * 1.5)
        {
            GameNetworkManager.Instance.localPlayerController.DamagePlayer(25);
        }
        
        if(!IsServer) return;

        List<EnemyAI> enemiesClose = FindObjectsOfType<EnemyAI>().ToList();
        enemiesClose.ForEach(enemy =>
        {
            if (Vector3.Distance(enemy.transform.position,
                    transform.position) <= explosionRange)
            {
                enemy.HitEnemyServerRpc(5, 0, false);
            }
        });
    }

    private void AllClientOnSwitchBehaviorState()
    {
        switch (currentBehaviourStateIndex)
        {
            case 0:
            {
                agent.speed = walkSpeed;
                creatureAnimator.SetBool(Run,false);

                break;
            }
            case 1:
            {
                agent.speed = runSpeed;
                creatureAnimator.SetBool(Run, true);

                break;
            }
            case 2:
            {
                agent.speed = 0;
                creatureAnimator.SetBool(Attack, false);
                creatureAnimator.SetBool(Jump,true);

                break;
            }
        }
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSfx = false, int hitID = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSfx, hitID);
        if (isEnemyDead) return;
        //base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        enemyHP -= force;

        if (enemyHP <= 0)
        {
            SwitchToBehaviourServerRpc(2);
        }

    }


}