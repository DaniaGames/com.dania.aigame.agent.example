using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// A "Hard" difficulty AI that:
    /// - Moves toward objectives
    /// - Engages in combat with enemies
    /// - Strafes and dodges incoming projectiles
    /// - Protects objectives when reached
    /// Uses a finite state machine with transitions triggered by in-game events.
    /// </summary>
    public class HardAI : ExampleAI
    {
        /// <summary>
        /// Cached reference to the idle state for quick resetting after death.
        /// </summary>
        private Idle idle;

        /// <inheritdoc/>
        protected override void StartAI()
        {
            // --- Create states ---
            Strafe strafe = new Strafe(this);
            idle = new Idle(this);
            Dodge dodge = new Dodge(this);
            FollowEnemy follow = new FollowEnemy(this);
            Combat combat = new Combat(this, strafe, follow, dodge);
            MoveToObjective moveToObjective = new MoveToObjective(this, dodge);
            ProtectObjective protectObjective = new ProtectObjective(this, dodge);

            // --- Create event listeners ---
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            EnemyEnterVision += () => OnEnemyEnterVision();
            combat.NoMoreEnemies += () => OnNomoreEnemies();
            BallDetected += (ball) => dodge.OnBallDetected(ball);
            Death += () => Ondeath();
            Respawned += () => OnSpawned();

            // --- Create state transitions ---
            AddTransition(moveToObjective, AICondition.Protect, protectObjective);
            AddTransition(idle, AICondition.Spawned, moveToObjective);
            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            AddTransition(combat, AICondition.MoveToObjective, moveToObjective);
            AddTransition(protectObjective, AICondition.SeesEnemy, combat);

            // Set initial state
            ChangeState(moveToObjective);
        }

        /// <inheritdoc/>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ProjectileRange, 5);
            AllocateStat(StatType.ReloadSpeed, 5);
        }

        /// <summary>
        /// Called when the AI dies. Resets to idle state.
        /// </summary>
        private void Ondeath()
        {
            ChangeState(idle);
        }

        /// <summary>
        /// Called when the objective is reached.
        /// Switches to the protect-objective state.
        /// </summary>
        private void OnObjectiveReached()
        {
            SetCondition(AICondition.Protect);
        }

        /// <summary>
        /// Called when an enemy enters the AI's vision range.
        /// Switches to combat state.
        /// </summary>
        private void OnEnemyEnterVision()
        {
            SetCondition(AICondition.SeesEnemy);
        }

        /// <summary>
        /// Called when there are no more visible enemies.
        /// Switches to move-to-objective state.
        /// </summary>
        private void OnNomoreEnemies()
        {
            SetCondition(AICondition.MoveToObjective);
        }

        /// <summary>
        /// Called when the AI respawns.
        /// Switches to spawned state.
        /// </summary>
        private void OnSpawned()
        {
            SetCondition(AICondition.Spawned);
        }
    }
}
