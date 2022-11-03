using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace Werewolf
{
    public class ScenPart_StartingWerewolves : ScenPart
    {

        private WerewolfFormDef forcedFirstWerewolfFormDef = null;
        private bool hiddenWerewolf = false;
        private bool allowMetis = true;
        private int maxWerewolves = 1;
        private string maxWerewolvesBuf = "";
        private int curWerewolves = 0;
        private List<Pawn> startingPawns;

        public bool HiddenWerewolfMode()
        {
            return hiddenWerewolf;
        }

        public bool GetAllowMetisBool()
        {
            return allowMetis;
        }

        public int GetMaxWerewolves()
        {
            return maxWerewolves;
        }

        public List<Pawn> GetStartingPawns()
        {
            return startingPawns;
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            // Forced First Werewolf Form Option
            Rect scenPartRect = listing.GetScenPartRect(this, RowHeight * 4f + 31f);
            if (Widgets.ButtonText(scenPartRect.TopPartPixels(RowHeight), this?.forcedFirstWerewolfFormDef?.LabelCap ?? "Randomize".Translate()))
            {
                FloatMenuUtility.MakeMenu(PossibleWerewolfDefs(), (WerewolfFormDef wwd) => wwd.LabelCap, (WerewolfFormDef wwd) => delegate
                {
                    forcedFirstWerewolfFormDef = wwd;
                });
            }

            DoWerewolfModifierEditInterface(new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, 31f));
        }

        // RimWorld.ScenPart_PawnModifier
        protected void DoWerewolfModifierEditInterface(Rect rect)
        {

            int rowCount = 1;

            // Werewolf Hidden
            // Checkbox
            Rect rHidden = new Rect(rect.x, rect.y + RowHeight * rowCount, rect.width, 31);
            Rect rHiddenLeft = rHidden.LeftPart(0.666f).Rounded();
            Rect rHiddenRight = rHidden.RightPart(0.333f).Rounded();
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rHiddenLeft, "ROM_HideWerewolfTrait".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.CheckboxLabeled(rHiddenRight, "", ref hiddenWerewolf);
            rowCount++;

            // Allow Metis
            // Checkbox
            Rect rMetis = new Rect(rect.x, rect.y + RowHeight * rowCount, rect.width, 31);
            Rect rMetisLeft = rMetis.LeftPart(0.666f).Rounded();
            Rect rMetisRight = rMetis.RightPart(0.333f).Rounded();
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rMetisLeft, "ROM_AllowMetis".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.CheckboxLabeled(rMetisRight, "", ref allowMetis);
            rowCount++;

            // Maximum Werewolves
            // Number
            Rect rMax = new Rect(rect.x, rect.y + RowHeight * rowCount, rect.width, 31);
            Rect rMaxLeft = rMax.LeftPart(0.666f).Rounded();
            Rect rMaxRight = rMax.RightPart(0.333f).Rounded();
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rMaxLeft, "ROM_MaxWerewolves".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.TextFieldNumeric(rMaxRight, ref maxWerewolves, ref maxWerewolvesBuf, 1, 100);

            //Reference - Slider
            //Text.Anchor = TextAnchor.MiddleCenter;
            //Widgets.Label(rect2, "ROM_WerewolfHideWerewolfTrait".Translate(generationRange.min + "-" + generationRange.max));
            //Rect rect3 = new Rect(rect.x, rect.y + RowHeight * 2, rect.width, 31);
            //Widgets.IntRange(rect3, 21, ref generationRange, 1, 13, "", 0); //HorizontalSlider(rect3, vampChance, 0f, 1f, false, "", "", "");


        }


        private IEnumerable<WerewolfFormDef> PossibleWerewolfDefs()
        {
            return from x in DefDatabase<WerewolfFormDef>.AllDefsListForReading
                   where x != null
                   select x;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            //private BloodlineDef bloodline = VampDefOf.ROMV_ClanTremere;
            //private int numOfVamps = 1;
            //private string numOfVampsBuffer = "";
            //private IntRange generationRange = new IntRange(10, 13);
            //private int maxGeneration = 15;
            //private bool spawnInCoffins = false;
            Scribe_Defs.Look(ref forcedFirstWerewolfFormDef, "forcedFirstWerewolfFormDef");
            Scribe_Values.Look(ref hiddenWerewolf, "hiddenWerewolf");
            Scribe_Values.Look(ref allowMetis, "allowMetis");
            Scribe_Values.Look(ref maxWerewolves, "maxWerewolves", 1);
            Scribe_Values.Look(ref curWerewolves, "curWerewolves");
            Scribe_Collections.Look(ref startingPawns, "startingPawns", LookMode.Reference);
        }

        public override string Summary(Scenario scen)
        {
#pragma warning disable CS0618
            return "ROM_StartingWerewolvesSummary".Translate(new object[]
#pragma warning restore CS0618
            {
                maxWerewolves.ToString(),
                GetHiddenTrait() ?? "",
                GetForcedFirstForm(),
                GetAllowMetis(),
            }).CapitalizeFirst();
        }

        public string GetHiddenTrait()
        {
            return this?.hiddenWerewolf == true ? "ROM_WHidden".Translate() : "ROM_WVisible".Translate();
        }

        public string GetForcedFirstForm()
        {
            return this?.forcedFirstWerewolfFormDef != null ? forcedFirstWerewolfFormDef.LabelCap : "ROM_WRandom".Translate();
        }


        public string GetAllowMetis()
        {
            return this?.allowMetis == true ?  "ROM_WEnabled".Translate() : "ROM_WDisabled".Translate();
        }


        public override void Randomize()
        {
            base.Randomize();
            forcedFirstWerewolfFormDef = PossibleWerewolfDefs().RandomElement();
            hiddenWerewolf = Rand.Value > 0.3 ? true : false;
            allowMetis = Rand.Value > 0.3 ? true : false;
            maxWerewolves = Rand.Range(1, 3);
        }

        public override void PostMapGenerate(Map map)
        {
            if (Find.GameInitData == null)
            {
                return;
            }
            foreach (Pawn p in Find.GameInitData.startingAndOptionalPawns)
            {
                if (startingPawns == null)
                    startingPawns = new List<Pawn>();
                startingPawns.Add(p);
                //Log.Message(p.Label);
            }
            WerewolfUtility.Shuffle(startingPawns);
        }

        public override void Notify_PawnGenerated(Pawn pawn, PawnGenerationContext context, bool redressed)
        {
            if (hiddenWerewolf) return;

            if (Find.CurrentMap == null)
            {
                curWerewolves = Find.GameInitData.startingAndOptionalPawns.FindAll(x => x?.IsWerewolf() != null)?.Count ?? 0;

                if (pawn.RaceProps.Humanlike && context == PawnGenerationContext.PlayerStarter)
                {
                    if (!pawn?.story?.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent) ?? false)
                    {
                        if (curWerewolves < maxWerewolves)
                        {
                            curWerewolves++;
                            WerewolfUtility.AddWerewolfTrait(pawn, allowMetis);
                        }
                    }
                }
            }
            
        }
    }
}
