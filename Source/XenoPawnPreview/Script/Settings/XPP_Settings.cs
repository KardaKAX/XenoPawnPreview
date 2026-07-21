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
		/// Determines if the values associated with the resource section should be displayed under the bar, rather than inside.
		/// </summary>
		public bool ResourceValuesCompact = false;

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
			Scribe_Values.Look(ref this.PatchBigAndSmall, "PatchBigAndSmall");
			Scribe_Values.Look(ref this.PatchIdeoFactIcon, "PatchIdeoFactIcon");

			Scribe_Values.Look(ref this.PawnGenerateMinimal, "PawnGenerateMinimal");

			Scribe_Values.Look(ref this.ResourceValuesCompact, "ResourceValuesCompact");

			Scribe_Values.Look(ref this.WindowOffset, "WindowOffset");
			Scribe_Values.Look(ref this.WindowStandalone, "WindowStandalone");
		}
	}
}
