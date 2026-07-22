// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Stores and handles settings for the mod.
	/// </summary>
	public class XPP_Settings : ModSettings
	{
		/// <summary>
		/// Defines the default window offset for the editor.
		/// </summary>
		public static readonly Vector2 WindowOffsetDefault = new Vector2(-256f, 0f);

#pragma warning disable SA1401
		/// <summary>
		/// Determines if this mod should patch compatibility with <b>Big and Small</b>.
		/// </summary>
		public bool PatchBigAndSmall = true;

		/// <summary>
		/// Determines if this mod should patch compatibility with <b>Ideoligon Icon as Faction Icon</b>.
		/// </summary>
		public bool PatchIdeoFactIcon = true;

		/// <summary>
		/// Determines if the mod should always generate a 'minimal' pawn.
		/// </summary>
		public bool PawnGenerateMinimal = false;

		/// <summary>
		/// Determines if the values associated with the resource section should be displayed inside the bar, rather than under it.
		/// </summary>
		public bool ResourceValuesCompact = true;

		/// <summary>
		/// The maximum height multiplier of the preview window compared to the base editor.
		/// </summary>
		public float WindowHeightMax = 0.8f;

		/// <summary>
		/// Offsets the <see cref="GeneCreationDialogBase"/> by the given value on the horizontal axis.
		/// </summary>
		public Vector2 WindowOffset = WindowOffsetDefault;

		/// <summary>
		/// Determines if the <see cref="PreviewWindow"/> should not be constrained to any <see cref="GeneCreationDialogBase"/>.
		/// </summary>
		public bool WindowStandalone = false;
#pragma warning restore SA1401

		/// <summary>
		/// Exposes data from the scribe for reading saved mod data.
		/// </summary>
		public override void ExposeData()
		{
			Scribe_Values.Look(ref this.PatchBigAndSmall, "PatchBigAndSmall", true);
			Scribe_Values.Look(ref this.PatchIdeoFactIcon, "PatchIdeoFactIcon", true);

			Scribe_Values.Look(ref this.PawnGenerateMinimal, "PawnGenerateMinimal", false);

			Scribe_Values.Look(ref this.ResourceValuesCompact, "ResourceValuesCompact", true);

			Scribe_Values.Look(ref this.WindowOffset, "WindowOffset", WindowOffsetDefault);
			Scribe_Values.Look(ref this.WindowStandalone, "WindowStandalone", false);
			Scribe_Values.Look(ref this.WindowHeightMax, "WindowHeightMax", 0.8f);
		}
	}
}
