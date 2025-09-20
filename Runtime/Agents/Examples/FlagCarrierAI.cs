using System;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// AI that plays capture-the-flag.
    /// Will try to capture the enemy flag and return the friendly flag if it is taken.
    /// Uses a utility-based decision-making system.
    /// </summary>
    class FlagCarrierAI : BaseAI
    {
        private ExampleFSM fsm;

        /// <summary>
        /// Reference to this agent's own team flag.
        /// </summary>
        private Flag myFlag;

        /// <summary>
        /// Reference to the opposing team's flag.
        /// </summary>
        private Flag enemyFlag;

        /// <summary>
        /// Dictionary of utility function names mapped to their scoring functions.
        /// Each function returns a score representing how desirable that action is.
        /// </summary>
        private Dictionary<string, Func<float>> utilities;

        /// <summary>
        /// Dictionary of action names mapped to the actual actions (state changes) to perform.
        /// </summary>
        private Dictionary<string, Action> actions;

        /// <summary>
        /// True if the friendly flag has been dropped in the field.
        /// </summary>
        private bool friendlyFlagIsDropped = false;

        /// <summary>
        /// True if both flags have been spawned into the game.
        /// </summary>
        private bool flagsSpawned = false;

        /// <summary>
        /// Cached reference to the idle state.
        /// </summary>
        private Idle idle;

        /// <summary>
        /// The name of the last action performed.
        /// Used to prevent re-entering the same state unnecessarily.
        /// </summary>
        private string lastAction = string.Empty;

        private bool visiblePowerUp;

        /// <inheritdoc/>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 18);
            AllocateStat(StatType.VisionRange, 1);
            AllocateStat(StatType.ProjectileRange, 1);
        }

        /// <inheritdoc/>
        protected override void StartAI()
        {
            fsm = new ExampleFSM();
            InitializeAI();

            // Subscribe to respawn event to reinitialize after respawn
            Respawned += OnRespawned;
        }

        /// <summary>
        /// Initializes or reinitializes the AI state.
        /// Called on startup and after respawn.
        /// </summary>
        private void InitializeAI()
        {

            var blueFlag = CaptureTheFlag.Instance.BlueFlag;
            var redFlag = CaptureTheFlag.Instance.RedFlag;

            myFlag = (blueFlag.TeamID == MyDetectable.TeamID) ? blueFlag : redFlag;
            enemyFlag = (myFlag == blueFlag) ? redFlag : blueFlag;

            // Reset state variables
            lastAction = string.Empty;
            flagsSpawned = CaptureTheFlag.Instance.BlueFlagZone != null && CaptureTheFlag.Instance.RedFlagZone != null;

            // Define utility functions
            utilities = new Dictionary<string, Func<float>>()
            {
                { "MoveToEnemyFlag", CalculateEnemyFlagUtility },
                { "MoveToFriendlyFlag", CalculateFriendlyFlagUtility },
                { "MoveToFriendlyZone", CalculateFriendlyZoneUtility },
                { "ChaseEnemyCarrier", CalculateChaseEnemyFlagCarrier },
                { "MoveToPowerUp", CalculateMovetoPowerUpUtility },
                { "Idle", CalculateIdle }
            };

            // Define actions
            actions = new Dictionary<string, Action>()
            {
                { "MoveToEnemyFlag", () => fsm.ChangeState(new MoveToEnemyFlag(this)) },
                { "MoveToFriendlyFlag", () => fsm.ChangeState(new FollowFriendlyFlag(this)) },
                { "MoveToFriendlyZone", () => fsm.ChangeState(new MoveToFriendlyZone(this)) },
                { "ChaseEnemyCarrier", () => fsm.ChangeState(new ChaseEnemyCarrier(this)) },
                { "MoveToPowerUp", () => fsm.ChangeState(new MoveToPowerUp(this)) },
                { "Idle", () => fsm.ChangeState(idle = new Idle(this)) }
            };

            // Subscribe to game events (only if not already subscribed)
            CaptureTheFlag.Instance.FlagsSpawned -= OnFlagSpawned; // Remove first to avoid duplicates
            CaptureTheFlag.Instance.FlagsSpawned += OnFlagSpawned;
            // PowerUpEnterVision += OnPowerUpSpotted;
            // PowerUpConsumed += OnPowerUpConsumed;
        }

        /// <summary>
        /// Called when the agent respawns. Reinitializes the AI state.
        /// </summary>
        private void OnRespawned()
        {
            InitializeAI();
        }

        /// <summary>
        /// Event handler called when both flags are spawned into the scene.
        /// </summary>
        private void OnFlagSpawned()
        {
            flagsSpawned = true;
        }

        /// <inheritdoc/>
        protected override void ExecuteAI()
        {
            // 1) Compute utility scores
            var scored = utilities
                .Select(u => new { Name = u.Key, Score = u.Value() })
                .OrderByDescending(x => x.Score)
                .ToList();

            // 2) Log all scores
            string line = string.Join(", ", scored.Select(x => $"{x.Name}: {x.Score:F2}"));

            // 3) Pick the highest scoring action
            var bestAction = scored.First().Name;

            // 4) Only change state if the action is different from last frame
            if (bestAction != lastAction)
            {
                actions[bestAction].Invoke();
                lastAction = bestAction;
            }

            fsm.Execute();
        }

        // private void OnPowerUpSpotted()
        // {
        //     Debug.Log("Powerup spotted");
        //     visiblePowerUp = true;
        // }

        // private void OnPowerUpConsumed()
        // {
        //     visiblePowerUp = false;
        // }

        /// <summary>
        /// Calculates the utility of moving to capture the enemy flag.
        /// </summary>
        private float CalculateEnemyFlagUtility()
        {
            if (!flagsSpawned) return 0;
            if (enemyFlag.CurrentStatus == Flag.FlagStatus.Pickedup) return 0;

            return 1;
        }

        /// <summary>
        /// Calculates the utility of chasing the enemy flag carrier.
        /// </summary>
        private float CalculateChaseEnemyFlagCarrier()
        {
            if (!MyFlagCarrier.HasFlag &&
                myFlag.CurrentStatus == Flag.FlagStatus.Pickedup &&
                Vector3.Distance(transform.position, myFlag.transform.position) < ProjectileRange)
                return 1.3f;

            return 0;
        }

        /// <summary>
        /// Calculates the utility of returning to the friendly flag zone.
        /// </summary>
        private float CalculateFriendlyZoneUtility()
        {
            if (MyFlagCarrier.HasFlag) return 1.1f;
            return 0;
        }

        /// <summary>
        /// Calculates the utility of moving to retrieve the friendly flag.
        /// </summary>
        private float CalculateFriendlyFlagUtility()
        {
            if (!MyFlagCarrier.HasFlag &&
                myFlag.CurrentStatus != Flag.FlagStatus.OnBase &&
                enemyFlag.CurrentStatus != Flag.FlagStatus.OnBase)
                return 1.2f;

            return 0;
        }

                /// <summary>
        /// Calculates the utility of chasing the enemy flag carrier.
        /// </summary>
        private float CalculateMovetoPowerUpUtility()
        {
            if (GetVisiblePowerUpsSnapshot().Count > 0 && GetActiveBuffs().Count == 0)
                 return 2f;

            return 0;
        }


        /// <summary>
        /// Calculates the utility of idling.
        /// </summary>
        private float CalculateIdle()
        {
            if (!flagsSpawned) return 2;
            if (!MyDetectable) return 2;
            if (!MyFlagCarrier.HasFlag && enemyFlag.CurrentStatus == Flag.FlagStatus.Pickedup) return 1;

            return 0;
        }
    }
}
