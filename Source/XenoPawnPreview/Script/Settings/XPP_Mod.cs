// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
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

		private static readonly Listing_Standard SettingsListing = new Listing_Standard();

		/// <summary>
		/// Initializes a new instance of the <see cref="XPP_Mod"/> class.
		/// </summary>
		/// <param name="content">The <see cref="ModContentPack"/> to be added.</param>
		public XPP_Mod(ModContentPack content)
			: base(content)
		{
			XPP_API.Settings = this.GetSettings<XPP_Settings>();

			if (XPPContentPack == null)
			{
				XPPContentPack = content;
			}

			CompatibilityUtility.Patch(new Harmony(ModID));

			Log.Message($"✓ - {ModName} loaded successfully.");
		}

		/// <summary>
		/// Gets this mod as a <see cref="ModContentPack"/>.
		/// </summary>
		public static ModContentPack XPPContentPack { get; private set; }

		/// <summary>
		/// Draws the settings window contents.
		/// </summary>
		/// <param name="inRect">The bounds of the settings window.</param>
		public override void DoSettingsWindowContents(Rect inRect)
		{
			SettingsListing.Begin(inRect);

			SettingsListing.Label("Karda.XPP.Settings.Window.Category".Translate());
			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Window.Standalone.Label".Translate(), ref XPP_API.Settings.WindowStandalone, "Karda.XPP.Settings.Window.Standalone.Tooltip".Translate());
			XPP_API.Settings.WindowOffset.x = SettingsListing.SliderLabeled($"{"Karda.XPP.Settings.Window.OffsetX.Label".Translate()}: {XPP_API.Settings.WindowOffset.x:F0}px", XPP_API.Settings.WindowOffset.x, -Screen.width, Screen.width, tooltip: "Karda.XPP.Settings.Window.Offset.Tooltip".Translate());
			XPP_API.Settings.WindowOffset.y = SettingsListing.SliderLabeled($"{"Karda.XPP.Settings.Window.OffsetY.Label".Translate()}: {XPP_API.Settings.WindowOffset.y:F0}px", XPP_API.Settings.WindowOffset.y, -Screen.height, Screen.height, tooltip: "Karda.XPP.Settings.Window.Offset.Tooltip".Translate());

			if (SettingsListing.ButtonText("Karda.XPP.Generic.Reset.Label".Translate()))
			{
				XPP_API.Settings.WindowOffset = XPP_Settings.WindowOffsetDefault;
			}

			SettingsListing.Label(string.Empty);
			SettingsListing.Label("Karda.XPP.Settings.Patch.Category".Translate());
			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Patch.BigAndSmall.Label".Translate(), ref XPP_API.Settings.PatchBigAndSmall, "Karda.XPP.Settings.Patch.BigAndSmall.Tooltip".Translate());
			SettingsListing.CheckboxLabeled("Karda.XPP.Settings.Patch.IdeoFactIcon.Label".Translate(), ref XPP_API.Settings.PatchIdeoFactIcon, "Karda.XPP.Settings.Patch.IdeoFactIcon.Tooltip".Translate());

			SettingsListing.End();
		}

		/// <summary>
		/// Sets the settings category name.
		/// </summary>
		/// <returns>The name of the settings category.</returns>
		public override string SettingsCategory() => ModName;
	}
}
