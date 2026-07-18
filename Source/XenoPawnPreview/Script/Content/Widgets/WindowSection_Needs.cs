namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about the needs of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Needs : WindowSection
	{
		private float needLabelWidth;

		private List<Need> pawnNeeds;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Needs"/> class.
		/// </summary>
		public WindowSection_Needs()
			: base()
		{
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "TabNeeds".Translate();

		/// <inheritdoc/>
		public override void Update()
		{
			this.pawnNeeds = XPP_API.PreviewPawn.needs.AllNeeds
				.Where(x => x.ShowOnNeedList || x is Need_Mood)
				.OrderByDescending(y => y.def.listPriority)
				.ToList();

			this.needLabelWidth = MathUtility.Max(this.pawnNeeds.Select(x => Text.CalcSize(x.LabelCap).x + XPP_API.MarginElements));

			XPP_API.PreviewPawn.needs.AddOrRemoveNeedsAsAppropriate();
		}

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			float curY = 0f;

			foreach (var need in this.pawnNeeds)
			{
				Rect rectNeeds = new Rect(0f, curY, MinWidth, Text.LineHeight);

				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(rectNeeds, need.LabelCap);
				Text.Anchor = this.OldAnchor;

				rectNeeds.xMin += this.needLabelWidth;

				need.DrawOnGUI(rect: rectNeeds, customMargin: 0f, drawArrows: false, drawLabel: false);

				curY += Text.LineHeight;
			}

			return new Vector2(MinWidth, curY);
		}
	}
}
