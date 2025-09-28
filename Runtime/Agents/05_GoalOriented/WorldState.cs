using System.Collections.Generic;
using UnityEngine;

namespace AIGame.Examples.GoalOriented
{
    [System.Serializable]
    public class WorldState
    {
        private Dictionary<string, object> state = new Dictionary<string, object>();

        /// <summary>
        /// Sets a state variable to the specified value.
        /// </summary>
        /// <param name="key">The state variable key.</param>
        /// <param name="value">The value to set.</param>
        public void SetState(string key, object value)
        {
            state[key] = value;
        }

        /// <summary>
        /// Gets a state variable of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="key">The state variable key.</param>
        /// <returns>The state value cast to type T, or default(T) if not found.</returns>
        public T GetState<T>(string key)
        {
            if (state.ContainsKey(key))
            {
                return (T)state[key];
            }
            return default(T);
        }

        /// <summary>
        /// Checks if a state variable exists.
        /// </summary>
        /// <param name="key">The state variable key to check.</param>
        /// <returns>True if the state variable exists, false otherwise.</returns>
        public bool HasState(string key)
        {
            return state.ContainsKey(key);
        }

        /// <summary>
        /// Creates a deep copy of this world state.
        /// </summary>
        /// <returns>A new WorldState with the same key-value pairs.</returns>
        public WorldState Clone()
        {
            WorldState clone = new WorldState();
            foreach (var kvp in state)
            {
                clone.state[kvp.Key] = kvp.Value;
            }
            return clone;
        }

        /// <summary>
        /// Checks if this world state satisfies all conditions in the goal state.
        /// </summary>
        /// <param name="goal">The goal state to check against.</param>
        /// <returns>True if all goal conditions are met, false otherwise.</returns>
        public bool Satisfies(WorldState goal)
        {
            foreach (var kvp in goal.state)
            {
                if (!state.ContainsKey(kvp.Key) || !state[kvp.Key].Equals(kvp.Value))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a string representation of this world state.
        /// </summary>
        /// <returns>A formatted string showing all state variables and values.</returns>
        public override string ToString()
        {
            string result = "WorldState: ";
            foreach (var kvp in state)
            {
                result += $"{kvp.Key}={kvp.Value}, ";
            }
            return result;
        }
    }

    // Predefined world state keys for flag capture
    public static class StateKeys
    {
        public const string HAS_ENEMY_FLAG = "HasEnemyFlag";
        public const string AT_HOME_BASE = "AtHomeBase";
        public const string ENEMY_FLAG_CAPTURED = "EnemyFlagCaptured";
        public const string KNOW_ENEMY_FLAG_LOCATION = "KnowEnemyFlagLocation";
        public const string ENEMY_FLAG_POSITION = "EnemyFlagPosition";
        public const string FLAGS_HAVE_SPAWNED = "FlagsHaveSpawned";
        public const string ENEMY_FLAG_DROPPED = "EnemyFlagDropped";
        public const string ENEMY_FLAG_TAKEN_BY_OTHER = "EnemyFlagTakenByOther";
    }
}