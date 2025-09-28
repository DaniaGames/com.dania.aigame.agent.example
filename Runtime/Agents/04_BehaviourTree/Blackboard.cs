using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.BeheaviourTree
{
    public class Blackboard
    {
        private static Blackboard sharedInstance;
        private Dictionary<string, object> data = new Dictionary<string, object>();

        public static Blackboard GetShared(BaseAI caller)
        {
            // Only allow BehaviourTreeAI to access the shared blackboard
            if (caller is BehaviourTreeAI)
            {
                if (sharedInstance == null)
                    sharedInstance = new Blackboard();
                return sharedInstance;
            }
            return null;
        }

        private Blackboard() { } // Private constructor

        public void SetValue<T>(string key, T value)
        {
            data[key] = value;
        }

        public T GetValue<T>(string key)
        {
            if (data.ContainsKey(key))
            {
                return (T)data[key];
            }
            return default(T);
        }

        public bool HasKey(string key)
        {
            return data.ContainsKey(key);
        }

        public void RemoveKey(string key)
        {
            data.Remove(key);
        }
    }
}