using System.Text;
using Verse;

namespace Werewolf
{
    public class HediffWithComps_WerewolfExtraInfo : HediffWithComps
    {
        public CompWerewolf CompWerewolf => pawn.TryGetComp<CompWerewolf>();

        public override string TipStringExtra
        {
            get
            {
                var s = new StringBuilder();
                s.AppendLine(
                    "ROM_FormHealth_Tooltip".Translate(CompWerewolf.CurrentWerewolfForm.FormHealthScale * 100));
                s.AppendLine("ROM_FormSize_Tooltip".Translate(CompWerewolf.CurrentWerewolfForm.FormBodySize * 100));
                s.AppendLine("ROM_FormDmg_Tooltip".Translate(CompWerewolf.CurrentWerewolfForm.DmgImmunity * 100));
                s.AppendLine("---");
                var str = base.TipStringExtra;
                if (str != "")
                {
                    s.Append(base.TipStringExtra);
                }

                return s.ToString().TrimEndNewlines();
            }
        }
    }
}