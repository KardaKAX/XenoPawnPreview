// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using BigAndSmall;
	using HarmonyLib;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains compatibility and utility functionality for compatibility with other mods.
	/// </summary>
	public static class CompatibilityUtility
	{
		/// <summary>
		/// The <see cref="HarmonyPatchCategory"/> used for core patches.
		/// </summary>
		public const string HarmonyCategoryCore = "XPP_Core";

		static CompatibilityUtility()
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			BigAndSmall_Assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "BigAndSmall");
			IdeoFactIcon_Assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "IdeoFactIcon");
		}

		/// <summary>
		/// Defines the types of windows which are supported by this mod.
		/// </summary>
		public enum WindowType
		{
			/// <summary>
			/// The preview window will not be displayed with this type.
			/// </summary>
			Undisplayed,

			/// <summary>
			/// The preview window will read from Rimworld's supported menus.
			/// </summary>
			Rimworld,

			/// <summary>
			/// The preview window will read from the 'CharacterEditor.DialogXenoType' class.
			/// </summary>
			CharacterEditor,

			/// <summary>
			/// The preview window will read from the 'WVC_XenotypesAndGenes.Dialog_Generemover' class.
			/// </summary>
			WVC_XaG_Generemover,

			/// <summary>
			/// The preview window will read from the 'WVC_XenotypesAndGenes.Dialog_Morpher' class.
			/// </summary>
			WVC_XaG_Morpher,

			/// <summary>
			/// The preview window will read from the 'WVC_XenotypesAndGenes.Dialog_XenotypeHolderBasic' class.
			/// </summary>
			WVC_XaG_XenotypeHolderBasic,
		}

		/// <summary>
		/// Gets the mod assembly for <b>Big and Small</b>.
		/// </summary>
		public static Assembly BigAndSmall_Assembly { get; }

		/// <summary>
		/// Gets the mod assembly for <b>Ideoligion Icon as Faction Icon</b>.
		/// </summary>
		public static Assembly IdeoFactIcon_Assembly { get; }

		/// <summary>
		/// Defines a <see cref="HarmonyPrefix"/> patch for Big and Small's 'GetInvalidateLater' method, allowing it to cache pawns on the main menu.
		/// </summary>
		/// <param name="__result">The original result of the targeted method.</param>
		/// <param name="pawn">The <see cref="Pawn"/> target of this method.</param>
		/// <returns><see langword="true"/> if the original method is to be executed.</returns>
		public static bool BigAndSmall_AllowCachingOnEntry(ref object __result, Pawn pawn)
		{
			if (Find.UIRoot is UIRoot_Entry)
			{
				// Reflection is far too laggy for this.
				__result = HumanoidPawnScaler.GetCache(pawn, true);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Defines a <see cref="HarmonyPrefix"/> patch for <b>Ideoligion Icon as Faction Icon</b>'s 'Get_FactionIcon_Helper' methor, bypassing the cache query if we're on the main menu.
		/// </summary>
		/// <param name="__result">The original result of the targeted method.</param>
		/// <param name="faction">The <see cref="Faction"/> target of this method.</param>
		/// <returns><see langword="true"/> if the original method is to be executed.</returns>
		public static bool IdeoFactIcon_SkipComponentQuery(ref Texture2D __result, Faction faction)
		{
			if (Find.UIRoot is UIRoot_Entry)
			{
				__result = faction.def.FactionIcon;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the genes of the given <paramref name="pawn"/>.
		/// </summary>
		/// <param name="window">The <see cref="GeneCreationDialogBase"/> window which should be read from.</param>
		/// <returns>A list of <see cref="GeneDef"/>s and if they are stored as a xenogene.</returns>
		public static List<GeneDefWithType> GetSelectedGenes(this GeneCreationDialogBase window)
		{
			var genes = new List<GeneDefWithType>();

			if (window == null)
			{
				return genes;
			}

			switch (HarmonyPatches_Core.WindowType)
			{
				case CompatibilityUtility.WindowType.Rimworld:
					genes.AddRange(Traverse.Create(window).Field("tmpGenesWithType").GetValue<List<GeneDefWithType>>());
					break;

				case CompatibilityUtility.WindowType.CharacterEditor:
					genes.AddRange(Traverse.Create(window).Field("tmpGenesWithType").GetValue<List<GeneDefWithType>>());
					break;

				case CompatibilityUtility.WindowType.WVC_XaG_Generemover:
					genes.AddRange(Traverse.Create(window).Field("selectedGenes").GetValue<List<GeneDef>>().Select(x => new GeneDefWithType(x, true)));
					break;

				case CompatibilityUtility.WindowType.WVC_XaG_Morpher:
					genes.AddRange(Traverse.Create(window).Field("selectedXenogenes").GetValue<List<GeneDef>>().Select(x => new GeneDefWithType(x, true)));
					genes.AddRange(Traverse.Create(window).Field("selectedEndogenes").GetValue<List<GeneDef>>().Select(x => new GeneDefWithType(x, false)));
					break;

				case CompatibilityUtility.WindowType.WVC_XaG_XenotypeHolderBasic:
					genes.AddRange(Traverse.Create(window).Field("selectedGenes").GetValue<List<GeneDef>>().Select(x => new GeneDefWithType(x, true)));
					break;

				default:
					break;
			}

			return genes.Distinct().ToList();
		}

		/// <summary>
		/// Gets the original <see cref="Pawn"/> target of the given <paramref name="window"/>.
		/// </summary>
		/// <param name="window"><inheritdoc cref="GetSelectedGenes(GeneCreationDialogBase)" path="/param[@name='window']"/></param>
		/// <returns>The original target <see cref="Pawn"/> of the <paramref name="window"/>.</returns>
		public static Pawn GetSelectedPawn(this GeneCreationDialogBase window)
		{
			if (window == null)
			{
				return null;
			}

			switch (HarmonyPatches_Core.WindowType)
			{
				case WindowType.Rimworld:
					return Current.ProgramState == ProgramState.Entry
						? Find.GameInitData.startingAndOptionalPawns.ElementAtOrDefault(Traverse.Create(window).Field("generationRequestIndex").GetValue<int>())
						: Find.Selector.SelectedPawns.FirstOrDefault();

				case WindowType.CharacterEditor:
					return Traverse.Create(window).Field("pawn").GetValue<Pawn>();

				case WindowType.WVC_XaG_Generemover:
					return Traverse.Create(window).Field("gene").GetValue<Gene>()?.pawn;

				case WindowType.WVC_XaG_Morpher:
					return Traverse.Create(window).Field("gene").GetValue<Gene>()?.pawn;

				default:
					return null;
			}
		}

		/// <summary>
		/// Performs all patches for this mod.
		/// </summary>
		/// <param name="harmony">The <see cref="Harmony"/> instance for this mod.</param>
		public static void Patch(Harmony harmony)
		{
			harmony.PatchCategory(HarmonyCategoryCore);

			// Patch OnGenesChanged in all methods.
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(y => typeof(GeneCreationDialogBase).IsAssignableFrom(y))))
			{
				MethodInfo queryMethod = type.GetMethod("OnGenesChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

				// Skip if method doesn't exist
				if (queryMethod == null
					|| queryMethod.DeclaringType != type
					|| !queryMethod.IsVirtual
					|| queryMethod.GetBaseDefinition() == queryMethod
					|| queryMethod.GetMethodBody()?.GetILAsByteArray()?.Length == 0)
				{
					continue;
				}

				harmony.Patch(
					original: queryMethod,
					postfix: new HarmonyMethod(typeof(HarmonyPatches_Core), nameof(HarmonyPatches_Core.GeneCreationDialogBase_OnGenesChanged)));
			}

			if (BigAndSmall_Assembly != null && XPP_API.Settings.PatchBigAndSmall)
			{
				harmony.Patch(
					original: AccessTools.Method(BigAndSmall_Assembly.GetType("BigAndSmall.HumanoidPawnScaler"), "GetInvalidateLater"),
					prefix: new HarmonyMethod(typeof(CompatibilityUtility), nameof(BigAndSmall_AllowCachingOnEntry)));
			}

			if (IdeoFactIcon_Assembly != null && XPP_API.Settings.PatchIdeoFactIcon)
			{
				harmony.Patch(
					original: AccessTools.Method(IdeoFactIcon_Assembly.GetType("nuff.Ideology_Faction_Icon.HarmonyPatches"), "Get_FactionIcon_Helper"),
					prefix: new HarmonyMethod(typeof(CompatibilityUtility), nameof(IdeoFactIcon_SkipComponentQuery)));
			}
		}
	}
}
