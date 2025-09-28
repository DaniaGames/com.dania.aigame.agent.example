using UnityEngine;
using AIGame.Core;
using System.Collections.Generic;

namespace AIGame.Examples.GoalOriented
{
    public abstract class Action
    {
        public string Name { get; protected set; }
        public float Cost { get; protected set; }

        protected WorldState preconditions = new WorldState();
        protected WorldState effects = new WorldState();

        protected BaseAI agent;

        public Action(BaseAI agent, string name, float cost = 1f)
        {
            this.agent = agent;
            this.Name = name;
            this.Cost = cost;
        }

        public virtual bool CanExecute(WorldState worldState)
        {
            return worldState.Satisfies(preconditions);
        }

        public WorldState ApplyEffects(WorldState currentState)
        {
            WorldState newState = currentState.Clone();

            // Apply all effects to the new state
            foreach (var effect in GetEffects())
            {
                newState.SetState(effect.Key, effect.Value);
            }

            return newState;
        }

        protected virtual System.Collections.Generic.Dictionary<string, object> GetEffects()
        {
            var effectDict = new System.Collections.Generic.Dictionary<string, object>();
            return effectDict;
        }

        public abstract bool Execute();
        public abstract bool IsComplete();
    }

    public class IdleAction : Action
    {
        public IdleAction(BaseAI agent, float cost = 1) : base(agent, "Idle", 1)
        {
        }


        public override bool Execute()
        {
            return true;
        }

        public override bool IsComplete()
        {
            
            return false;
        }
    }

    // Specific actions for flag capture

    public class MoveToEnemyFlagAction : Action
    {
        public MoveToEnemyFlagAction(BaseAI agent) : base(agent, "MoveToEnemyFlag", 2f)
        {
            // Requires knowing where the enemy flag is
            preconditions.SetState(StateKeys.KNOW_ENEMY_FLAG_LOCATION, true);
        }

        protected override System.Collections.Generic.Dictionary<string, object> GetEffects()
        {
            var effects = new System.Collections.Generic.Dictionary<string, object>();
            effects[StateKeys.HAS_ENEMY_FLAG] = true;
            return effects;
        }

        public override bool Execute()
        {
            // First try to get the flag position if it's dropped (we know exact location)
            Vector3? droppedPosition = CaptureTheFlag.Instance.GetDiscoverableEnemyFlagPosition(agent);
            if (droppedPosition.HasValue)
            {
                agent.MoveTo(droppedPosition.Value);

                return true;
            }

            // Fallback to visual search if flag is not dropped
            var flags = agent.GetVisibleFlagsSnapShot();
            foreach (var flag in flags)
            {
                if (flag.Team != agent.MyDetectable.TeamID)
                {
                    agent.MoveTo(flag.Position);

                    return true;
                }
            }

            return false;
        }

        public override bool IsComplete()
        {
            var fc = agent.GetComponent<FlagCarrier>();
            return fc != null && fc.HasFlag;
        }
    }

    public class ReturnToBaseAction : Action
    {
        public ReturnToBaseAction(BaseAI agent) : base(agent, "ReturnToBase", 3f)
        {
            // Requires having the enemy flag
            preconditions.SetState(StateKeys.HAS_ENEMY_FLAG, true);
        }

        protected override System.Collections.Generic.Dictionary<string, object> GetEffects()
        {
            var effects = new System.Collections.Generic.Dictionary<string, object>();
            effects[StateKeys.AT_HOME_BASE] = true;
            effects[StateKeys.ENEMY_FLAG_CAPTURED] = true;
            return effects;
        }

        public override bool Execute()
        {
            Vector3? flagPosition = CaptureTheFlag.Instance.GetOwnFlagPosition(agent);
            if (flagPosition.HasValue)
            {
                agent.MoveTo(flagPosition.Value);
                return true;
            }
            return false;
        }

        public override bool IsComplete()
        {
            var fc = agent.GetComponent<FlagCarrier>();
            Vector3? homePos = CaptureTheFlag.Instance.GetOwnFlagPosition(agent);

            if (homePos.HasValue)
            {
                return Vector3.Distance(agent.transform.position, homePos.Value) < 3f;
            }
            return false;
        }
    }

    public class FindEnemyFlagAction : Action
    {
        private Vector3 searchTarget;
        private bool hasTarget = false;
        private int currentZoneIndex = -1; // Start at -1 to pick closest zone first
        private bool hasSearchedCurrentZone = false;

        public FindEnemyFlagAction(BaseAI agent) : base(agent, "FindEnemyFlag", 1f)
        {
            // No preconditions - can start moving to zones even before flags spawn
        }

        protected override Dictionary<string, object> GetEffects()
        {
            var effects = new Dictionary<string, object>();
            effects[StateKeys.KNOW_ENEMY_FLAG_LOCATION] = true;
            return effects;
        }

