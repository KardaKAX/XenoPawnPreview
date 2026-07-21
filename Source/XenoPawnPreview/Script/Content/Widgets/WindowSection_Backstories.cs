// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains basic information about the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Backstories : WindowSection
	{
		private const float MarginKeyValues = 90f;

		private readonly BackstorySlot[] backstories;

		private readonly Rect rectLabelDef;

		private readonly Rect rectLabelDesc;

		private Rect rectBackstories;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Backstories"/> class.
		/// </summary>
		public WindowSection_Backstories()
			: base()
		{
			this.backstories = (BackstorySlot[])Enum.GetValues(typeof(BackstorySlot));

			this.rectLabelDef = new Rect(0f, 0f, MinWidth, Text.LineHeight);
			this.rectLabelDesc = new Rect(0f, this.rectLabelDef.yMax, MinWidth, Text.LineHeight);
			this.rectBackstories = new Rect(0f, this.rectLabelDesc.yMax, MinWidth, default);
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "Backstory".Translate();

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			Widgets.Label(this.rectLabelDef, XPP_API.PreviewPawn.def.LabelCap.ToString());

			if (Mouse.IsOver(this.rectLabelDef))
			{
				TooltipHandler.TipRegion(this.rectLabelDef, XPP_API.PreviewPawn.def.description);
			}

			Widgets.Label(this.rectLabelDesc, XPP_API.PreviewPawn.MainDesc(false, true));

			this.rectBackstories.height = (this.backstories.Length * Text.LineHeight) + (Math.Max(0, this.backstories.Length - 1) * XPP_API.MarginElements);

			Widgets.BeginGroup(this.rectBackstories);

			try
			{
				float curY = 0f;

				foreach (BackstorySlot bsSlot in this.backstories)
				{
					Rect bsRect = new Rect(0f, curY, MinWidth, Text.LineHeight);
					BackstoryDef bsDef = XPP_API.PreviewPawn.story.GetBackstory(bsSlot);

					if (bsDef != null)
					{
						string bsValueText = bsDef?.TitleCapFor(XPP_API.PreviewPawn.gender);

						Text.Anchor = TextAnchor.MiddleLeft;
						Widgets.Label(bsRect, bsSlot.ToString().Translate());

						bsRect.xMin += MarginKeyValues;

						GUI.color = CharacterCardUtility.StackElementBackground;
						GUI.DrawTexture(bsRect, BaseContent.WhiteTex);
						GUI.color = this.OldColour;

						Text.Anchor = TextAnchor.MiddleCenter;
						Widgets.Label(bsRect, bsValueText.Truncate(bsRect.width));
						Text.Anchor = this.OldAnchor;

						if (Mouse.IsOver(bsRect))
						{
							Widgets.DrawHighlight(bsRect);
							TooltipHandler.TipRegion(bsRect, bsDef.FullDescriptionFor(XPP_API.PreviewPawn).Resolve());
						}
					}

					curY += Text.LineHeight + XPP_API.MarginElements;
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[XPP] Exception whilst listing backstories:\n{ex}");
			}

			Widgets.EndGroup(); // rectBackstories

			return new Vector2(MinWidth, this.rectBackstories.yMax - this.rectLabelDef.yMin);
		}
	}
}
