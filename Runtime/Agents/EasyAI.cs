using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// AIClass for an EASY agent that just runs to point
    /// </summary>
    class EasyAI : ExampleAI
    {
        protected override void StartAI()
        { 
            //Create states
            Idle idle = new Idle(this);

            MoveToObjective moveToObjective = new MoveToObjective(this);

            //Create listeners
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            Respawned += () => OnRespawned();
         
            //Create transitions
            AddTransition(moveToObjective, AICondition.Idle, idle);
            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);

            AddTransition(idle, AICondition.MoveToObjective, moveToObjective);
            //Default state
            ChangeState(moveToObjective);

        }


        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 20);
        }

        public void OnObjectiveReached()
        {
            SetCondition(AICondition.Idle);
        }

        public void OnRespawned()
        {
            SetCondition(AICondition.MoveToObjective);
        }
    }
}


