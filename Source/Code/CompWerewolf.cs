using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vampire;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace Werewolf
{
    /// CompWerewolf
    /// Werewolves in RimWorld
    /// ------------
    /// 
    /// Summary: 
    /// Adds systems for handling Werewolves in-game.
    ///   
    /// Detailed Summary:
    /// This is [a thing-attachable object that holds data and has functions that can trigger actively 
    /// on a per tick basis] (known hereafter as --Component--) while the character (--Pawn--) is in
    /// the game's playing field (--Spawned--).
    ///   
    /// This Component handles cases where Spawned Pawns have the Werewolf Trait in their Character Sheet.
    ///   
    /// There are two CompWerewolf states:
    /// Unblooded  :   First time WWP (Werewolf Pawn).
    /// Blooded    :   WWP that has been triggered by a full moon event at least once before.
    /// 
    /// When =WorldComponent_MoonCycle= ticks through its list of =Moon= class objects and a
    /// =GameCondition_FullMoon= (--full moon--) is declared, Unblooded WPPs will have their mind state
    /// forced into MentalStateDef WerewolfFury. They will be given =JobGiver_WerewolfHunt=, which is
    /// a customized version of the original RimWorld's =JobGiver_Manhunter=, but focused on breaking doors
    /// and killing targets. Then they will transform randomly into one of the four Werewolf Forms:
    ///   
    /// Werewolf Forms:
    /// Glabro  :   The Near Man Form
    /// Crinos  :   The Battle Form
    /// Hispo   :   The Near Wolf Form
    /// Lupus   :   The Wolf Form
    ///     
    /// =HediffComp_Rage= handles the duration of the transformation, and this class' methods handle most
    /// cases for transformation issues (e.g. upper body items and weapons are stored away in temporary lists).
    ///   
    /// Once WWPs become Blooded, they may transform at will. Each transformation from a full moon fury causes
    /// the Werewolf Form to level up. =WerewolfFormDef= holds the variables for the rates of level progression.
    /// ==WerewolfForm== and ==HarmonyPatches== handle cases for applying that progression (e.g. WerewolfBodySize).
    public class CompWerewolf : CompAbilityUser
    {
        #region Variables and Properties

        public enum State
        {
            Unknown = -1,
            Unblooded = 0,
            Werewolf = 1,
            PackMember = 2,
            Metis = 3
        }

        #region Variables

        private bool furyToggled;
        private bool factionResolved;
        private bool needsGraphicRefresh = true;
        private bool isReverting;
        private bool? isBlooded;
        private int cooldownTicksLeft = -1;
        private readonly float metisChance = 0.4f;
        private WerewolfForm currentWerewolfForm;
        private List<WerewolfForm> werewolfForms;
        private List<ThingWithComps> storedItems = new List<ThingWithComps>();
        private List<Apparel> upperBodyItems = new List<Apparel>();

        #endregion Variables

        #region Properties

        //Simple Properties
        public bool FuryToggled => furyToggled;

        public bool IsReverting
        {
            get => isReverting;
            set => isReverting = value;
        }

        public int CooldownMaxTicks => GenDate.TicksPerHour * 6;

        public int CooldownTicksLeft
        {
            get => cooldownTicksLeft;
            set => cooldownTicksLeft = value;
        }

        public WerewolfForm CurrentWerewolfForm
        {
            get => currentWerewolfForm;
            set => currentWerewolfForm = value;
        }

        public List<ThingWithComps> StoredWeapons
        {
            get => storedItems;
            set => storedItems = value;
        }

        public List<Apparel> UpperBodyItems
        {
            get => upperBodyItems;
            set => upperBodyItems = value;
        }

        public List<WerewolfForm> WerewolfForms
        {
            set => werewolfForms = value;
            get
            {
                if (werewolfForms.NullOrEmpty() && IsWerewolf)
                {
                    werewolfForms = new List<WerewolfForm>
                    {
                        new WerewolfForm(WWDefOf.ROM_Glabro, 0, Pawn),
                        new WerewolfForm(WWDefOf.ROM_Crinos, 0, Pawn),
                        new WerewolfForm(WWDefOf.ROM_Hispo, 0, Pawn),
                        new WerewolfForm(WWDefOf.ROM_Lupus, 0, Pawn),
                        new WerewolfForm(WWDefOf.ROM_Metis, 0, Pawn)
                    };
                }

                return werewolfForms;
            }
        }

        public bool IsBlooded
        {
            get
            {
                if (isBlooded != null)
                {
                    return isBlooded.Value;
                }

                isBlooded = false;
                if (Pawn?.story?.traits?.GetTrait(WWDefOf.ROM_Werewolf)?.Degree > 1)
                {
                    isBlooded = true;
                }

                return isBlooded.Value;
            }
            set => isBlooded = value;
        }

        //Utilitarian Properties
        public bool IsSimpleSidearmsLoaded =>
            ModLister.AllInstalledMods.FirstOrDefault(x => x.Name == "Simple sidearms" && x.Active) != null;

        public bool IsTransformed => CurrentWerewolfForm != null;

        public bool IsWerewolf => WerewolfTrait != null;

        public bool CanTransformNow => IsWerewolf && !IsTransformed && CooldownTicksLeft <= 0;
        public Vector3 Vec3 => Pawn.PositionHeld.ToVector3();
        public Map Map => Pawn.MapHeld;
        public Trait WerewolfTrait => Pawn?.story?.traits?.GetTrait(WWDefOf.ROM_Werewolf);
        public Gene WerewolfGene => ModsConfig.BiotechActive ? Pawn?.genes?.GetGene(WWDefOf.ROMW_WerewolfGene) : null;
        public WerewolfForm HighestLevelForm => WerewolfForms.MaxBy(x => x.level);

        #endregion Properties

        #endregion Variables and Properties

        #region Methods

        #region Transform Methods

        public override void PostDraw()
        {
            if (IsTransformed)
            {
                var mat = CurrentWerewolfForm.def.graphicData.Graphic.MeshAt(Pawn.Rotation);
                Vector3 loc = this.parent.TrueCenter();
                loc.y = AltitudeLayer.PawnUnused.AltitudeFor() + Altitudes.AltInc * 0.5f;
                var graphic = CurrentWerewolfForm.def.graphicData.Graphic;
                graphic.Draw(loc, Pawn.Rotation, this.parent, 0f);
            }
            base.PostDraw();
        }

        /// Sets the current werewolf form and calls other sub-methods.
        public void TransformInto(WerewolfForm form, bool moonTransformation = false)
        {
            CurrentWerewolfForm = form;
            WerewolfUtility.UpdateTransformedWerewolvesCount();
            if (Pawn is { } p)
            {
                if (p.Faction == Faction.OfPlayer)
                {
                    p.ClearMind();
                }

                if (form != null)
                {
                    //Log.Message("ResolveTransformationEffects");
                    //Log.Message($"{Pawn.NameShortColored} ResolveTransformEffects");
                    ResolveTransformEffects(p, currentWerewolfForm, moonTransformation);
                    //Log.Message($"{Pawn.NameShortColored} ResolveEquipmentStorage");
                    ResolveEquipmentStorage();
                    return;
                }

                //Log.Message("Restore");
                //Log.Message($"{Pawn.NameShortColored} RestoreEquipment");
                
                RestoreEquipment();
                //Log.Message($"{Pawn.NameShortColored} ResolveAllGraphics");
                p.Drawer.renderer.graphics.ResolveAllGraphics();
                Messages.Message("ROM_WerewolfRevert".Translate(Pawn),
                    MessageTypeDefOf.SilentInput); //MessageTypeDefOf.SilentInput);

            }

            isReverting = false;
        }

        public override float CombatPoints()
        {
            //Log.Message("Combat points called");
            if (forbiddenWolfhood)
            {
                return 0;
            }

            if (WerewolfForms.NullOrEmpty())
            {
                return 400;
            }

            var combatPoints = WerewolfForms.Max(x => x.level) * 400;
            //Log.Message("combatPoints: " + combatPoints);
            return combatPoints;
        }

        private bool forbiddenWolfhood;

        public override void DisableAbilityUser()
        {
            if (WerewolfGene is { } gene)
                Pawn.genes.RemoveGene(gene);
            Pawn.story.traits.allTraits.Remove(WerewolfTrait);
            forbiddenWolfhood = true;
        }

        /// Gives a random new transformation from full moon furies.
        public void TransformRandom(bool moonTransformation)
        {
            var formToTake = ResolveRandomWerewolfForm();
            TransformInto(formToTake, moonTransformation);
        }

        /// Restores the original form of the Pawn.
        public void TransformBack(bool killed = false)
        {
            //If killed, make one last howl.
            if (isReverting)
            {
                //Log.Message($"{Pawn.NameShortColored} is already transforming, ignoring function-call");
                return;
            }

            //Log.Message($"{Pawn.NameShortColored} Transforming back, killed: {killed}");
            isReverting = true;
            if (killed && currentWerewolfForm.def.transformSound is { } howl)
            {
                howl.PlayOneShot(new TargetInfo(Pawn.PositionHeld, Pawn.MapHeld));
            }

            //Clear the graphic cache.
            if (WerewolfForms.FirstOrDefault(x => x.bodyGraphicData != null) is { } f)
            {
                f.bodyGraphicData = null;
            }

            //Heal some of the injuries to avoid instant death when reverting back to humanoid form.
            if (!Pawn.Dead && Pawn?.health?.hediffSet is { } health)
            {
                //Log.Message($"{Pawn.NameShortColored} GetInjuredParts");
                for (var i = 0; i < (int) health?.GetInjuredParts().Count(); i++)
                {
                    var rec = health.GetInjuredParts().ElementAt(i);
                    var injuriesToHeal =
                        WerewolfUtility.GetAllInjuries(Pawn).ToList().FindAll(x => x.Part == rec);
                    if (injuriesToHeal.NullOrEmpty())
                    {
                        continue;
                    }

                    //Log.Message($"{Pawn.NameShortColored} injuriesToHeal");
                    foreach (var current in injuriesToHeal)
                    {
                        if (current.CanHealNaturally() && !current.IsPermanent()
                        ) // basically check for scars and old wounds
                        {
                            current.Severity -= current.Severity * 0.80f;
                        }
                    }
                }

                //Log.Message($"{Pawn.NameShortColored} BloodLoss");
                if (health.hediffs?.FirstOrDefault(x => x.def == HediffDefOf.BloodLoss) is { } bloodLoss)
                {
                    bloodLoss.Severity = 0;
                }
            }

            //Fix equipment before they are dropped by the game
            //Log.Message($"{Pawn.NameShortColored} RestoreEquipment");
            RestoreEquipment();

            //Remove the health differentials.
            //Log.Message($"{Pawn.NameShortColored} ResolveClearHediffs");
            ResolveClearHediffs();

            //Trigger a null transformation
            //Log.Message($"{Pawn.NameShortColored} TransformInto");
            TransformInto(null);
        }

        #region Transform Effects

        /// Returns a Werewolf form depending on the state of the character.
        private WerewolfForm ResolveRandomWerewolfForm()
        {
            WerewolfForm formToTake = null;
            var metisForm = WerewolfForms?.FirstOrDefault(x => x.def == WWDefOf.ROM_Metis);
            switch ((State) WerewolfTrait.Degree)
            {
                //Unknown werewolves have a chance of becoming Metis Werewolves.
                case State.Unknown:

                    //Check Metis chance and replace the werewolf trait.
                    Pawn.story.traits.allTraits.Remove(WerewolfTrait);
                    Trait newWerewolfTrait;
                    var rand = Rand.Value;
                    if (rand > metisChance)
                    {
                        newWerewolfTrait = new Trait(WWDefOf.ROM_Werewolf, (int) State.Werewolf);
                        formToTake = WerewolfForms.RandomElement();
                        
                        if (ModsConfig.BiotechActive)
                        {
                            Gene wolfGene = Pawn.genes.GetGene(WWDefOf.ROMW_WerewolfGene) ?? GeneMaker.MakeGene(WWDefOf.ROMW_WerewolfGene, Pawn);
                            newWerewolfTrait.sourceGene = wolfGene;
                        }
                    }
                    else
                    {
                        newWerewolfTrait = new Trait(WWDefOf.ROM_Werewolf, (int) State.Metis);
                        formToTake = WerewolfForms!.FirstOrDefault(x => x.def == WWDefOf.ROM_Metis);

                        if (ModsConfig.BiotechActive)
                        {
                            Gene wolfGene = Pawn.genes.GetGene(WWDefOf.ROMW_WerewolfGene);
                            if (wolfGene != null)
                            {
                                Pawn.genes.RemoveGene(wolfGene);
                            }
                            Gene metisGene = Pawn.genes.GetGene(WWDefOf.ROM_WerewolfMetisSterile) ?? GeneMaker.MakeGene(WWDefOf.ROM_WerewolfMetisSterile, Pawn);
                            Pawn.genes.Endogenes.Add(metisGene);
                            newWerewolfTrait.sourceGene = metisGene;
                        }
                    }

                    Pawn.story.traits.GainTrait(newWerewolfTrait);

                    //Just in-case, be sure to flag the Werewolf as unblooded.
                    IsBlooded = false;
                    break;

                //Metis werewolves can ONLY become Metis werewolves.
                case State.Metis:
                    formToTake = WerewolfForms!.FirstOrDefault(x => x.def == WWDefOf.ROM_Metis);
                    break;

                //Unblooded Werewolves do not become Metis.
                case State.Unblooded:
                    if (metisForm != null)
                    {
                        WerewolfForms.Remove(metisForm);
                    }

                    formToTake = WerewolfForms.RandomElement();

                    //Replace trait.
                    Pawn.story.traits.allTraits.Remove(WerewolfTrait);
                    Pawn.story.traits.GainTrait(new Trait(WWDefOf.ROM_Werewolf, (int) State.Werewolf));
                    formToTake = WerewolfForms.RandomElement();

                    isBlooded = false; //Just in-case
                    break;

                //Regular Werewolves and Pack Members do not become Metis.
                case State.Werewolf:
                case State.PackMember:
                    if (metisForm != null)
                    {
                        WerewolfForms.Remove(metisForm);
                    }

                    formToTake = WerewolfForms.RandomElement();
                    break;
            }

            return formToTake;
        }

        /// Burst out of any containers (e.g. Cryptosleep pods)
        private void ResolveContainment()
        {
            var spawnLoc = Pawn.PositionHeld;
            if (spawnLoc == IntVec3.Invalid)
            {
                spawnLoc = Pawn.Position;
            }

            if (spawnLoc == IntVec3.Invalid)
            {
                _ = DropCellFinder.RandomDropSpot(Map);
            }

            //Hops / Other storage buildings
            if (Pawn.StoringThing() is Building building)
            {
                if (building is Building_Storage buildingS)
                {
                    buildingS.Notify_LostThing(Pawn);
                }
            }

            if (Pawn.holdingOwner?.Owner is not Building_Casket casket)
            {
                return;
            }

            casket.EjectContents();
            Messages.Message("ROM_WerewolfEscaped".Translate(Pawn.LabelShort, casket.Label), new GlobalTargetInfo(Pawn),
                MessageTypeDefOf.SilentInput);
        }

        /// Drop all bionic parts when transforming.
        private void ResolveBionics()
        {
            if (Pawn.Dead)
            {
                return;
            }

            foreach (var rec in Pawn.health.hediffSet.GetNotMissingParts())
            {
                var addedPartsList = new List<Hediff_AddedPart>();
                Pawn.health.hediffSet.GetHediffs<Hediff_AddedPart>(ref addedPartsList);
                var hediff_AddedPart = (from x in addedPartsList
                    where x.Part == rec && !x.Part.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource)
                    select x).FirstOrDefault();
                if (hediff_AddedPart == null)
                {
                    continue;
                }

                if (WerewolfForms.Any(x => x.level >= 5) && !PartCanBeWerewolfPart(hediff_AddedPart.Part) &&
                    !(hediff_AddedPart.def?.addedPartProps?.partEfficiency < 1f))
                {
                    continue;
                }

                var showMessage = true;
                // VAMPIRISM: Hide Lose Fangs Message ////////////////////////
                try
                {
                    if (LoadedModManager.RunningMods.FirstOrDefault(x =>
                        x.Name.Contains("Rim of Madness - Vampires")) != null)
                    {
                        AreFangs(hediff_AddedPart);
                        showMessage = false;
                    }
                }
                catch (TypeLoadException)
                {
                    //Log.Message(e.toString());
                }

                // ////////////////////////////////////////////////////////////
                WerewolfUtility.SpawnNaturalPartIfClean(Pawn, rec, Pawn.Position, Pawn.Map);
                WerewolfUtility.SpawnThingsFromHediffs(Pawn, rec, Pawn.Position, Pawn.Map);
                Pawn.health.hediffSet.hediffs.Remove(hediff_AddedPart);
                Pawn.health.RestorePart(rec);
                if (Pawn.Faction != Faction.OfPlayer || !showMessage)
                {
                    continue;
                }

                Messages.Message(
                    "ROM_WerewolfDecyberize".Translate(Pawn.LabelShort, rec.def.label,
                        hediff_AddedPart.Label),
                    MessageTypeDefOf.NegativeEvent); //MessageTypeDefOf.NegativeEvent);
                LessonAutoActivator.TeachOpportunity(WWDefOf.ROMWW_ConceptBionics, Pawn,
                    OpportunityType.Critical);
            }
        }

        private bool AreFangs(Hediff_AddedPart addedPart)
        {
            return addedPart is Hediff_AddedPart_Fangs;
        }


        /// Restores all missing parts when transforming
        private void ResolveMissingParts(Pawn p)
        {
            var missingParts = new List<Hediff_MissingPart>()
                .Concat(p?.health?.hediffSet?.GetMissingPartsCommonAncestors()).ToList();
            if (missingParts.NullOrEmpty())
            {
                return;
            }

            foreach (var part in missingParts)
            {
                p?.health?.RestorePart(part.Part);
                Messages.Message("ROM_WerewolfLimbRegen".Translate(p?.LabelShort, part.Label),
                    MessageTypeDefOf.PositiveEvent); //MessageSound.Benefit);
            }
        }

        /// Adds the primary Werewolf stats.
        private void ResolveWerewolfStatMods(Pawn p, WerewolfForm werewolfForm)
        {
            //Gives Werewolf full-body stats.
            var formHediff = HediffMaker.MakeHediff(werewolfForm.def.formHediff, p);
            formHediff.Severity = 1.0f;
            p.health.AddHediff(formHediff);
        }

        public bool PartCanBeWerewolfPart(BodyPartRecord part)
        {
            return PartCanBeWerewolfJaw(part) || PartCanBeWerewolfClaw(part);
        }

        public bool PartCanBeWerewolfJaw(BodyPartRecord part)
        {
            return part.def == BodyPartDefOf.Jaw || part.def.tags.Contains(BodyPartTagDefOf.EatingSource);
        }

        public bool PartCanBeWerewolfClaw(BodyPartRecord part)
        {
            return part.def == BodyPartDefOf.Hand;
        }


        /// Adds the Werewolf jaws and claws to each respective body part.
        private void ResolveTransformedHediffs(Pawn p, WerewolfForm werewolfForm)
        {
            var recs = p.health.hediffSet.GetNotMissingParts();
            var bodyPartRecords = new Dictionary<BodyPartRecord, HediffDef>();
            var partRecords = recs as BodyPartRecord[] ?? recs.ToArray();
            if (partRecords.FirstOrDefault(PartCanBeWerewolfJaw) is { } jaw)
            {
                bodyPartRecords.Add(jaw, werewolfForm.def.jawHediff);
            }

            foreach (var partRecord in partRecords.Where(PartCanBeWerewolfClaw))
            {
                bodyPartRecords.Add(partRecord, werewolfForm.def.clawHediff);
            }

            if ((bodyPartRecords?.Count ?? 0) > 0)
            {
                foreach (var transformableParts in bodyPartRecords)
                {
                    var transformedHediff =
                        HediffMaker.MakeHediff(transformableParts.Value, p, transformableParts.Key);
                    transformedHediff.Severity = 1.0f;
                    p.health.AddHediff(transformedHediff, transformableParts.Key);
                }
            }

            HealthUtility.AdjustSeverity(p, HediffDefOf.BloodLoss, -9999);
        }

        /// Adds a fury to the Werewolf.
        private void ResolveFury(Pawn p, WerewolfForm werewolfForm, bool moonTransformation = false)
        {
            //Give Werewolf fury during a full moon event IF
            //1) Werewolf is UNBLOODED
            //2) Fury mode is ENABLED
            var giveFuryMentalState = false;
            if (moonTransformation)
            {
                if (!IsBlooded)
                {
                    IsBlooded = true;
                    giveFuryMentalState = true;
                }

                if (FuryToggled)
                {
                    giveFuryMentalState = true;
                }

                werewolfForm.LevelUp();
            }

            if (giveFuryMentalState)
            {
                p.mindState.mentalStateHandler.TryStartMentalState(WWDefOf.ROM_WerewolfFury,
                    "ROM_MoonCycle_FullMoonArgless".Translate(), true);
            }
        }

        /// Apply Werewolf stats and gives visual/audio feedback to players when transforming.
        private void ResolveTransformEffects(Pawn p, WerewolfForm werewolfForm, bool moonTransformation = false)
        {
            if (moonTransformation)
            {
                ResolveContainment();
            }

            ResolveBionics();
            ResolveMissingParts(p);
            ResolveWerewolfStatMods(p, werewolfForm);
            ResolveTransformedHediffs(p, werewolfForm);
            ResolveFury(p, werewolfForm, moonTransformation);

            //--------------------------------
            //     FEEDBACK (GFX/SFX/MSG)
            //--------------------------------

            //Pre-transformation Effects
            FleckMaker.ThrowAirPuffUp(Vec3, Map);
            FleckMaker.ThrowSmoke(Vec3, Map, 2.0f);

            //ResolveAllGraphics is patched in HarmonyPatches.
            //This changes the graphic of a character into a Werewolf.
            p.Drawer.renderer.graphics.ResolveAllGraphics();

            if (werewolfForm.def.transformSound is { } howl)
            {
                howl.PlayOneShot(new TargetInfo(p.PositionHeld, p.MapHeld));
            }

            Messages.Message("ROM_WerewolfTransformation".Translate(p.LabelShort, werewolfForm.def.label),
                MessageTypeDefOf.SilentInput);
        }

        /// Clears up remaining hediffs
        private void ResolveClearHediffs()
        {
            var hediffTemps = new List<Hediff>();
            var hediffs = Pawn?.health?.hediffSet?.hediffs;
            if (hediffs == null || hediffs.Count <= 0)
            {
                //Log.Message($"{Pawn.NameShortColored} no hediffs");
                return;
            }

            hediffTemps.AddRange(hediffs);
            foreach (var hediff in hediffTemps)
            {
                //Log.Message($"{Pawn.NameShortColored} parsing {hediff}");
                if (hediff?.def == CurrentWerewolfForm?.def?.clawHediff ||
                    hediff?.def == CurrentWerewolfForm?.def?.formHediff)
                {
                    Pawn?.health?.RemoveHediff(hediff);
                }

                if (hediff?.def != CurrentWerewolfForm?.def?.jawHediff)
                {
                    continue;
                }

                Pawn?.health?.RemoveHediff(hediff);
                try
                {
                    if (LoadedModManager.RunningMods?.FirstOrDefault(x =>
                        x.Name.Contains("Rim of Madness - Vampires")) != null)
                    {
                        ReAddVampireFangs();
                    }
                }
                catch (TypeLoadException)
                {
                    //Log.Message(e.toString());
                }
            }
        }

        private void ReAddVampireFangs()
        {
            if (Pawn.IsVampire(false))
            {
                VampireGen.AddFangsHediff(Pawn);
            }
        }

        #region Equipment Handlers

        /// Moves the equipped weapons into the inventory section
        private void ResolveWeaponStorage(Pawn_InventoryTracker inventory)
        {
            //Put away all weapons.
            if (Pawn?.equipment?.AllEquipmentListForReading is not { } equipment ||
                equipment.NullOrEmpty())
            {
                return;
            }

            var temp = new List<ThingWithComps>();
            temp.AddRange(equipment);
            foreach (var c in temp)
            {
                if (!Pawn.equipment.Contains(c))
                    continue;
                
                if (!Pawn.equipment.TryDropEquipment(c, out var d, Pawn.PositionHeld, false) || d == null)
                {
                    continue;
                }

                StoredWeapons.Add(d);
                inventory.innerContainer.InnerListForReading.Add(d);
                if (d.Spawned)
                {
                    d.DeSpawn();
                }
            }
        }

        /// Moves the equipped apparel into the inventory section
        private void ResolveApparelStorage()
        {
            //Take off upper body items.
            if (Pawn?.apparel?.WornApparel is not { } apparel || apparel.NullOrEmpty())
            {
                return;
            }

            var temp = new List<Apparel>(apparel);
            foreach (var c in temp)
            {
                if (c?.def?.apparel?.bodyPartGroups is not { } groups || groups.NullOrEmpty() ||
                    !groups.Contains(BodyPartGroupDefOf.Torso) && !groups.Contains(BodyPartGroupDefOf.FullHead) &&
                    !groups.Contains(BodyPartGroupDefOf.LeftHand) && !groups.Contains(BodyPartGroupDefOf.RightHand))
                {
                    continue;
                }

                if (!Pawn.apparel.Contains(c)) continue;
                if (!Pawn.apparel.TryDrop(c, out var d, Pawn.PositionHeld, false) || d == null)
                {
                    continue;
                }

                upperBodyItems.Add(d);
                Pawn.inventory.innerContainer.InnerListForReading.Add(d);
                if (d.Spawned)
                {
                    d.DeSpawn();
                }
            }
        }

        /// Unequip and store equipment/apparel in a list while transformed into a werewolf.
        private void ResolveEquipmentStorage()
        {
            //Clear previous lists.
            StoredWeapons = new List<ThingWithComps>();
            UpperBodyItems = new List<Apparel>();


            if (Pawn?.inventory is not { } inventory)
            {
                return;
            }

            if (!IsSimpleSidearmsLoaded)
            {
                ResolveWeaponStorage(inventory);
                ResolveApparelStorage();
            }
            else if (CurrentWerewolfForm != null)
            {
                if (Pawn.apparel.WornApparel is { } apps && !apps.NullOrEmpty())
                {
                    var temp = new List<Apparel>(apps);
                    foreach (var app in temp)
                    {
                        if (Pawn.apparel.Contains(app))
                            Pawn.apparel.TryDrop(app, out _, Pawn.PositionHeld);
                    }
                }

                if (Pawn.equipment.AllEquipmentListForReading is not { } weps || weps.NullOrEmpty())
                {
                    return;
                }

                {
                    var temp = new List<ThingWithComps>(weps);
                    foreach (var wep in temp)
                    {
                        if (Pawn.equipment.Contains(wep))
                            Pawn.equipment.TryDropEquipment(wep, out _, Pawn.PositionHeld);
                    }
                }
            }
        }
        
        
        /// Equip previously stored equipment after reverting back into the original form.
        private void RestoreEquipment()
        {
            var p = Pawn;
            
            //If dead or downed, don't do this
            if (p.Dead || p.Downed) return;

            if (p?.inventory is not { } invTracker) return;
            if (p?.equipment is not { } equipTracker) return;
            if (p?.apparel is not { } apparelTracker) return;

            //Equip all pre-transformation weapons.
            if (!StoredWeapons.NullOrEmpty())
            {
                foreach (var c in StoredWeapons)
                {
                    if (c == null || !Pawn.inventory.innerContainer.Remove(c))
                    {
                        continue;
                    }
                    equipTracker.AddEquipment(c);
                }
                StoredWeapons.Clear();
            }
            
            //Wear all pre-transformation apparel
            if (!UpperBodyItems.NullOrEmpty())
            {
                if (!UpperBodyItems.NullOrEmpty())
                {
                    foreach (var a in UpperBodyItems)
                    {
                        invTracker.innerContainer.InnerListForReading.Remove(a);
                        apparelTracker.Wear(a, false);
                    }
                    UpperBodyItems.Clear();
                }  
            }
        }

        #endregion Equipment Handlers

        #endregion Transform Effects

        #endregion Transform Methods

        #region AI Methods

        /// Transform AI Werewolves when they are in danger.
        public void ResolveAIHostileReaction()
        {
            if (Find.TickManager.TicksGame % 250 == 0 && Pawn.Faction != Faction.OfPlayer &&
                Pawn.mindState.enemyTarget != null && CanTransformNow)
            {
                TransformInto(HighestLevelForm);
            }
        }

        /// Give AI Werewolves levels in different forms.
        public void ResolveAIFactionSpawns()
        {
            if (factionResolved || Pawn?.Faction?.def?.defName != "ROM_WerewolfClan" ||
                Pawn?.kindDef?.defName == "ROM_WerewolfStraggler" || forbiddenWolfhood)
            {
                return;
            }

            factionResolved = true;


            if (!Pawn.story.traits.HasTrait(WWDefOf.ROM_Werewolf))
            {
                var newTrait = new Trait(WWDefOf.ROM_Werewolf, 2, true);
                if (Pawn.kindDef?.defName == "ROM_WerewolfNewBlood")
                {
                    newTrait = new Trait(WWDefOf.ROM_Werewolf, -1, true);
                }

                var toRemove = Pawn.story.traits.allTraits.RandomElement();
                Pawn.story.traits.allTraits.Remove(toRemove);
                Pawn.story.traits.GainTrait(newTrait);
            }

            //Give random werewolf abilities
            switch (Pawn.kindDef?.defName)
            {
                case "ROM_WolfHandler":

                    SpawnWolves(Rand.Range(1, 3));
                    LevelUp(WWDefOf.ROM_Hispo, Rand.Range(1, 3));
                    LevelUp(WWDefOf.ROM_Lupus, Rand.Range(1, 3));
                    break;

                case "ROM_Werewolf":
                    LevelUp(WWDefOf.ROM_Glabro, Rand.Range(3, 4));
                    LevelUp(WWDefOf.ROM_Crinos, Rand.Range(3, 4));
                    LevelUp(WWDefOf.ROM_Hispo, Rand.Range(3, 4));
                    LevelUp(WWDefOf.ROM_Lupus, Rand.Range(3, 4));
                    break;

                case "ROM_WerewolfFang":
                    LevelUp(WWDefOf.ROM_Glabro, Rand.Range(1, 6));
                    LevelUp(WWDefOf.ROM_Crinos, Rand.Range(4, 6));
                    LevelUp(WWDefOf.ROM_Hispo, Rand.Range(4, 6));
                    LevelUp(WWDefOf.ROM_Lupus, Rand.Range(4, 6));
                    break;

                case "ROM_WerewolfAlpha":
                    SpawnWolves(Rand.Range(3, 4));
                    LevelUp(WWDefOf.ROM_Glabro, Rand.Range(1, 2));
                    LevelUp(WWDefOf.ROM_Crinos, Rand.Range(6, 8));
                    LevelUp(WWDefOf.ROM_Hispo, Rand.Range(6, 8));
                    LevelUp(WWDefOf.ROM_Lupus, Rand.Range(6, 8));
                    break;
            }

            //Manage the AI sensibly.
            if (Pawn?.mindState?.duty?.def != DutyDefOf.AssaultColony || !IsBlooded)
            {
                return;
            }

            if (Pawn is {mindState: { }})
            {
                Pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("ROM_WerewolfAssault"));
            }
        }

        #endregion AI Methods

        /// Sets the level of a specific Werewolf type.
        public void LevelUp(WerewolfFormDef def, int level)
        {
            var werewolfForm = WerewolfForms.FirstOrDefault(x => x.def == def);
            if (werewolfForm != null)
            {
                werewolfForm.level = level;
            }
        }

        /// Spawns companion wolves.
        public void SpawnWolves(int numToSpawn)
        {
            for (var i = 0; i < numToSpawn; i++)
            {
                var wolfKind = Pawn.MapHeld.Biome == BiomeDefOf.IceSheet ||
                               Pawn.MapHeld.Biome == BiomeDefOf.SeaIce ||
                               Pawn.MapHeld.Biome == BiomeDefOf.Tundra
                    ? PawnKindDef.Named("Wolf_Arctic")
                    : PawnKindDef.Named("Wolf_Timber");
                var pawn = PawnGenerator.GeneratePawn(wolfKind, Pawn.Faction);
                GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(Pawn.Position, Pawn.Map, 4), Pawn.Map);
                var lord = Pawn.GetLord();
                lord.AddPawn(pawn);
            }
        }

        #endregion Methods

        #region Pawn Overrides

        //public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        //{

        //}

        /// 
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            //foreach (Gizmo x in base.CompGetGizmosExtra())
            //{
            //    yield return x;
            //}

            if (IsWerewolf && !IsTransformed && IsBlooded)
            {
                if (DebugSettings.godMode)
                {
                    if (Find.Selector.NumSelected == 1)
                    {
                        var command_Action = new Command_Action
                        {
                            action = delegate
                            {
                                var list = new List<FloatMenuOption>();
                                foreach (var current in WerewolfForms)
                                {
                                    list.Add(new FloatMenuOption("Level up " + current.def.LabelCap,
                                        delegate { current.LevelUp(); }));
                                }

                                Find.WindowStack.Add(new FloatMenu(list));
                            },
                            defaultLabel = "(God Mode) Level Up Forms",
                            defaultDesc = "",
                            hotKey = KeyBindingDefOf.Misc1,
                            icon = TexCommand.ClearPrioritizedWork
                        };
                        yield return command_Action;
                    }
                }


                if (Find.Selector.NumSelected == 1)
                {
                    foreach (var form in WerewolfForms)
                    {
                        if (form.level <= 0)
                        {
                            continue;
                        }

                        var wolfFormButton = new Command_WerewolfButton(this)
                        {
                            defaultLabel = form.def.label + " Lv." + form.level,
                            defaultDesc = form.Desc,
                            icon = form.def.Icon,
                            action = delegate
                            {
                                TransformInto(form);
                                CooldownTicksLeft = CooldownMaxTicks;
                            }
                        };
                        if (CooldownTicksLeft > 0)
                        {
                            wolfFormButton.Disable("ROM_NeedsRest".Translate(Pawn.LabelShort));
                        }

                        yield return wolfFormButton;
                    }
                }

                yield return new Command_Toggle
                {
                    defaultLabel = "ROM_FullMoonFury".Translate(),
                    defaultDesc = "ROM_FullMoonFuryDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Button/MoonFury"),
                    isActive = () => furyToggled,
                    toggleAction = delegate { furyToggled = !furyToggled; }
                };
            }
            else if (IsTransformed &&
                     Pawn?.health?.hediffSet?.hediffs
                         .FirstOrDefault(x => x.TryGetComp<HediffComp_Rage>() != null) is { } rageHediff &&
                     rageHediff.TryGetComp<HediffComp_Rage>() is { } rageComp)
            {
                yield return new Gizmo_HediffRageStatus
                {
                    rage = rageComp
                };
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Pawn.Spawned)
            {
                return;
            }

            ResolveAIFactionSpawns();

            if (!IsWerewolf)
            {
                return;
            }

            if (CooldownTicksLeft > 0)
            {
                CooldownTicksLeft--;
            }

            ResolveAIHostileReaction();

            if (!needsGraphicRefresh)
            {
                return;
            }

            if (WerewolfForms.FirstOrDefault(x => x.bodyGraphicData != null) is { } f)
            {
                f.bodyGraphicData = null;
            }

            Pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            needsGraphicRefresh = false;
        }

        public override bool TryTransformPawn()
        {
            return IsWerewolf || Pawn?.Faction?.def?.defName == "ROM_WerewolfClan";
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref factionResolved, "factionResolved");
            Scribe_Values.Look(ref furyToggled, "furyToggled");
            Scribe_Values.Look(ref isReverting, "isReverting");
            Scribe_Values.Look(ref isBlooded, "isBlooded");
            Scribe_Values.Look(ref cooldownTicksLeft, "cooldownTicksLeft", -1);
            Scribe_References.Look(ref currentWerewolfForm, "currentWerewolfForm");
            Scribe_Collections.Look(ref werewolfForms, "werewolfForms", LookMode.Deep);
            Scribe_Collections.Look(ref storedItems, "storedWeapons", LookMode.Reference);
            Scribe_Collections.Look(ref upperBodyItems, "upperBodyItems", LookMode.Reference);
        }

        #endregion Pawn Overrides
    }
}