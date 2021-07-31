using System;
using System.Linq;
using Verse;

namespace Werewolf
{
    public static class WerewolfUtility
    {
        public static int transformedWerewolfCount = 0;

        public static CompWerewolf CompWW(this Pawn pawn)
        {
            return pawn?.GetComp<CompWerewolf>() is { } w ? w : null;
        }

        public static bool IsWerewolf(this Pawn pawn)
        {
            return pawn.CompWW() is {IsWerewolf: true};
        }

        // RimWorld.MedicalRecipesUtility
        public static bool IsClean(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !(from x in pawn.health.hediffSet.hediffs
                where x.Part == part
                select x).Any();
        }


        // RimWorld.WerewolfUtility
        public static bool IsCleanAndDroppable(Pawn pawn, BodyPartRecord part)
        {
            return !pawn.Dead && !pawn.RaceProps.Animal && part.def.spawnThingOnRemoved != null && IsClean(pawn, part);
        }


        // RimWorld.WerewolfUtility
        public static Thing SpawnNaturalPartIfClean(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            return IsCleanAndDroppable(pawn, part) ? GenSpawn.Spawn(part.def.spawnThingOnRemoved, pos, map) : null;
        }


        // RimWorld.WerewolfUtility
        public static void SpawnThingsFromHediffs(Pawn pawn, BodyPartRecord part, IntVec3 pos, Map map)
        {
            if (!pawn.health.hediffSet.GetNotMissingParts().Contains(part))
            {
                return;
            }

            var enumerable = from x in pawn.health.hediffSet.hediffs
                where x.Part == part
                select x;
            foreach (var current in enumerable)
            {
                if (current.def.spawnThingOnRemoved != null)
                {
                    GenSpawn.Spawn(current.def.spawnThingOnRemoved, pos, map);
                }
            }

            foreach (var bodyPartRecord in part.parts)
            {
                SpawnThingsFromHediffs(pawn, bodyPartRecord, pos, map);
            }
        }

        internal static void UpdateTransformedWerewolvesCount()
        {
            var maps = Find.Maps.ToList();
            int wwTransformedCount = 0;
            foreach (var _ in from Map m in maps
                              from Pawn pawn in m.mapPawns.AllPawnsSpawned
                              where pawn.IsWerewolf()
                              where pawn.GetComp<CompWerewolf>().IsTransformed
                              select new { })
            {
                wwTransformedCount += 1;
            }
            transformedWerewolfCount = wwTransformedCount;
            //Log.Message(transformedWerewolfCount.ToString() + " transformations active.");
        }
    }
}