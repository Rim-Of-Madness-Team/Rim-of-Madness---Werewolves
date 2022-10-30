using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Werewolf
{
	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]

	static class PawnRenderer_RenderPawnAt_Patch
	{

		static bool ClearCache(Pawn pawn, bool useCache)
		{
			// use your own ShouldClearCache() implementation here
			if (useCache && ShouldClearCache(pawn))
				useCache = false;
			return useCache;
		}

		private static bool ShouldClearCache(Pawn pawn)
		{
			return true;
			//Log.ErrorOnce("Wolf Cache check active", 66228831);
			//if (WerewolfUtility.transformedWerewolfCount > 0)
			//{
			//	Log.ErrorOnce("Cache cleared for transformations", 66228835);
			//	return true;
			//}
   //         return false;
		}

		static readonly MethodInfo mGetPosture = SymbolExtensions.GetMethodInfo(() => PawnUtility.GetPosture(null));
		static readonly FieldInfo fPawn = AccessTools.Field(typeof(PawnRenderer), "pawn");
		static readonly MethodInfo mClearCache = SymbolExtensions.GetMethodInfo(() => ClearCache(default, default));
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var list = instructions.ToList();

			var idx = list.FindIndex(code => code.Calls(mGetPosture));
			if (idx == -1)
			{
				Log.Error("Cannot find CALL PawnUtility.GetPosture in PawnRenderer.RenderPawnAt");
				return list.AsEnumerable();
			}

			idx -= 2;
			if (list[idx].opcode != OpCodes.Ldarg_0)
			{
				Log.Error("Weird call to PawnUtility.GetPosture in PawnRenderer.RenderPawnAt");
				return list.AsEnumerable();
			}

			var ldloc = new CodeInstruction(OpCodes.Nop);
			var stloc = list[idx - 1].opcode;
			var stloc_nr = list[idx - 1].operand;
			if (stloc == OpCodes.Stloc_0) ldloc = new CodeInstruction(OpCodes.Ldloc_0);
			if (stloc == OpCodes.Stloc_1) ldloc = new CodeInstruction(OpCodes.Ldloc_1);
			if (stloc == OpCodes.Stloc_2) ldloc = new CodeInstruction(OpCodes.Ldloc_2);
			if (stloc == OpCodes.Stloc_3) ldloc = new CodeInstruction(OpCodes.Ldloc_3);
			if (stloc == OpCodes.Stloc) ldloc = new CodeInstruction(OpCodes.Ldloc, stloc_nr);
			if (ldloc.opcode == OpCodes.Nop)
			{
				Log.Error("Wrong local variable in PawnRenderer.RenderPawnAt");
				return list.AsEnumerable();
			}

			var labels = new List<Label>(list[idx].labels);
			list[idx].labels.Clear();
			list.InsertRange(idx, new[]
			{
			new CodeInstruction(OpCodes.Ldarg_0) { labels = labels },
			new CodeInstruction(OpCodes.Ldfld, fPawn),
			new CodeInstruction(ldloc),
			new CodeInstruction(OpCodes.Call, mClearCache),
			new CodeInstruction(stloc),
		});
			return list.AsEnumerable();
		}
	}
}
