using RimWorld;
using Verse;
using Verse.AI;

namespace Werewolf
{
    public class MentalState_WerewolfFury : MentalState
    {
        public override bool ForceHostileTo(Thing t)
        {
            return true;
        }

        public override bool ForceHostileTo(Faction f)
        {
            return true;
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }
    }
}