// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// The API for the Xenotype Pawn Preview mod.
	/// </summary>
	public static class XPP_API
	{
		/// <summary>
		/// The common size margin between elements.
		/// </summary>
		public const float MarginElements = 6f;

		/// <summary>
		/// Gets the <see cref="GeneCreationDialogBase"/> window which the <see cref="PreviewWindow"/> is attached to.
		/// </summary>
		public static GeneCreationDialogBase BaseWindow { get; internal set; }

		/// <summary>
		/// Gets the <see cref="Pawn"/> which was the original target of the <see cref="BaseWindow"/>.
		/// </summary>
		public static Pawn BasePawn { get; internal set; }

		/// <summary>
		/// Gets the currently active <see cref="XenoPawnPreview.PreviewWindow"/> instance.
		/// </summary>
		public static PreviewWindow PreviewWindow { get; internal set; }

		/// <summary>
		/// Gets the <see cref="Pawn"/> displayed in the preview window.
		/// </summary>
		public static Pawn PreviewPawn { get; internal set; }

		/// <summary>
		/// Gets this mod's settings.
		/// </summary>
		public static XPP_Settings Settings { get; internal set; }
	}
}
