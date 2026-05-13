// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using RimWorld;
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
		/// Gets or sets the current <see cref="XenoPawnPreview.PreviewWindow"/> instance.
		/// </summary>
		private static PreviewWindow PreviewWindow { get; set; }

		/// <summary>
		/// Postfixes the <see cref="Dialog_CreateXenotype.OnGenesChanged"/> method to register updates when the player changes genes.
		/// </summary>
		/// <param name="__instance">The <see cref="Dialog_CreateXenotype"/> instance that opened.</param>
		[HarmonyPostfix]
		[HarmonyPatch(typeof(GeneCreationDialogBase), "OnGenesChanged")]
		public static void GeneCreationDialogBase_OnGenesChanged(GeneCreationDialogBase __instance)
		{
			GenesChanged?.Invoke(Traverse.Create(__instance).Field("tmpGenesWithType").GetValue<List<GeneDefWithType>>().Distinct().ToList());
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
				PreviewWindow?.Close(false);
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

				if (__instance is Dialog_CreateXenotype || __instance is Dialog_CreateXenogerm)
				{
					/// Ideo editor returns null
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
