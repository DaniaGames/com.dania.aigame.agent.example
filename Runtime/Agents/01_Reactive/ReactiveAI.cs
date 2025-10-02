using System.Collections.Generic;
using AIGame.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

namespace AIGame.Examples.ReactiveAI
{
    public class ReactiveAI : BaseAI
    {

        #region Fields: Wandering

        private float wanderRadius = 200f;
        private int maxAttempts = 30;
        private Vector3 currentDestination;
        private const float ARRIVAL_THRESHOLD = 0.5f;
        private bool wander = true;

        #endregion

        #region Fields: PowerUp
        private int powerUpID = -1;
        #endregion

        #region Fields: Flag
        private FlagCarrier fc;

        private Agent agent;

        private bool movingToFlag = false;

        #endregion

        #region Combat

        private bool inCombat = false;

        #endregion

        #region Fields: General

        #endregion

        protected override void StartAI()
        {
            fc = GetComponent<FlagCarrier>();
            agent = GetComponent<Agent>();

            currentDestination = PickRandomDestination();
            MoveTo(currentDestination);

            FlagEnter += OnFlagDiscovered;
            PowerUpEnterVision += PickPowerUp;
            fc.Pickup += OnPickUpFlag;
            fc.Capture += OnCaptureFlag;
            Death += OnDeath;
            EnemyEnterVision += OnEnemyDetected;


        }


        protected override void ConfigureStats()
        {
            AllocateStat(StatType.DodgeCooldown, 4);
            AllocateStat(StatType.ProjectileRange, 4);
            AllocateStat(StatType.ReloadSpeed, 4);
            AllocateStat(StatType.Speed, 4);
            AllocateStat(StatType.VisionRange, 4);
        }

        protected override void ExecuteAI()
        {
            if (!IsAlive)
                return;

            // Prioritize combat over everything
            if (inCombat)
            {
                if (!CurrentTarget.HasValue)
                {
                    if (GetVisibleEnemiesSnapshot().Count > 0)
                        RefreshOrAcquireTarget();
                    else
                    {
                        WhatNow();
                        return;
                    }
                }
                if (TryGetTarget(out var target))
                {
                    FaceTarget(target.Position);
                    ThrowBallAt(target);
                }
            }
            // If moving to flag, don't interrupt unless in combat
            else if (movingToFlag)
            {
                if (HasReachedDestination())
                {
                    // Try to pick up the flag or continue to flag position
                    MoveToEnemyFlag();
                }
            }
            // Handle other movement states
            else if (wander && HasReachedDestination())
            {
                currentDestination = PickRandomDestination();
                MoveTo(currentDestination);
            }
            else if (!inCombat && HasReachedDestination())
            {
                if (powerUpID != -1)
                {
                    TryEatPowerUp();
                }
                else
                {
                    // No powerup to consume, resume wandering
                    wander = true;
                    currentDestination = PickRandomDestination();
                    MoveTo(currentDestination);
                }
            }
        }

        private void WhatNow()
        {
            if (!CurrentTarget.HasValue && GetVisibleEnemiesSnapshot().Count == 0)
            {
                inCombat = false;

                // Movement should resume automatically via MoveTo() calls

                if (fc.HasFlag)
                {
                    ReturnWithFlag();
                }
                else
                {
                    wander = true;
                    currentDestination = PickRandomDestination();
                    MoveTo(currentDestination);
                }
            }
        }

        #region Methods: Combat

        private void OnEnemyDetected()
        {
            wander = false;

            if (GetVisibleEnemiesSnapshot().Count == 0)
            {
                WhatNow();
                return;
            }

            RefreshOrAcquireTarget();
            StopMoving();
            inCombat = true;
        }

        private void OnDeath()
        {
            wander = true;
            movingToFlag = false;
        }

        #endregion


        #region Methods: Flag
        private void OnFlagDiscovered(Team team)
        {
            if (team != MyDetectable.TeamID)
            {
                // Enemy flag discovered - go get it (if we don't already have a flag)
                if (!fc.HasFlag)
                {
                    movingToFlag = true;
                    wander = false;
                    MoveToEnemyFlag();
                }
            }
            else if (team == MyDetectable.TeamID && fc.HasFlag)
            {
                // Friendly flag discovered and we have enemy flag - go score!
                movingToFlag = false;
                ReturnWithFlag();
            }
        }

        private void MoveToEnemyFlag()
        {
            var flags = GetVisibleFlagsSnapShot();

            if (flags.Count > 0)
            {
                for (int i = 0; i < flags.Count; i++)
                {
                    if (flags[i].Team != MyDetectable.TeamID)
                    {
                        currentDestination = flags[i].Position;
                        MoveTo(currentDestination);
                        return;
                    }
                }
            }

            // No enemy flag visible anymore - stop moving to flag
            movingToFlag = false;
            wander = true;
            currentDestination = PickRandomDestination();
            MoveTo(currentDestination);
        }

