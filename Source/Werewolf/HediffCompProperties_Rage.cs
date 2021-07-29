using Verse;

namespace Werewolf
{
    public class HediffCompProperties_Rage : HediffCompProperties
    {
        public float baseRageSeconds;

        public HediffCompProperties_Rage()
        {
            compClass = typeof(HediffComp_Rage);
        }
    }
}