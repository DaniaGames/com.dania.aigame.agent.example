using AIGame.Core;

namespace AIGame.Examples
{
    /// <summary>
    /// A Hard AI that will Run to point attack unfriendly targets and dodge incoming balls
    /// </summary>
    public class HardAI : ExampleAI
    {
        private Idle idle;
        protected override void StartAI()
        {
            //Create states
            Strafe strafe = new Strafe(this);

            idle = new Idle(this);

            Dodge dodge = new Dodge(this);

            FollowEnemy follow = new FollowEnemy(this);


            Combat combat = new Combat(this, strafe, follow, dodge);


            MoveToObjective moveToObjective = new MoveToObjective(this, dodge);

            ProtectObjective protectObjective = new ProtectObjective(this, dodge);


            //Create listeners
            moveToObjective.DestinationReached += () => OnObjectiveReached();
            EnemyEnterVision += () => OnEnemyEnterVision();
            combat.NoMoreEnemies += () => OnNomoreEnemies();
            BallDetected += (ball) => dodge.OnBallDetected(ball);
            Death += () => Ondeath();
            Respawned += () => OnSpawned();

            //Create transitions
            AddTransition(moveToObjective, AICondition.Protect, protectObjective);

            AddTransition(idle, AICondition.Spawned, moveToObjective);

            AddTransition(moveToObjective, AICondition.MoveToObjective, moveToObjective);

            AddTransition(moveToObjective, AICondition.SeesEnemy, combat);

            AddTransition(combat, AICondition.MoveToObjective, moveToObjective);

            AddTransition(protectObjective, AICondition.SeesEnemy, combat);

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

        private void Ondeath()
        {
            ChangeState(idle);
        }

        private void OnObjectiveReached()
        {
            SetCondition(AICondition.Protect);
        }

        private void OnEnemyEnterVision()
        {
            SetCondition(AICondition.SeesEnemy);
        }

        private void OnNomoreEnemies()
        {
            SetCondition(AICondition.MoveToObjective);
        }

        private void OnSpawned()
        {
            SetCondition(AICondition.Spawned);
        }
    }
}