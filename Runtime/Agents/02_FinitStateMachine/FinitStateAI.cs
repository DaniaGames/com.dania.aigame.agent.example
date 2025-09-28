using AIGame.Core;

namespace AIGame.Examples.FSM
{
    public class FinitStateAI : BaseAI
    {
        /// <summary>
        /// Cached reference to the idle state for quick resetting after death.
        /// </summary>
        private Idle idle;

        private FSM fsm;

        /// <inheritdoc/>
        protected override void StartAI()
        {
            fsm = new FSM();

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
            fsm.AddTransition(moveToObjective, AICondition.Protect, protectObjective);
            fsm.AddTransition(idle, AICondition.Spawned, moveToObjective);
            fsm.AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            fsm.AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            fsm.AddTransition(combat, AICondition.MoveToObjective, moveToObjective);
            fsm.AddTransition(protectObjective, AICondition.SeesEnemy, combat);

            // Set initial state
            fsm.ChangeState(moveToObjective);
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
            fsm.ChangeState(idle);
        }

        /// <summary>
        /// Called when the objective is reached.
        /// Switches to the protect-objective state.
        /// </summary>
        private void OnObjectiveReached()
        {
            fsm.SetCondition(AICondition.Protect);
        }

        /// <summary>
        /// Called when an enemy enters the AI's vision range.
        /// Switches to combat state.
        /// </summary>
        private void OnEnemyEnterVision()
        {
            fsm.SetCondition(AICondition.SeesEnemy);
        }

        /// <summary>
        /// Called when there are no more visible enemies.
        /// Switches to move-to-objective state.
        /// </summary>
        private void OnNomoreEnemies()
        {
            fsm.SetCondition(AICondition.MoveToObjective);

        }

        /// <summary>
        /// Called when the AI respawns.
        /// Switches to spawned state.
        /// </summary>
        private void OnSpawned()
        {
            fsm.SetCondition(AICondition.Spawned);
        }

        protected override void ExecuteAI()
        {
            fsm.Execute();
        }

        public bool EatPowerUp(int id)
        {
            return TryConsumePowerup(id);
        }
    }
}

