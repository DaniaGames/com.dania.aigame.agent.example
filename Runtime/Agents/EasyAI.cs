using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// AI class for an easy agent that simply runs toward the objective.
    /// </summary>
    class EasyAI : ExampleAI
    {
        /// <summary>
        /// Called when the AI is initialized.
        /// Responsible for creating states, setting up listeners, and defining transitions.
        /// </summary>
        protected override void StartAI()
        {
            // Create states
            Idle idle = new Idle(this);
            MoveToObjective moveToObjective = new MoveToObjective(this);

            // Create listeners
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            Respawned += () => OnRespawned();

            // Create transitions
            AddTransition(moveToObjective, AICondition.Idle, idle);
            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            AddTransition(idle, AICondition.MoveToObjective, moveToObjective);

            // Set default state
            ChangeState(moveToObjective);
        }

        /// <summary>
        /// Configures the base stats for this AI instance.
        /// </summary>
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 20);
        }

        /// <summary>
        /// Called when the AI reaches the objective.
        /// Switches state to <see cref="Idle"/>.
        /// </summary>
        public void OnObjectiveReached()
        {
            SetCondition(AICondition.Idle);
        }

        /// <summary>
        /// Called when the AI respawns.
        /// Switches state to <see cref="MoveToObjective"/>.
        /// </summary>
        public void OnRespawned()
        {
            SetCondition(AICondition.MoveToObjective);
        }
    }
}