        private void OnPickUpFlag()
        {
            ReturnWithFlag();
        }

        private void OnCaptureFlag()
        {
            wander = true;
        }

        private void ReturnWithFlag()
        {
            movingToFlag = false;

            // Check if we no longer have the flag (it was handed in)
            if (!fc.HasFlag)
            {
                return;
            }

            Vector3? flagPosition = CaptureTheFlag.Instance.GetOwnFlagPosition(this);

            if (flagPosition.HasValue)
            {
                currentDestination = flagPosition.Value;
                MoveTo(currentDestination);
            }
            else
            {
                wander = true;
            }
        }

        #endregion

        #region Methods: PowerUp
        private void PickPowerUp()
        {
            // Don't interrupt flag movement or if already has buffs
            if (movingToFlag || GetActiveBuffs().Count > 0 || inCombat)
                return;

            var powerUps = GetVisiblePowerUpsSnapshot();

            if (powerUps.Count > 0)
            {
                // Pure reactive approach: add more randomness to reduce clustering
                float chanceToGo = powerUps.Count == 1 ? 0.9f : 0.4f; // Lower chance when multiple powerups visible

                if (Random.Range(0f, 1f) < chanceToGo)
                {
                    wander = false;
                    var targetPowerUp = GetClosestPowerUp(powerUps);
                    currentDestination = targetPowerUp.Position;
                    powerUpID = targetPowerUp.Id;
                    MoveTo(currentDestination);
                }
                else
                {
                    // Skip powerup and continue current behavior
                    if (!wander)
                    {
                        wander = true;
                        currentDestination = PickRandomDestination();
                        MoveTo(currentDestination);
                    }
                }
            }
            else if (!wander)
            {
                wander = true;
                currentDestination = PickRandomDestination();
                MoveTo(currentDestination);
            }
        }

        private PerceivedPowerUp GetClosestPowerUp(IReadOnlyList<PerceivedPowerUp> powerUps)
        {
            if (powerUps.Count == 0)
            {
                // Return a default/invalid power-up if the list is empty
                return default(PerceivedPowerUp);
            }

            PerceivedPowerUp closest = powerUps[0];
            float closestDistance = Vector3.Distance(transform.position, closest.Position);

            // Loop through all power-ups to find the closest one
            for (int i = 1; i < powerUps.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, powerUps[i].Position);
                if (distance < closestDistance)
                {
                    closest = powerUps[i];
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void TryEatPowerUp()
        {
            // If we see a friendly agent nearby while going for powerup, give up
            var allies = GetVisibleAlliesSnapshot();
            if (allies.Count > 0)
            {
                float powerUpDistance = Vector3.Distance(transform.position, currentDestination);
                foreach (var ally in allies)
                {
                    float allyToPowerUpDistance = Vector3.Distance(ally.Position, currentDestination);
                    // If ally is closer to the powerup than us, give up
                    if (allyToPowerUpDistance < powerUpDistance)
                    {
                        powerUpID = -1;
                        wander = true;
                        currentDestination = PickRandomDestination();
                        MoveTo(currentDestination);
                        return;
                    }
                }
            }

            if (TryConsumePowerup(powerUpID))
            {
                powerUpID = -1;

                if (fc.HasFlag)
                {
                    ReturnWithFlag();
                }
                else
                {
                    wander = true;
                    currentDestination = PickRandomDestination();
                    MoveTo(currentDestination);
                }
            }
            else
            {
                // PowerUp no longer available (likely taken by another agent)
                powerUpID = -1;
                wander = true;
                currentDestination = PickRandomDestination();
                MoveTo(currentDestination);
            }
        }
        #endregion

        #region Methods: Wandering

        private bool HasReachedDestination()
        {

            if (GetRemainingDistance() <= ARRIVAL_THRESHOLD)
            {
                return true;
            }
            else if (!IsPathPending() && !HasPath() && Vector3.Distance(transform.position, currentDestination) <= ARRIVAL_THRESHOLD)
            {
                return true;
            }

            return false;

        }

        /// <summary>
        /// Picks a random destination that the AI can walk to using NavMesh.
        /// </summary>
        /// <returns>A random walkable position, or the current position if no valid destination found</returns>
        private Vector3 PickRandomDestination()
        {
            Vector3 currentPosition = transform.position;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Generate a random direction
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += currentPosition;

                // Try to find a valid NavMesh position near the random point
                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
                {
                    // Additional check: make sure we can actually path to this destination
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(currentPosition, hit.position, NavMesh.AllAreas, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete)
                        {
                            return hit.position;
                        }
                    }
                }
            }

            return currentPosition;
        }

        #endregion

    }
}