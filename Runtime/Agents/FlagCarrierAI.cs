
using System;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// An ai that captures the enemy flag and tries to return the friendly flag
    /// </summary>
    class FlagCarrierAI : ExampleAI
    {
        private Flag myFlag;
        private Flag enemyFlag;


        private Dictionary<string, Func<float>> utilities;
        private Dictionary<string, Action> actions;

        private bool friendlyFlagIsDropped = false;
        private bool flagsSpawned = false;

        private Idle idle;

        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 18);

            AllocateStat(StatType.VisionRange, 1);
            AllocateStat(StatType.ProjectileRange, 1);
        }

        protected override void StartAI()
        {

            var blueFlag = CaptureTheFlag.Instance.BlueFlag;
            var redFlag = CaptureTheFlag.Instance.RedFlag;

            myFlag = (blueFlag.TeamID == MyDetectable.TeamID) ? blueFlag : redFlag;
            enemyFlag = (myFlag == blueFlag) ? redFlag : blueFlag;

            // Define utility functions
            utilities = new Dictionary<string, Func<float>>()
            {
                { "MoveToEnemyFlag", CalculateEnemyFlagUtility },
                { "MoveToFriendlyFlag", CalculateFriendlyFlagUtility },
                { "MoveToFriendlyZone", CalculateFriendlyZoneUtility },
                { "ChaseEnemyCarrier", CalculateChaseEnemyFlagCarrier },
                { "Idle", CalculateIdle }
            };

            // Define actions
            actions = new Dictionary<string, Action>()
            {
                { "MoveToEnemyFlag", () => ChangeState(new MoveToEnemyFlag(this)) },
                { "MoveToFriendlyFlag", () => ChangeState(new FollowFriendlyFlag(this)) },
                { "MoveToFriendlyZone", () => ChangeState(new MoveToFriendlyZone(this)) },
                { "ChaseEnemyCarrier", () => ChangeState(new ChaseEnemyCarrier(this)) },
                { "Idle", () => ChangeState(idle = new Idle(this)) }
            };

            CaptureTheFlag.Instance.FlagsSpawned += OnFlagSpawned;
           // Respawned += RespawnAgent;
        }

        private void OnFlagSpawned()
        {
            flagsSpawned = true;
        }

        private string lastAction = string.Empty;

        protected override void ExecuteAI()
        {
            // 1) Compute all utilities into a list
            var scored = utilities
              .Select(u => new { Name = u.Key, Score = u.Value() })
              .OrderByDescending(x => x.Score)
              .ToList();

            // 2) Log them all at once
            string line = string.Join(
                ", ",
                scored.Select(x => $"{x.Name}: {x.Score:F2}")
            );
           
           

            // 3) Pick the best
            var bestAction = scored.First().Name;

            Debug.Log($"[Utils] {line} Picked: {bestAction}");

            // 4) Change state if needed
            if (bestAction != lastAction)
            {
                actions[bestAction].Invoke();
                lastAction = bestAction;
            }

            base.ExecuteAI();
        }

        private float CalculateEnemyFlagUtility()
        {
            if (!flagsSpawned) return 0;

            if (enemyFlag.CurrentStatus == Flag.FlagStatus.Pickedup) return 0;
      
            return 1;
        }

        private float CalculateChaseEnemyFlagCarrier()
        {
            if (!MyFlagCarrier.HasFlag && myFlag.CurrentStatus == Flag.FlagStatus.Pickedup && Vector3.Distance(transform.position, myFlag.transform.position) < ProjectileRange) return 1.3f;

            return 0;
        }


        private float CalculateFriendlyZoneUtility()
        {
            if (MyFlagCarrier.HasFlag) return 1.1f;

            return 0;
        }

        private float CalculateFriendlyFlagUtility()
        {
            if(!MyFlagCarrier.HasFlag && myFlag.CurrentStatus != Flag.FlagStatus.OnBase && enemyFlag.CurrentStatus != Flag.FlagStatus.OnBase) return 1.2f;

            return 0;
        }

        private float CalculateIdle()
        {
            if (!flagsSpawned) return 2;

            if (!MyDetectable) return 2;

            if (!MyFlagCarrier.HasFlag && enemyFlag.CurrentStatus == Flag.FlagStatus.Pickedup) return 1;

            return 0;
        }

        //private void RespawnAgent()
        //{
        //    ChangeState(idle);
        //    lastAction = "Idle";
        //}

    }
}
