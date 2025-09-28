using System.Collections.Generic;
using UnityEngine;
using AIGame.Core;

namespace AIGame.Examples.ModelBased
{
    [System.Serializable]
    public class KnownPowerUp
    {
        public Vector3 position;
        public float lastSeenTime;
        public bool collected;

        public KnownPowerUp(Vector3 pos, float time)
        {
            position = pos;
            lastSeenTime = time;
            collected = false;
        }

        public bool IsStillValid()
        {
            // PowerUps might respawn, so old data becomes invalid
            return !collected && (Time.time - lastSeenTime) < 30f;
        }
    }

    [System.Serializable]
    public class KnownEnemy
    {
        public Vector3 lastSeenPosition;
        public float lastSeenTime;
        public int enemyId;

        public KnownEnemy(int id, Vector3 pos, float time)
        {
            enemyId = id;
            lastSeenPosition = pos;
            lastSeenTime = time;
        }

        public bool IsRecentSighting()
        {
            // Only consider enemy positions recent if seen within 5 seconds
            return (Time.time - lastSeenTime) < 5f;
        }
    }

    public class WorldModel
    {
        private Dictionary<int, KnownPowerUp> knownPowerUps = new Dictionary<int, KnownPowerUp>();
        private Dictionary<int, KnownEnemy> knownEnemies = new Dictionary<int, KnownEnemy>();

        public void UpdatePowerUp(int id, Vector3 position)
        {
            if (knownPowerUps.ContainsKey(id))
            {
                knownPowerUps[id].position = position;
                knownPowerUps[id].lastSeenTime = Time.time;
                knownPowerUps[id].collected = false; // Reset if we see it again
            }
            else
            {
                knownPowerUps[id] = new KnownPowerUp(position, Time.time);
            }
        }

        public void MarkPowerUpCollected(int id)
        {
            if (knownPowerUps.ContainsKey(id))
            {
                knownPowerUps[id].collected = true;
            }
        }

        public void UpdateEnemy(int enemyId, Vector3 position)
        {
            if (knownEnemies.ContainsKey(enemyId))
            {
                knownEnemies[enemyId].lastSeenPosition = position;
                knownEnemies[enemyId].lastSeenTime = Time.time;
            }
            else
            {
                knownEnemies[enemyId] = new KnownEnemy(enemyId, position, Time.time);
            }
        }

        public Vector3? GetClosestKnownPowerUp(Vector3 myPosition)
        {
            Vector3? closest = null;
            float closestDistance = float.MaxValue;

            foreach (var powerUp in knownPowerUps.Values)
            {
                if (powerUp.IsStillValid())
                {
                    float distance = Vector3.Distance(myPosition, powerUp.position);
                    if (distance < closestDistance)
                    {
                        closest = powerUp.position;
                        closestDistance = distance;
                    }
                }
            }

            return closest;
        }

        public Vector3? GetMostRecentEnemyPosition()
        {
            KnownEnemy mostRecent = null;
            float mostRecentTime = 0f;

            foreach (var enemy in knownEnemies.Values)
            {
                if (enemy.IsRecentSighting() && enemy.lastSeenTime > mostRecentTime)
                {
                    mostRecent = enemy;
                    mostRecentTime = enemy.lastSeenTime;
                }
            }

            return mostRecent?.lastSeenPosition;
        }

        public void CleanupOldData()
        {
            // Remove old powerup data
            var powerUpsToRemove = new List<int>();
            foreach (var kvp in knownPowerUps)
            {
                if (!kvp.Value.IsStillValid())
                {
                    powerUpsToRemove.Add(kvp.Key);
                }
            }
            foreach (int id in powerUpsToRemove)
            {
                knownPowerUps.Remove(id);
            }

            // Remove old enemy data
            var enemiesToRemove = new List<int>();
            foreach (var kvp in knownEnemies)
            {
                if (!kvp.Value.IsRecentSighting())
                {
                    enemiesToRemove.Add(kvp.Key);
                }
            }
            foreach (int id in enemiesToRemove)
            {
                knownEnemies.Remove(id);
            }
        }

        public int GetKnownPowerUpCount()
        {
            int count = 0;
            foreach (var powerUp in knownPowerUps.Values)
            {
                if (powerUp.IsStillValid()) count++;
            }
            return count;
        }

        public int GetKnownEnemyCount()
        {
            int count = 0;
            foreach (var enemy in knownEnemies.Values)
            {
                if (enemy.IsRecentSighting()) count++;
            }
            return count;
        }
    }
}