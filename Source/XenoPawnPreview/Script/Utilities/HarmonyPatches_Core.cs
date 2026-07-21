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
	[HarmonyPatchCategory(CompatibilityUtility.HarmonyCategoryCore)]
	public static class HarmonyPatches_Core
	{
		private static WorldGenPart genStage;

		/// <summary>
		/// Handles updates to the genes being changed within the editor.
		/// </summary>
		/// <param name="newGenes">The new selection of genes by the player.</param>
		public delegate void GenesChangedEventHandler(List<GeneDefWithType> newGenes);

		/// <summary>
		/// Called when the genes have been changed within the editor.
		/// </summary>
		public static event GenesChangedEventHandler GenesChanged;

		[Flags]
		private enum WorldGenPart
		{
			None = 0,
			Game = 1,
			InitData = 2,
			Scenario = 4,
			Storyteller = 8,
			World = 16,
		}

		/// <summary>
		/// Gets the supported window type currently in use with the preview window.
		/// </summary>
		public static CompatibilityUtility.WindowType WindowType { get; private set; }

		/// <summary>
		/// Sets a value indicating whether we are currently requiring modification to the pawn generation for safe generation.
		/// </summary>
		/// <remarks>This will automatically revert to <see langword="false"/> once generation has started.</remarks>
		public static bool PrepareGeneration { private get; set; }

		/// <summary>
		/// Gets the original <see cref="Rect.position"/> of the displayed <see cref="GeneCreationDialogBase"/> <see cref="Window"/>.
		/// </summary>
		public static Vector2 OriginalWindowPosition { get; private set; }

		/// <summary>
		/// Postfixes the <see cref="Dialog_CreateXenotype.OnGenesChanged"/> method to register updates when the player changes genes.
		/// </summary>
		/// <param name="__instance">The <see cref="Dialog_CreateXenotype"/> instance that opened.</param>
		/// <remarks>This method is patched on every instance of a <see cref="GeneCreationDialogBase"/>.</remarks>
		public static void GeneCreationDialogBase_OnGenesChanged() => GenesChanged?.Invoke(XPP_API.BaseWindow.GetSelectedGenes());

		/// <summary>
		/// Prefixes the <see cref="Need_Food.MaxLevel"/> property getter to enable the full need display outside of a playing program state.
		/// </summary>
		/// <param name="__result">The original result of the method.</param>
		/// <param name="___pawn">The local <see cref="Pawn"/> this tracker is linked to.</param>
		/// <returns><see langword="true"/> if the original method is allowed to execute after the patch.</returns>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(Need_Food), "get_MaxLevel")]
		public static bool NeedFood_GetMaxLevel(ref float __result, Pawn ___pawn)
		{
			__result = ___pawn.GetStatValue(StatDefOf.MaxNutrition, true, 15);

			return false;
		}

		/// <summary>
		/// Prefixes the <see cref="PawnGenerator"/>.TryGenerateNewPawnInternal method to fix issues on the created preview pawn.
		/// </summary>
		/// <param name="request">The <see cref="PawnGenerationRequest"/> of the duplicate.</param>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
		public static void PawnGenerator_TryGenerateNewPawnInternal(ref PawnGenerationRequest request)
		{
			if (PrepareGeneration)
			{
				request.CanGeneratePawnRelations = false;

				PrepareGeneration = false;
			}
		}

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
				CloseGracefully();
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
				gcdbInstance.absorbInputAroundWindow = false;
				XPP_API.BaseWindow = gcdbInstance;
				OriginalWindowPosition = gcdbInstance.windowRect.position;

				// Create a temporary world if one doesn't exist.
				if (Current.ProgramState == ProgramState.Entry)
				{
					if (Current.Game == null)
					{
						try
						{
							Current.Game = new Game();

							Log.Message("[XPP] Temporary world: Created 'Game'.");
							genStage |= WorldGenPart.Game;
						}
						catch (Exception ex)
						{
							CloseGracefully($"Temporary world generation failed whilst creating 'Game'.\n{ex}");
							return;
						}
					}

					if (Current.Game.InitData == null || Current.Game.InitData.startingPawnCount < 1)
					{
						try
						{
							Current.Game.InitData = new GameInitData()
							{
								startingPawnCount = 1,
							};

							Log.Message("[XPP] Temporary world: Created 'Game.InitData'.");
							genStage |= WorldGenPart.InitData;
						}
						catch (Exception ex)
						{
							CloseGracefully($"Temporary world generation failed whilst creating 'Game.InitData'.\n{ex}");
							return;
						}
					}

					if (Current.Game.Scenario == null)
					{
						try
						{
							Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
							Find.Scenario.PreConfigure();

							Log.Message("[XPP] Temporary world: Created 'Game.Scenario'.");
							genStage |= WorldGenPart.Scenario;
						}
						catch (Exception ex)
						{
							CloseGracefully($"Temporary world generation failed whilst creating 'Game.Scenario'.\n{ex}");
							return;
						}
					}

					if (Current.Game.storyteller.def == null || Current.Game.storyteller.difficultyDef == null)
					{
						try
						{
							Current.Game.storyteller.def = StorytellerDefOf.Cassandra;
							Current.Game.storyteller.difficultyDef = DifficultyDefOf.Rough;

							Log.Message("[XPP] Temporary world: Assigned 'Game.Storyteller'.");
							genStage |= WorldGenPart.Storyteller;
						}
						catch (Exception ex)
						{
							CloseGracefully($"Temporary world generation failed whilst assigning 'Game.Storyteller'.\n{ex}");
							return;
						}
					}

					if (Current.Game.World == null)
					{
						try
						{
							Current.Game.InitData.ResetWorldRelatedMapInitData();
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
									FactionDefOf.OutlanderCivil,
								});
							Find.GameInitData.startingTile = TileFinder.RandomStartingTile();

							Log.Message("[XPP] Temporary world: Created 'Game.World'.");
							genStage |= WorldGenPart.World;
						}
						catch (Exception ex)
						{
							CloseGracefully($"Temporary world generation failed whilst creating 'Game.World'.\n{ex}");
							return;
						}
					}
				}

				if (gcdbInstance is Dialog_CreateXenotype || gcdbInstance is Dialog_CreateXenogerm)
				{
					WindowType = CompatibilityUtility.WindowType.Rimworld;
				}
				else if (gcdbInstance.IsWindowOfType("CharacterEditor.DialogXenoType, CharacterEditor"))
				{
					WindowType = CompatibilityUtility.WindowType.CharacterEditor;
				}
				else if (gcdbInstance.IsWindowOfType("WVC_XenotypesAndGenes.Dialog_Generemover, WVC_BiotechFramework_XenotypesAndGenes"))
				{
					WindowType = CompatibilityUtility.WindowType.WVC_XaG_Generemover;
				}
				else if (gcdbInstance.IsWindowOfType("WVC_XenotypesAndGenes.Dialog_Morpher, WVC_BiotechFramework_XenotypesAndGenes"))
				{
					WindowType = CompatibilityUtility.WindowType.WVC_XaG_Morpher;
				}
				else if (gcdbInstance.IsWindowOfType("WVC_XenotypesAndGenes.Dialog_XenotypeHolderBasic, WVC_BiotechFramework_XenotypesAndGenes"))
				{
					WindowType = CompatibilityUtility.WindowType.WVC_XaG_XenotypeHolderBasic;
				}
				else if (gcdbInstance.IsWindowOfType(new string[]
				{
					"WVC_XenotypesAndGenes.Dialog_Golemlink, WVC_BiotechFramework_XenotypesAndGenes",
					"WVC_XenotypesAndGenes.Dialog_Voidlink, WVC_BiotechFramework_XenotypesAndGenes",
				}))
				{
					WindowType = CompatibilityUtility.WindowType.Undisplayed;
				}
				else
				{
					WindowType = CompatibilityUtility.WindowType.Undisplayed;
					CloseGracefully($"Unsupported window: {gcdbInstance}\nReport this to the mod author!");
					return;
				}

				if (WindowType != CompatibilityUtility.WindowType.Undisplayed && !Find.WindowStack.IsOpen(typeof(PreviewWindow)))
				{
					Find.WindowStack.Add(XPP_API.PreviewWindow = new PreviewWindow(gcdbInstance.GetSelectedPawn()));
				}
			}
		}

		/// <summary>
		/// Gets if the given <paramref name="type"/> is assignable to this <paramref name="window"/>.
		/// </summary>
		/// <param name="window">The <see cref="GeneCreationDialogBase"/> to match against.</param>
		/// <param name="type">The <see cref="GeneCreationDialogBase"/> type name to query.</param>
		/// <returns><see langword="true"/> if <paramref name="type"/> can be assigned to <paramref name="window"/>.</returns>
		public static bool IsWindowOfType(this GeneCreationDialogBase window, string type) => Type.GetType(type)?.IsAssignableFrom(window.GetType()) ?? false;

		/// <summary>
		/// Gets if any of the given <paramref name="types"/> are assignable to this <paramref name="window"/>.
		/// </summary>
		/// <param name="window"><inheritdoc cref="IsWindowOfType(GeneCreationDialogBase, string)" path="/param[@name='window']"/></param>
		/// <param name="types">The <see cref="GeneCreationDialogBase"/> types to query.</param>
		/// <returns><see langword="true"/> if any <paramref name="types"/> can be assigned to <paramref name="window"/>.</returns>
		public static bool IsWindowOfType(this GeneCreationDialogBase window, string[] types) => types.Any(x => window.IsWindowOfType(x));

		/// <summary>
		/// Returns from the creation of the preview window whilst cleaning up after itself.
		/// </summary>
		/// <param name="errorMessage">The error message to be displayed after failure.</param>
		private static void CloseGracefully(string errorMessage = "")
		{
			XPP_API.BaseWindow = null;
			XPP_API.PreviewWindow?.Close(false);
			XPP_API.PreviewWindow = null;

			if (genStage.HasFlag(WorldGenPart.Game))
			{
				Current.Game = null;

				Log.Message("[XPP] Temporary world: Cleared 'Game'.");
				genStage = WorldGenPart.None;
			}

			if (genStage.HasFlag(WorldGenPart.InitData))
			{
				Current.Game.InitData = null;

				Log.Message("[XPP] Temporary world: Cleared 'Game.InitData'.");
				genStage &= ~WorldGenPart.InitData;
			}

			if (genStage.HasFlag(WorldGenPart.Scenario))
			{
				Current.Game.Scenario = null;

				Log.Message("[XPP] Temporary world: Cleared 'Game.Scenario'.");
				genStage &= ~WorldGenPart.Scenario;
			}

			// Storyteller only assigns default values if they were invalid.
			genStage &= ~WorldGenPart.Storyteller;

			if (genStage.HasFlag(WorldGenPart.World))
			{
				Current.Game.World = null;

				Log.Message("[XPP] Temporary world: Cleared 'Game.World'.");
				genStage &= ~WorldGenPart.World;
			}

			if (errorMessage != string.Empty)
			{
				Log.Error($"[XPP] {errorMessage}");
			}
		}
	}
}
