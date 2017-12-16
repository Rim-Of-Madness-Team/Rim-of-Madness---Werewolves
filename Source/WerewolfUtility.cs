using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Werewolf
{
    public static class WerewolfUtility
    {
        public static CompWerewolf CompWW(this Pawn pawn)
        {
            if (pawn?.GetComp<CompWerewolf>() is CompWerewolf w) return w;
            return null;
        }

        public static bool IsWerewolf(this Pawn pawn)
        {
            if (pawn.CompWW() is CompWerewolf ww && ww.IsWerewolf) return true;
            return false;
        }

        // RimWorld.MedicalRecipesUtility
        public static bool IsClean(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !(from x in pawn.health.hediffSet.hediffs
                                   where x.Part == part
                                   select x).Any<Hediff>();
        }


        // RimWorld.WerewolfUtility
        public static bool IsCleanAndDroppable(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !pawn.RaceProps.Animal && part.def.spawnThingOnRemoved != null && WerewolfUtility.IsClean(pawn, part);
        }


        // RimWorld.WerewolfUtility
        public static Thing SpawnNaturalPartIfClean(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (WerewolfUtility.IsCleanAndDroppable(pawn, part))
            {
                return GenSpawn.Spawn(part.def.spawnThingOnRemoved, pos, map);
            }
            return null;
        }


        // RimWorld.WerewolfUtility
        public static void SpawnThingsFromHediffs(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (!pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined).Contains(part))
            {
                return;
            }
            IEnumerable<Hediff> enumerable = from x in pawn.health.hediffSet.hediffs
                                             where x.Part == part
                                             select x;
            foreach (Hediff current in enumerable)
            {
                if (current.def.spawnThingOnRemoved != null)
                {
                    GenSpawn.Spawn(current.def.spawnThingOnRemoved, pos, map);
                }
            }
            for (int i = 0; i < part.parts.Count; i++)
            {
                WerewolfUtility.SpawnThingsFromHediffs(pawn, part.parts[i], pos, map);
            }
        }

    }
}
