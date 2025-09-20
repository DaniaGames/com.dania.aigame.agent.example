using System;
using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BehaviorTree
{
    /// <summary>
    /// Centralized data storage system for behavior trees.
    /// Provides type-safe storage and retrieval of shared data between nodes.
    /// </summary>
    public class Blackboard
    {
        /// <summary>
        /// Internal storage for all blackboard data.
        /// </summary>
        private readonly Dictionary<string, object> data;

        /// <summary>
        /// Event triggered when data is changed on the blackboard.
        /// Useful for reactive behaviors and debugging.
        /// </summary>
        public event Action<string, object> OnDataChanged;

        /// <summary>
        /// Creates a new blackboard instance.
        /// </summary>
        public Blackboard()
        {
            data = new Dictionary<string, object>();
        }

        /// <summary>
        /// Stores a value in the blackboard with type safety.
        /// </summary>
        /// <typeparam name="T">Type of the value to store.</typeparam>
        /// <param name="key">Unique identifier for the data.</param>
        /// <param name="value">Value to store.</param>
        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("Blackboard: Attempted to set value with null or empty key");
                return;
            }

            data[key] = value;
            OnDataChanged?.Invoke(key, value);
        }

        /// <summary>
        /// Retrieves a value from the blackboard with type safety.
        /// </summary>
        /// <typeparam name="T">Expected type of the value.</typeparam>
        /// <param name="key">Unique identifier for the data.</param>
        /// <returns>The stored value, or default(T) if not found or wrong type.</returns>
        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("Blackboard: Attempted to get value with null or empty key");
                return default(T);
            }

            if (data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
                else
                {
                    Debug.LogWarning($"Blackboard: Type mismatch for key '{key}'. Expected {typeof(T)}, got {value?.GetType()}");
                    return default(T);
                }
            }

            return default(T);
        }

        /// <summary>
        /// Retrieves a value with a fallback default if not found.
        /// </summary>
        /// <typeparam name="T">Expected type of the value.</typeparam>
        /// <param name="key">Unique identifier for the data.</param>
        /// <param name="defaultValue">Value to return if key not found.</param>
        /// <returns>The stored value or the provided default.</returns>
        public T Get<T>(string key, T defaultValue)
        {
            if (HasKey(key))
            {
                return Get<T>(key);
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a key exists in the blackboard.
        /// </summary>
        /// <param name="key">Key to check for.</param>
        /// <returns>True if the key exists.</returns>
        public bool HasKey(string key)
        {
            return !string.IsNullOrEmpty(key) && data.ContainsKey(key);
        }

        /// <summary>
        /// Removes a value from the blackboard.
        /// </summary>
        /// <param name="key">Key to remove.</param>
        /// <returns>True if the key was found and removed.</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            bool removed = data.Remove(key);
            if (removed)
            {
                OnDataChanged?.Invoke(key, null);
            }
            return removed;
        }

        /// <summary>
        /// Clears all data from the blackboard.
        /// </summary>
        public void Clear()
        {
            data.Clear();
            OnDataChanged?.Invoke("*", null); // Special key to indicate full clear
        }

        /// <summary>
        /// Gets all keys currently stored in the blackboard.
        /// Useful for debugging and inspection.
        /// </summary>
        /// <returns>Collection of all keys.</returns>
        public IEnumerable<string> GetAllKeys()
        {
            return data.Keys;
        }

        /// <summary>
        /// Gets the number of entries in the blackboard.
        /// </summary>
        public int Count => data.Count;

        // Convenience methods for common AI data types

        /// <summary>
        /// Sets the list of currently visible enemies.
        /// </summary>
        /// <param name="enemies">Collection of perceived enemy agents.</param>
        public void SetVisibleEnemies(IReadOnlyList<PerceivedAgent> enemies)
        {
            Set("VisibleEnemies", enemies ?? new List<PerceivedAgent>());
        }

        /// <summary>
        /// Gets the list of currently visible enemies.
        /// </summary>
        /// <returns>Read-only list of enemy agents, or empty list if none.</returns>
        public IReadOnlyList<PerceivedAgent> GetVisibleEnemies()
        {
            return Get("VisibleEnemies", (IReadOnlyList<PerceivedAgent>)new List<PerceivedAgent>());
        }

        /// <summary>
        /// Sets the list of currently visible allies.
        /// </summary>
        /// <param name="allies">Collection of perceived ally agents.</param>
        public void SetVisibleAllies(IReadOnlyList<PerceivedAgent> allies)
        {
            Set("VisibleAllies", allies ?? new List<PerceivedAgent>());
        }

        /// <summary>
        /// Gets the list of currently visible allies.
        /// </summary>
        /// <returns>Read-only list of ally agents, or empty list if none.</returns>
        public IReadOnlyList<PerceivedAgent> GetVisibleAllies()
        {
            return Get("VisibleAllies", (IReadOnlyList<PerceivedAgent>)new List<PerceivedAgent>());
        }

        /// <summary>
        /// Sets the current target for combat actions.
        /// </summary>
        /// <param name="target">Target agent, or null to clear target.</param>
        public void SetCurrentTarget(PerceivedAgent? target)
        {
            Set("CurrentTarget", target);
        }

        /// <summary>
        /// Gets the current combat target.
        /// </summary>
        /// <returns>Current target, or null if no target set.</returns>
        public PerceivedAgent? GetCurrentTarget()
        {
            return Get<PerceivedAgent?>("CurrentTarget");
        }

        /// <summary>
        /// Sets the agent's current world position.
        /// </summary>
        /// <param name="position">World position vector.</param>
        public void SetMyPosition(Vector3 position)
        {
            Set("MyPosition", position);
        }

        /// <summary>
        /// Gets the agent's current world position.
        /// </summary>
        /// <returns>World position vector.</returns>
        public Vector3 GetMyPosition()
        {
            return Get("MyPosition", Vector3.zero);
        }

        /// <summary>
        /// Sets the current objective position.
        /// </summary>
        /// <param name="position">Objective world position.</param>
        public void SetObjectivePosition(Vector3 position)
        {
            Set("ObjectivePosition", position);
        }

        /// <summary>
        /// Gets the current objective position.
        /// </summary>
        /// <returns>Objective world position.</returns>
        public Vector3 GetObjectivePosition()
        {
            return Get("ObjectivePosition", Vector3.zero);
        }


        /// <summary>
        /// Stores enemy memory for position prediction and tactical planning.
        /// </summary>
        /// <param name="memory">List of remembered enemy positions over time.</param>
        public void SetEnemyMemory(List<PerceivedAgent> memory)
        {
            Set("EnemyMemory", memory ?? new List<PerceivedAgent>());
        }

        /// <summary>
        /// Gets stored enemy memory.
        /// </summary>
        /// <returns>List of remembered enemy agents.</returns>
        public List<PerceivedAgent> GetEnemyMemory()
        {
            return Get("EnemyMemory", new List<PerceivedAgent>());
        }

        /// <summary>
        /// Sets the last time a dodge was performed (for cooldown management).
        /// </summary>
        /// <param name="time">Time when dodge was performed.</param>
        public void SetLastDodgeTime(float time)
        {
            Set("LastDodgeTime", time);
        }

        /// <summary>
        /// Gets the last time a dodge was performed.
        /// </summary>
        /// <returns>Time of last dodge, or 0 if never dodged.</returns>
        public float GetLastDodgeTime()
        {
            return Get("LastDodgeTime", 0f);
        }

        /// <summary>
        /// Provides a debug-friendly string representation of the blackboard contents.
        /// </summary>
        /// <returns>String representation of all stored data.</returns>
        public override string ToString()
        {
            var result = $"Blackboard ({data.Count} entries):\n";
            foreach (var kvp in data)
            {
                result += $"  {kvp.Key}: {kvp.Value?.ToString() ?? "null"}\n";
            }
            return result;
        }
    }
}