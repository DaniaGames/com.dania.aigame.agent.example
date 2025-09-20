using AIGame.Core;


namespace Awesome.AI
{
    public class ReactiveAI : BaseAI
    {
        protected override void ConfigureStats()
        {
            AllocateStat(StatType.Speed, 5);
            AllocateStat(StatType.ProjectileRange, 5);
            AllocateStat(StatType.VisionRange, 5);
            AllocateStat(StatType.ReloadSpeed, 5);

        }

        protected override void ExecuteAI()
        {

        }

        protected override void StartAI()
        {

        }

    }
}