        public override bool Execute()
        {
            // Check if we can see the enemy flag via current visibility
            var flags = agent.GetVisibleFlagsSnapShot();
            bool flagCurrentlyVisible = false;
            foreach (var flag in flags)
            {
                if (flag.Team != agent.MyDetectable.TeamID)
                {
                    flagCurrentlyVisible = true;
                    break;
                }
            }

            // If we can see the enemy flag, we're done
            if (flagCurrentlyVisible)
            {
                return true; // Found it!
            }

            // Get enemy flag zones
            FlagZone[] enemyZones = GetEnemyFlagZones();
            if (enemyZones == null || enemyZones.Length == 0)
            {

                return false;
            }

            // If we don't have a target yet, pick the closest zone
            if (!hasTarget)
            {
                searchTarget = GetClosestFlagZone(enemyZones);
                currentZoneIndex = GetZoneIndex(enemyZones, searchTarget);
                hasTarget = true;
                hasSearchedCurrentZone = false;

            }

            // Check if we've reached the current zone
            bool atCurrentZone = Vector3.Distance(agent.transform.position, searchTarget) < 3f;

            if (atCurrentZone)
            {
                hasSearchedCurrentZone = true;

                // Check if flags have spawned and if flag is at this zone
                if (CaptureTheFlag.Instance != null)
                {
                    // Look for flag at current zone
                    bool flagFoundHere = false;
                    foreach (var flag in flags)
                    {
                        if (flag.Team != agent.MyDetectable.TeamID &&
                            Vector3.Distance(flag.Position, searchTarget) < 5f)
                        {
                            flagFoundHere = true;
                            break;
                        }
                    }

                    // If we've searched this zone and no flag here, move to next zone
                    if (hasSearchedCurrentZone && !flagFoundHere)
                    {
                        searchTarget = GetNextFlagZoneToSearch(enemyZones);
                        hasSearchedCurrentZone = false;
                      
                    }
                }
            }

            agent.MoveTo(searchTarget);
            return true;
        }

        public override bool IsComplete()
        {
            var flags = agent.GetVisibleFlagsSnapShot();
            foreach (var flag in flags)
            {
                if (flag.Team != agent.MyDetectable.TeamID)
                {
                    return true;
                }
            }
            return false;
        }

        private FlagZone[] GetEnemyFlagZones()
        {
            if (agent.MyDetectable.TeamID == Team.Red)
            {
                // We're red team, so search blue flag zones
                return CaptureTheFlag.Instance.BlueFlagZones;
            }
            else
            {
                // We're blue team, so search red flag zones
                return CaptureTheFlag.Instance.RedFlagZones;
            }
        }

        private Vector3 GetClosestFlagZone(FlagZone[] zones)
        {
            if (zones == null || zones.Length == 0) return agent.transform.position;

            Vector3 closestZone = zones[0].transform.position;
            float closestDistance = Vector3.Distance(agent.transform.position, closestZone);

            for (int i = 1; i < zones.Length; i++)
            {
                float distance = Vector3.Distance(agent.transform.position, zones[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestZone = zones[i].transform.position;
                }
            }

            return closestZone;
        }

        private int GetZoneIndex(FlagZone[] zones, Vector3 targetPosition)
        {
            for (int i = 0; i < zones.Length; i++)
            {
                if (Vector3.Distance(zones[i].transform.position, targetPosition) < 1f)
                {
                    return i;
                }
            }
            return 0;
        }

        private Vector3 GetNextFlagZoneToSearch(FlagZone[] enemyZones)
        {
            if (enemyZones == null || enemyZones.Length == 0)
            {

                return agent.transform.position;
            }

            // Move to next zone in sequence
            currentZoneIndex = (currentZoneIndex + 1) % enemyZones.Length;
            Vector3 targetZone = enemyZones[currentZoneIndex].transform.position;

            return targetZone;
        }
    }

    public class PatrolAction : Action
    {
        private Vector3 patrolTarget;
        private bool hasTarget = false;

        public PatrolAction(BaseAI agent) : base(agent, "Patrol", 0.5f)
        {
            // Only patrol when enemy flag is taken by someone else
            preconditions.SetState(StateKeys.ENEMY_FLAG_TAKEN_BY_OTHER, true);
        }

        protected override System.Collections.Generic.Dictionary<string, object> GetEffects()
        {
            var effects = new System.Collections.Generic.Dictionary<string, object>();
            // Patrol doesn't change world state, just keeps us busy
            return effects;
        }

        public override bool Execute()
        {
            // Pick a new patrol target if we don't have one or reached current one
            if (!hasTarget || Vector3.Distance(agent.transform.position, patrolTarget) < 3f)
            {
                patrolTarget = GetRandomPatrolPoint();
                hasTarget = true;
            }

            agent.MoveTo(patrolTarget);
            return true;
        }

        public override bool IsComplete()
        {
            // Patrol never completes, it's an ongoing action
            return false;
        }

        private Vector3 GetRandomPatrolPoint()
        {
            // Random patrol around the map
            Vector3 randomPos = agent.transform.position + Random.insideUnitSphere * 15f;
            randomPos.y = agent.transform.position.y;

            // Keep within reasonable bounds
            randomPos.x = Mathf.Clamp(randomPos.x, -50f, 50f);
            randomPos.z = Mathf.Clamp(randomPos.z, -50f, 50f);

            return randomPos;
        }
    }
}