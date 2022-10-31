using System;
using System.Linq;
using HarmonyLib;
using Verse;

namespace Werewolf;

[StaticConstructorOnStartup]
public static class WerewolfDebugActions
{
    [DebugAction("Werewolves", 
        name: "Give Lycanthropy (Normal)", 
        actionType = DebugActionType.ToolMapForPawns)]
    private static void GiveLycanthropyNormal(Pawn p)
    {
        if (p?.RaceProps?.Humanlike == true)
            p.AddWerewolfTrait(false, true);
    }
    
    
    [DebugAction("Werewolves", 
        name: "Give Lycanthropy (Metis Chance)", 
        actionType = DebugActionType.ToolMapForPawns)]
    private static void GiveLycanthropyMetis(Pawn p)
    {
        if (p?.RaceProps?.Humanlike == true)
            p.AddWerewolfTrait(true, true);
    }
    
    
    [DebugAction("Werewolves", 
        name: "Remove Lycanthropy", actionType = 
            DebugActionType.ToolMapForPawns)]
    private static void RemoveLycanthropy(Pawn p)
    {
        if (p?.RaceProps?.Humanlike == true)
            p.RemoveWerewolfTrait(true);
    }
    
    
    [DebugAction(category: "Werewolves", 
        name: "Regenerate Moons", 
        allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void RegenerateMoons()
    {
        Find.World.GetComponent<WorldComponent_MoonCycle>().DebugRegenerateMoons(Find.World);
    }
    
    
    [DebugAction(category: "Werewolves", 
        name: "Trigger Full Moon", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TriggerFullMoon()
    {
        Find.World.GetComponent<WorldComponent_MoonCycle>().DebugTriggerNextFullMoon();
    }
    
    
}