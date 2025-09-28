using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.GoalOriented
{
    public class GoalOrientedAI : BaseAI
    {
        private Planner planner;
        private Plan currentPlan;

        private FlagCarrier fc;

        private List<Action> availableActions;
        private WorldState currentGoal;

        private float lastPlanTime = 0f;
        private const float REPLAN_INTERVAL = 2f; // Replan every 2 seconds
        private bool flagsHaveSpawned = false;
        private bool enemyFlagVisible = false;
        private bool enemyFlagDropped = false;
        private bool enemyFlagTakenByOther = false;
        private Vector3 droppedFlagPosition;

        protected override void StartAI()
        {
            planner = new Planner();
            fc = GetComponent<FlagCarrier>();

            // Initialize available actions
            availableActions = new List<Action>
            {
                new IdleAction(this),
                new FindEnemyFlagAction(this),
                new MoveToEnemyFlagAction(this),
                new ReturnToBaseAction(this)
            };

            // Set main goal: capture the enemy flag
            currentGoal = new WorldState();

            currentGoal.SetState(StateKeys.ENEMY_FLAG_CAPTURED, true);

            // Subscribe to game events
            CaptureTheFlag.Instance.FlagsSpawned += OnFlagsSpawned;
            FlagEnter += OnFlagDiscovered;
            FlagExit += OnFlagLost;

            CaptureTheFlag.Instance.FlagDropped += OnFlagDrop;
            CaptureTheFlag.Instance.FlagPickedUp += OnFlagPickup;
            CaptureTheFlag.Instance.FlagReturned += OnFlagReturned;

            // Subscribe to local flag carrier events
            fc.Pickup += OnLocalFlagPickup;
            fc.Drop += OnLocalFlagDrop;

            // Subscribe to respawn event
            Respawned += OnRespawned;

        }

        private void OnFlagsSpawned()
        {
            flagsHaveSpawned = true;
        }

        private void OnRespawned()
        {
            // Reset only AI-specific state, not global game state
            enemyFlagVisible = false; // AI's personal vision state
            currentPlan = null;       // Clear old plan
            lastPlanTime = 0f;        // Reset planning timer

            // Don't reset enemyFlagDropped or enemyFlagTakenByOther
            // as these are global game states that persist

        }

        private void OnFlagDiscovered(Team team)
        {
            if (team != MyDetectable.TeamID)
            {
                enemyFlagVisible = true;
                currentPlan = null;
            }
        }

        private void OnFlagLost(Team team)
        {
            if (team != MyDetectable.TeamID)
            {
                enemyFlagVisible = false;
            }
        }

        private void OnFlagPickup(Team flagTeam)
        {
            // Always reset enemyFlagDropped when ANY flag gets picked up
            enemyFlagDropped = false;

            // Only handle when other agents pick up flags (not me)
            if (flagTeam != MyDetectable.TeamID && (fc == null || !fc.HasFlag))
            {
                enemyFlagTakenByOther = true;
                currentPlan = null;
            }
        }

        private void OnFlagDrop(Team flagTeam)
        {
            if (flagTeam != MyDetectable.TeamID)
            {
                Vector3? droppedPosition = CaptureTheFlag.Instance.GetDiscoverableEnemyFlagPosition(this);

                if (droppedPosition.HasValue)
                {
                    droppedFlagPosition = droppedPosition.Value;
                    enemyFlagDropped = true;
                    enemyFlagTakenByOther = false;
                    currentPlan = null;
                }
            }
        }

        private void OnFlagReturned(Team flagTeam)
        {
            enemyFlagDropped = false;
            currentPlan = null;
        }

        private void OnLocalFlagPickup()
        {
            enemyFlagDropped = false;
            enemyFlagTakenByOther = false;
            enemyFlagVisible = false;
            currentPlan = null;
        }

        private void OnLocalFlagDrop()
        {
            enemyFlagVisible = false;
            currentPlan = null;
        }

        protected override void ConfigureStats()
        {
            // Balanced stats with focus on speed for flag running
            AllocateStat(StatType.Speed, 18);       // High speed for flag capture
            AllocateStat(StatType.VisionRange, 2); // Good vision to find flag
        }

        protected override void ExecuteAI()
        {
            if (!IsAlive) return;

            // Start planning immediately - AI can move to flag zones even before flags spawn
            if (currentPlan == null || currentPlan.IsEmpty() || Time.time - lastPlanTime > REPLAN_INTERVAL)
            {
                CreateNewPlan();
                lastPlanTime = Time.time;
            }

            // Execute current action
            if (currentPlan != null && !currentPlan.IsEmpty())
            {
                ExecuteCurrentAction();
            }
        }

        private void CreateNewPlan()
        {
            WorldState currentWorldState = AssessCurrentWorldState();
            currentPlan = planner.CreatePlan(currentWorldState, currentGoal, availableActions);
        }

        private WorldState AssessCurrentWorldState()
        {
            WorldState state = new WorldState();
            // Check if we have the enemy flag
            var flagCarrier = GetComponent<FlagCarrier>();
            bool hasEnemyFlag = flagCarrier != null && flagCarrier.HasFlag;
            state.SetState(StateKeys.HAS_ENEMY_FLAG, hasEnemyFlag);

            // Check if we're at home base
            Vector3? homePos = CaptureTheFlag.Instance.GetOwnFlagPosition(this);
            bool atHomeBase = false;
            if (homePos.HasValue)
            {
                atHomeBase = Vector3.Distance(transform.position, homePos.Value) < 5f;
            }
            state.SetState(StateKeys.AT_HOME_BASE, atHomeBase);

            // Check if we can see the enemy flag (using event-based detection)
            state.SetState(StateKeys.KNOW_ENEMY_FLAG_LOCATION, enemyFlagVisible);

            // If flag is visible, get its position
            if (enemyFlagVisible)
            {
                var visibleFlags = GetVisibleFlagsSnapShot();
                foreach (var flag in visibleFlags)
                {
                    if (flag.Team != MyDetectable.TeamID)
                    {
                        state.SetState(StateKeys.ENEMY_FLAG_POSITION, flag.Position);
                        break;
                    }
                }
            }

            // Check for nearby enemies
            var visibleEnemies = GetVisibleEnemiesSnapshot();
            bool enemiesNearby = visibleEnemies.Count > 0;

            // Check if flags have spawned
            state.SetState(StateKeys.FLAGS_HAVE_SPAWNED, flagsHaveSpawned);

            // Check flag status
            state.SetState(StateKeys.ENEMY_FLAG_DROPPED, enemyFlagDropped);
            state.SetState(StateKeys.ENEMY_FLAG_TAKEN_BY_OTHER, enemyFlagTakenByOther);

            // If flag is dropped, set its known position
            if (enemyFlagDropped)
            {
                state.SetState(StateKeys.ENEMY_FLAG_POSITION, droppedFlagPosition);
                state.SetState(StateKeys.KNOW_ENEMY_FLAG_LOCATION, true);
            }

            // Check if goal is achieved
            // Goal is achieved when we've captured the enemy flag and returned to base
            bool flagCaptured = hasEnemyFlag && atHomeBase;
            state.SetState(StateKeys.ENEMY_FLAG_CAPTURED, flagCaptured);

            return state;
        }



        private void ExecuteCurrentAction()
        {
            Action currentAction = currentPlan.GetCurrentAction();
            if (currentAction == null) return;


            // Execute the action
            bool actionStarted = currentAction.Execute();

            if (!actionStarted)
            {
                currentPlan.RemoveCurrentAction();
                return;
            }

            // Check if action is complete
            if (currentAction.IsComplete())
            {
                currentPlan.RemoveCurrentAction();
            }
        }
    }
}