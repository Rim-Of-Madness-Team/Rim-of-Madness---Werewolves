using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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

        public static void AddWerewolfTrait(this Pawn pawn, bool metisChance = true, bool showMessage = false)
        {
            if (pawn == null)
                return;

            if (!pawn.IsWerewolf())
            {
                Trait werewolfTrait = null;

                if (metisChance)
                {
                    werewolfTrait = new Trait(WWDefOf.ROM_Werewolf, -1);
                    pawn.story.traits.GainTrait(werewolfTrait);
                }
                else
                {
                    werewolfTrait = new Trait(WWDefOf.ROM_Werewolf);
                    pawn.story.traits.GainTrait(werewolfTrait);
                }
                
                //If Biotech is activated, add a werewolf gene
                if (ModsConfig.BiotechActive)
                {
                    Gene werewolfGene = GeneMaker.MakeGene(WWDefOf.ROMW_WerewolfGene, pawn);
                    pawn.genes.Endogenes.Add(werewolfGene);
                    werewolfTrait.sourceGene = werewolfGene;
                }


                if (showMessage)
                {
                    pawn.Drawer.Notify_DebugAffected();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is now a werewolf");
                }
            }
            else
            {
                if (showMessage)
                    Messages.Message(pawn.LabelCap + " is already a werewolf.", MessageTypeDefOf.RejectInput);
            }

        }

        public static void RemoveWerewolfTrait(this Pawn pawn, bool showMessage = false)
        {
            if (pawn == null)
                return;

            if (pawn.IsWerewolf())
            {
                if (pawn.CompWW().IsTransformed)
                {
                    pawn.CompWW().TransformBack();
                }
                
                //Biotech werewolf gene
                if (ModsConfig.BiotechActive && pawn.CompWW().WerewolfGene is { } gene)
                    pawn.genes.RemoveGene(gene);

                pawn.story.traits.allTraits.RemoveAll(x =>
                    x.def == WWDefOf.ROM_Werewolf); //GainTrait(new Trait(WWDefOf.ROM_Werewolf, -1));
                                                    //pawn.health.AddHediff(VampDefOf.ROM_Vampirism, null, null);
                if (showMessage)
                {
                    pawn.Drawer.Notify_DebugAffected();
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, pawn.LabelShort + " is no longer a werewolf");
                }
            }
            else
            {
                if (showMessage)
                    Messages.Message(pawn.LabelCap + " is not a werewolf.", MessageTypeDefOf.RejectInput);
            }

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

        public static IEnumerable<Hediff_Injury> GetAllInjuries(Pawn pawn)
        {
            int num;
            if (pawn?.health?.hediffSet != null)
            {
                for (var i = 0; i < pawn.health.hediffSet.hediffs.Count; i = num)
                {
                    if (pawn.health.hediffSet.hediffs[i] is Hediff_Injury { } hediffInjury)
                    {
                        yield return hediffInjury;
                    }

                    num = i + 1;
                }
            }
        }
        
        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

    }
}