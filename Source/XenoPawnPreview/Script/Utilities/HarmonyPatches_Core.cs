// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using RimWorld;
	using RimWorld.Planet;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains core <see cref="Harmony"/> patches used by this mod.
	/// </summary>
	[HarmonyPatch]
	[HarmonyPatchCategory(XPP_Mod.HarmonyCategoryCore)]
	public static class HarmonyPatches_Core
	{
		/// <summary>
		/// Handles updates to the genes being changed within the editor.
		/// </summary>
		/// <param name="newGenes">The new selection of genes by the player.</param>
		public delegate void GenesChangedEventHandler(List<GeneDefWithType> newGenes);

		/// <summary>
		/// Called when the genes have been changed within the editor.
		/// </summary>
		public static event GenesChangedEventHandler GenesChanged;

		/// <summary>
		/// Gets the <see cref="Pawn"/> being targeted by this editor window.
		/// </summary>
		public static Pawn OriginalPawn { get; private set; }

		/// <summary>
		/// Gets the genes present on the current target <see cref="Pawn"/>.
		/// </summary>
		public static List<GeneDefWithType> CurrentGenes
		{
			get => TargetWindow != null
				? Traverse.Create(TargetWindow).Field("tmpGenesWithType").GetValue<List<GeneDefWithType>>().Distinct().ToList()
				: new List<GeneDefWithType>();
		}

		/// <summary>
		/// Gets the original <see cref="Rect.position"/> of the displayed <see cref="GeneCreationDialogBase"/> <see cref="Window"/>.
		/// </summary>
		public static Vector2 OriginalWindowPosition { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether a world was created in the process of opening the preview window.
		/// </summary>
		private static bool CreatedWorld { get; set; }

		/// <summary>
		/// Gets or sets the current <see cref="XenoPawnPreview.PreviewWindow"/> instance.
		/// </summary>
		private static PreviewWindow PreviewWindow { get; set; }

		/// <summary>
		/// Gets or sets the currently targeted <see cref="GeneCreationDialogBase"/> window.
		/// </summary>
		private static GeneCreationDialogBase TargetWindow { get; set; }

		/// <summary>
		/// Postfixes the <see cref="Dialog_CreateXenotype.OnGenesChanged"/> method to register updates when the player changes genes.
		/// </summary>
		/// <param name="__instance">The <see cref="Dialog_CreateXenotype"/> instance that opened.</param>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(GeneCreationDialogBase), "OnGenesChanged")]
		public static void GeneCreationDialogBase_OnGenesChanged() => GenesChanged?.Invoke(CurrentGenes);

		/// <summary>
		/// Patches the <see cref="Window.Close(bool)"/> method to also close the <see cref="XenoPawnPreview.PreviewWindow"/> alongside any opened <see cref="GeneCreationDialogBase"/>.
		/// </summary>
		/// <param name="__instance">The instance of the <see cref="Window"/> that is open.</param>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Window), "PostClose")]
		public static void Window_PostClose(Window __instance)
		{
			if (__instance is GeneCreationDialogBase)
			{
				PreviewWindow?.Close(false);
				TargetWindow = null;

				if (CreatedWorld)
				{
					Current.Game = null;
					Find.GameInitData?.startingAndOptionalPawns.Clear();

					Log.Message("[XPP] Cleared the temporary world.");
				}
			}
		}

		/// <summary>
		/// Patches the <see cref="Window.PostOpen"/> method to also open the <see cref="XenoPawnPreview.PreviewWindow"/> alongside any opening <see cref="GeneCreationDialogBase"/>.
		/// </summary>
		/// <param name="__instance">The instance of the <see cref="Window"/> that is open.</param>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(Window), "PostOpen")]
		public static void Window_PostOpen(Window __instance)
		{
			if (__instance is GeneCreationDialogBase gcdbInstance)
			{
				__instance.absorbInputAroundWindow = false;
				TargetWindow = gcdbInstance;
				OriginalWindowPosition = gcdbInstance.windowRect.position;

				// Create a temporary world if one doesn't exist.
				if (Find.GameInitData == null && Current.ProgramState == ProgramState.Entry)
				{
					Current.Game = new Game();
					Current.Game.InitData = new GameInitData()
					{
						startingPawnCount = 1,
					};

					Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
					Find.Scenario.PreConfigure();

					Current.Game.storyteller = new Storyteller()
					{
						def = StorytellerDefOf.Cassandra,
						difficultyDef = DifficultyDefOf.Rough,
					};

					Current.Game.World = WorldGenerator.GenerateWorld(
						planetCoverage: 0.05f,
						seedString: "0",
						overallRainfall: OverallRainfall.Normal,
						overallTemperature: OverallTemperature.Normal,
						population: OverallPopulation.Normal,
						landmarkDensity: LandmarkDensity.Normal,
						factions: new List<FactionDef>
						{
							FactionDefOf.PlayerColony,
						});
					Find.GameInitData.startingTile = TileFinder.RandomStartingTile();

					Log.Message("[XPP] Created a temporary world");
					CreatedWorld = true;
				}

				if (__instance is Dialog_CreateXenotype || __instance is Dialog_CreateXenogerm)
				{
					OriginalPawn = Find.GameInitData.startingAndOptionalPawns.ElementAtOrDefault(Traverse.Create(__instance).Field("generationRequestIndex").GetValue<int>());
				}
				else if (Type.GetType("CharacterEditor.DialogXenoType, CharacterEditor")?.IsAssignableFrom(__instance.GetType()) ?? false)
				{
					OriginalPawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
				}
				else
				{
					Log.Error($"[Xenotype Pawn Preview] Unsupported window: {gcdbInstance}\nReport this to the mod author!");
					return;
				}

				if (!Find.WindowStack.IsOpen(typeof(PreviewWindow)))
				{
					Find.WindowStack.Add(PreviewWindow = new PreviewWindow(gcdbInstance));
				}
			}
		}
	}
}
