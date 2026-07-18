namespace Karda.XenoPawnPreview
{
	using System;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about traits of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Traits : WindowSection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Traits"/> class.
		/// </summary>
		public WindowSection_Traits()
			: base()
		{
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "Traits".Translate();

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			if (XPP_API.PreviewPawn.story.traits.allTraits.Count == 0)
			{
				Widgets.NoneLabelCenteredVertically(this.BoundsContents, $"[{"Karda.XPP.Traits.None.Label".Translate()}]");

				return this.DesiredContentSize;
			}

			Rect outRect = GenUI.DrawElementStack(
				rect: this.BoundsContents,
				rowHeight: Text.LineHeight,
				elements: XPP_API.PreviewPawn.story.traits.TraitsSorted,
				drawer: (background, element) =>
				{
					GUI.color = CharacterCardUtility.StackElementBackground;
					GUI.DrawTexture(background, BaseContent.WhiteTex);
					GUI.color = this.OldColour;

					if (Mouse.IsOver(background))
					{
						Widgets.DrawHighlight(background);
					}

					if (element.Suppressed)
					{
						GUI.color = ColoredText.SubtleGrayColor;
					}
					else if (element.sourceGene != null)
					{
						GUI.color = ColoredText.GeneColor;
					}

					Text.Anchor = TextAnchor.MiddleCenter;
					Widgets.Label(background, element.LabelCap);
					Text.Anchor = 0;
					GUI.color = this.OldColour;

					if (Mouse.IsOver(background))
					{
						TooltipHandler.TipRegion(background, element.TipString(XPP_API.PreviewPawn));
					}
				},
				widthGetter: x => Text.CalcSize(x.LabelCap).x + (XPP_API.MarginElements * 2f),
				allowOrderOptimization: false);

			return outRect.size;
		}
	}
}
