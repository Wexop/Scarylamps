using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace ScaryLamps.Scripts;

public class ScaryLampEnemyAI : EnemyAI
{
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Death = Animator.StringToHash("Death");
    public List<AudioClip> walkSounds;
    public AudioSource AttackAudioSource;
    public AudioClip deathSound;

    public Light angryLight;

    public Volume Volume;
    
    private readonly float walkSpeed = 3.5f;

    private float aiInterval = 0.2f;
    private int lastBehaviorState;
    private readonly float walkSoundDelayRun = 0.5f;
    private readonly float walkSoundDelayWalk = 0.9f;
    
    private float attackPlayerTimer;
    private readonly float attackPlayerDelay = 0.5f;
    
    private float volumeWeightTimer;
    private readonly float volumeWeightDelay = 2f;

    private float walkSoundTimer;
    private float visionWidth = 100f;

    public override void Start()
    {
        base.Start();

        agent.speed = walkSpeed;
        agent.acceleration = 255f;
        agent.angularSpeed = 900f;
        angryLight.enabled = false;
    }

    public override void Update()
    {
        Volume.weight = Mathf.Clamp(volumeWeightTimer / volumeWeightDelay, 0,1);
        volumeWeightTimer -= Time.deltaTime;
        if(isEnemyDead) return;
        base.Update();
        
        attackPlayerTimer -= Time.deltaTime;

        if (lastBehaviorState != currentBehaviourStateIndex)
        {
            
            Debug.Log($"New behavior state : {currentBehaviourStateIndex} last : {lastBehaviorState}");
            lastBehaviorState = currentBehaviourStateIndex;
            AllClientOnSwitchBehaviorState();
        }

        walkSoundTimer -= Time.deltaTime;

        //WALKSOUNDS
        if (walkSoundTimer <= 0f)
        {
            var randomSound = walkSounds[Random.Range(0, walkSounds.Count)];
            creatureSFX.PlayOneShot(randomSound);
            walkSoundTimer = currentBehaviourStateIndex == 1 ? walkSoundDelayRun : walkSoundDelayWalk;
        }

        if (currentBehaviourStateIndex == 1 &&
            GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(
                transform.position + Vector3.up * 0.25f, visionWidth, 60))
        {
            volumeWeightTimer = volumeWeightDelay;
            if (attackPlayerTimer <= 0)
            {
                attackPlayerTimer = attackPlayerDelay;
                GameNetworkManager.Instance.localPlayerController.DamagePlayer(ScaryLampsPlugin.instance.scaryLampDamage.Value);
                GameNetworkManager.Instance.localPlayerController.sprintMeter = 0;
                volumeWeightTimer = volumeWeightDelay;

            }
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
                    aiSearchRoutine.searchWidth = 50f;
                    aiSearchRoutine.searchPrecision = 8f;
                    StartSearch(ChooseFarthestNodeFromPosition(transform.position, true).position, aiSearchRoutine);
                }

                if (targetPlayer)
                {
                    transform.LookAt(targetPlayer.gameplayCamera.transform);
                    transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                    TurnOnPlayerTargetClientRpc(transform.rotation);
                    SwitchToBehaviourState(1);
                }

                break;
            }
            //attack
            case 1:
            {
                TargetClosestPlayer(requireLineOfSight: true, viewWidth: visionWidth);
                if (!targetPlayer)
                {
                    SwitchToBehaviourState(0);
                }

                break;
            }
            //die
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

    private void AllClientOnSwitchBehaviorState()
    {
        switch (currentBehaviourStateIndex)
        {
            case 0:
            {
                agent.speed = walkSpeed;
                creatureAnimator.SetBool(Attack, false);
                angryLight.enabled = false;
                AttackAudioSource.Stop();

                break;
            }
            case 1:
            {
                agent.speed = 0;
                creatureAnimator.SetBool(Attack, true);
                AttackAudioSource.Play();
                angryLight.enabled = true;
                break;
            }
            case 2:
            {
                agent.speed = 0;
                creatureAnimator.SetBool(Attack, false);
                creatureAnimator.SetBool(Death, true);
                angryLight.enabled = false;
                creatureVoice.PlayOneShot(deathSound);
                AttackAudioSource.Stop();

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