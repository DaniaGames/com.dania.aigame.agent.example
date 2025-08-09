using AIGame.Core;

namespace AIGame.Examples
{

    /// <summary>
    /// A MEDIUM AI that will run to point and attack unfriendly targets.
    /// </summary>
    public class MediumAI : ExampleAI
    {
        protected override void StartAI()
        {
            //Create states
            Idle idle = new Idle(this);

            MoveToObjective moveToObjective = new MoveToObjective(this);
            Combat combat = new Combat(this);

            //Create listeners
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            combat.NoMoreEnemies += () => OnNomoreEnemies();
            EnemyEnterVision += () => OnEnemyEnterVision();

            //Create transitions
            AddTransition(moveToObjective, AICondition.Idle, idle);
            AddTransition(idle, AICondition.Idle, moveToObjective);
            AddTransition(moveToObjective, AICondition.SeesEnemy, combat);
            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);
            AddTransition(combat, AICondition.MoveToObjective, moveToObjective);

            //Default state
            ChangeState(moveToObjective);
        }

        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ProjectileRange, 5);
            AllocateStat(StatType.ReloadSpeed, 5);
        }

        private void OnObjectiveReached()
        {
            SetCondition(AICondition.Idle);
        }

        private void OnNomoreEnemies()
        {
            SetCondition(AICondition.MoveToObjective);
        }

        private void OnEnemyEnterVision()
        {
            SetCondition(AICondition.SeesEnemy);
        }

        public void OnRespawned()
        {
            SetCondition(AICondition.MoveToObjective);
        }
    }
}