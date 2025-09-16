using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// A "Medium" difficulty AI that:
    /// - Moves toward objectives
    /// - Engages in combat with enemies
    /// Uses a finite state machine with transitions based on in-game events.
    /// </summary>
    public class MediumAI : ExampleAI
    {
        /// <inheritdoc/>
        protected override void StartAI()
        {
            // --- Create states ---
            Idle idle = new Idle(this);
            MoveToObjective moveToObjective = new MoveToObjective(this);
            Combat combat = new Combat(this);

            // --- Create event listeners ---
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            combat.NoMoreEnemies += () => OnNomoreEnemies();
            EnemyEnterVision += () => OnEnemyEnterVision();

            // --- Create state transitions ---
            AddTransition(moveToObjective, AICondition.Idle, idle);
            AddTransition(idle, AICondition.Idle, moveToObjective);
            AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            AddTransition(combat, AICondition.MoveToObjective, moveToObjective);

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
        /// Called when the objective is reached.
        /// Switches to the idle state.
        /// </summary>
        private void OnObjectiveReached()
        {
            SetCondition(AICondition.Idle);
        }

        /// <summary>
        /// Called when there are no more visible enemies.
        /// Switches to the move-to-objective state.
        /// </summary>
        private void OnNomoreEnemies()
        {
            SetCondition(AICondition.MoveToObjective);
        }

        /// <summary>
        /// Called when an enemy enters the AI's vision range.
        /// Switches to the combat state.
        /// </summary>
        private void OnEnemyEnterVision()
        {
            SetCondition(AICondition.SeesEnemy);
        }

        /// <summary>
        /// Called when the AI respawns.
        /// Switches to the move-to-objective state.
        /// </summary>
        public void OnRespawned()
        {
            SetCondition(AICondition.MoveToObjective);
        }
    }
}
