// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about the abilities of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Abilities : WindowSection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Abilities"/> class.
		/// </summary>
		public WindowSection_Abilities()
			: base()
		{
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "Abilities".Translate();

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			if (XPP_API.PreviewPawn.abilities.abilities.Count == 0)
			{
				Widgets.NoneLabelCenteredVertically(this.BoundsContents, $"[{"Karda.XPP.Abilities.None.Label".Translate()}]");

				return this.DesiredContentSize;
			}

			Rect outRect = GenUI.DrawElementStack(
				rect: this.BoundsContents,
				rowHeight: WindowButtonSmall.Size.y,
				elements: XPP_API.PreviewPawn.abilities.abilities,
				drawer: (background, element) =>
				{
					new WindowButtonSmall(
						bounds: background,
						callback: () => Find.WindowStack.Add(new Dialog_InfoCard(element.def)),
						icon: element.def.uiIcon,
						tooltip: element.Tooltip)
					.DrawComponent();
				},
				widthGetter: x => WindowButtonSmall.Size.x,
				rowMargin: XPP_API.MarginElements,
				elementMargin: XPP_API.MarginElements,
				allowOrderOptimization: true);

			return outRect.size;
		}
	}
}
