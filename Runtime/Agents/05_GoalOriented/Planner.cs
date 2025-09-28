using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIGame.Examples.GoalOriented
{
    public class Plan
    {
        public List<Action> Actions { get; private set; }
        public float TotalCost { get; private set; }

        public Plan()
        {
            Actions = new List<Action>();
            TotalCost = 0f;
        }

        public void AddAction(Action action)
        {
            Actions.Add(action);
            TotalCost += action.Cost;
        }

        public bool IsEmpty()
        {
            return Actions.Count == 0;
        }

        public Action GetCurrentAction()
        {
            return Actions.Count > 0 ? Actions[0] : null;
        }

        public void RemoveCurrentAction()
        {
            if (Actions.Count > 0)
            {
                Actions.RemoveAt(0);
            }
        }

        public override string ToString()
        {
            string result = $"Plan (Cost: {TotalCost}): ";
            foreach (var action in Actions)
            {
                result += action.Name + " -> ";
            }
            return result + "GOAL";
        }
    }

    public class Planner
    {
        public Plan CreatePlan(WorldState currentState, WorldState goalState, List<Action> availableActions)
        {
            if (currentState.Satisfies(goalState))
            {
                return new Plan(); // Already at goal
            }

            // Simple greedy planning - not optimal but fast and demonstrates the concept
            List<Action> planActions = new List<Action>();
            WorldState workingState = currentState.Clone();

            int maxIterations = 10; // Prevent infinite loops
            int iterations = 0;

            while (!workingState.Satisfies(goalState) && iterations < maxIterations)
            {
                iterations++;

                Action bestAction = null;
                float bestScore = float.MinValue;

                // Find the best action to take from current state
                foreach (var action in availableActions)
                {
                    if (action.CanExecute(workingState))
                    {
                        // Score the action based on how much it helps us reach the goal
                        float score = ScoreAction(action, workingState, goalState);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestAction = action;
                        }
                    }

                }

                if (bestAction != null)
                {
                    planActions.Add(bestAction);
                    workingState = bestAction.ApplyEffects(workingState);
               
                }
                else
                {

                    break;
                }
            }

            Plan plan = new Plan();
            foreach (var action in planActions)
            {
                plan.AddAction(action);
            }


            return plan;
        }

        private float ScoreAction(Action action, WorldState currentState, WorldState goalState)
        {
            WorldState resultState = action.ApplyEffects(currentState);

            // Basic scoring: how many goal conditions does this action help achieve?
            float score = 0f;

            // Check if this action gets us closer to the goal
            if (resultState.Satisfies(goalState))
            {
                score += 100f; // High score for reaching the goal
            }

            // Subtract cost to prefer cheaper actions
            score -= action.Cost;
            // Add specific bonuses for flag capture actions
            if (action is ReturnToBaseAction && currentState.GetState<bool>(StateKeys.HAS_ENEMY_FLAG))
            {
                score += 80f; // Highest priority when actually carrying the flag
            }
            else if (action is MoveToEnemyFlagAction)
            {
                // High priority if flag is dropped (we know exact location)
                if (currentState.GetState<bool>(StateKeys.ENEMY_FLAG_DROPPED))
                {
                    score += 80f; // Highest priority for dropped flag
                }
                else if (currentState.GetState<bool>(StateKeys.KNOW_ENEMY_FLAG_LOCATION) && !currentState.GetState<bool>(StateKeys.HAS_ENEMY_FLAG))
                {
                    score += 70f; // Very high priority when we can see the flag
                }
            }
            else if (action is FindEnemyFlagAction && !currentState.GetState<bool>(StateKeys.KNOW_ENEMY_FLAG_LOCATION))
            {
                // Only search if flags have spawned
                if (currentState.GetState<bool>(StateKeys.FLAGS_HAVE_SPAWNED))
                {
                    // Don't search if flag is taken by someone else
                    if (!currentState.GetState<bool>(StateKeys.ENEMY_FLAG_TAKEN_BY_OTHER))
                    {
                        score += 20f; // Need to find the flag first
                    }
                    else
                    {
                        score -= 10f; // Avoid searching when flag is taken
                    }
                }
                else
                {
                    score -= 50f; // Heavy penalty for searching before flags spawn
                }
            }
            else if (action is IdleAction)
            {
                // Prefer idle when flags haven't spawned yet
                if (!currentState.GetState<bool>(StateKeys.FLAGS_HAVE_SPAWNED))
                {
                    score += 10f; // Positive score to wait until flags spawn
                }
                else
                {
                    score -= 100f; // Lowest priority when flags are available
                }
            }
            return score;
        }



    }
}