// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Linq;
	using System.Reflection;
	using HarmonyLib;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Creates a new Xenotype Pawn Preview mod instance.
	/// </summary>
	[StaticConstructorOnStartup]
	public class XPP_Mod : Mod
	{
		/// <summary>
		/// The ID and base namespace of the mod.
		/// </summary>
		public const string ModID = "karda.xenopawnpreview";

		/// <summary>
		/// The user-friendly name of this mod.
		/// </summary>
		public const string ModName = "Xenotype Pawn Preview";

		/// <summary>
		/// The <see cref="HarmonyPatchCategory"/> used for core patches.
		/// </summary>
		public const string HarmonyCategoryCore = "XPP_Core";

		private static readonly Listing_Standard SettingsListing = new Listing_Standard();

		private static XPP_Settings modSettings;

		/// <summary>
		/// Initializes a new instance of the <see cref="XPP_Mod"/> class.
		/// </summary>
		/// <param name="content">The <see cref="ModContentPack"/> to be added.</param>
		public XPP_Mod(ModContentPack content)
			: base(content)
		{
			Harmony harmony = new Harmony(ModID);

			if (XPPContentPack == null)
			{
				XPPContentPack = content;
			}

			harmony.PatchCategory(HarmonyCategoryCore);

			if (CompatibilityUtility.BigAndSmall_Assembly != null && ModSettings.PatchBigAndSmall)
			{
				harmony.Patch(
					original: AccessTools.Method(CompatibilityUtility.BigAndSmall_Assembly.GetType("BigAndSmall.HumanoidPawnScaler"), "GetInvalidateLater"),
					prefix: new HarmonyMethod(typeof(CompatibilityUtility), nameof(CompatibilityUtility.BigAndSmall_AllowCachingOnEntry)));
			}

			if (CompatibilityUtility.IdeoFactIcon_Assembly != null && ModSettings.PatchIdeoFactIcon)
			{
				harmony.Patch(
					original: AccessTools.Method(CompatibilityUtility.IdeoFactIcon_Assembly.GetType("nuff.Ideology_Faction_Icon.HarmonyPatches"), "Get_FactionIcon_Helper"),
					prefix: new HarmonyMethod(typeof(CompatibilityUtility), nameof(CompatibilityUtility.IdeoFactIcon_SkipComponentQuery)));
			}

			modSettings = this.GetSettings<XPP_Settings>();

			Log.Message($"✓ - {ModName} loaded successfully.");
		}

		/// <summary>
		/// Gets this mod as a <see cref="ModContentPack"/>.
		/// </summary>
		public static ModContentPack XPPContentPack { get; private set; }

		/// <summary>
		/// Gets the mod settings.
		/// </summary>
		public static XPP_Settings ModSettings { get => modSettings; }

		/// <summary>
		/// Draws the settings window contents.
		/// </summary>
		/// <param name="inRect">The bounds of the settings window.</param>
		public override void DoSettingsWindowContents(Rect inRect)
		{
			SettingsListing.Begin(inRect);

			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Window.Standalone.Label".Translate(), ref ModSettings.WindowStandalone, "Karda.XPP.Settings.Window.Standalone.Tooltip".Translate());
			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Patch.BigAndSmall.Label".Translate(), ref ModSettings.PatchBigAndSmall, "Karda.XPP.Settings.Patch.BigAndSmall.Tooltip".Translate());
			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Patch.IdeoFactIcon.Label".Translate(), ref ModSettings.PatchIdeoFactIcon, "Karda.XPP.Settings.Patch.IdeoFactIcon.Tooltip".Translate());

			SettingsListing.End();
		}

		/// <summary>
		/// Sets the settings category name.
		/// </summary>
		/// <returns>The name of the settings category.</returns>
		public override string SettingsCategory() => ModName;
	}
}
