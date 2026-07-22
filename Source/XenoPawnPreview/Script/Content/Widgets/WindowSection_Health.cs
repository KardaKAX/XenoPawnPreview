// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about the health status of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Health : WindowSection
	{
		private float hediffCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Health"/> class.
		/// </summary>
		public WindowSection_Health()
			: base()
		{
		}

		/// <summary>
		/// Gets the hediff listing height value.
		/// </summary>
		public static float HediffListingHeight { get; internal set; }

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "Health".Translate();

		/// <inheritdoc/>
		public override void Update()
		{
			this.hediffCount = XPP_API.PreviewPawn.health.hediffSet.GetMissingPartsCommonAncestors().Count + XPP_API.PreviewPawn.health.hediffSet.hediffs.Where(x => !(x is Hediff_MissingPart) && x.Visible).Count();

			foreach (var hediff in XPP_API.PreviewPawn.health.hediffSet.hediffs)
			{
				XPP_API.PreviewPawn.health.Notify_HediffChanged(hediff);
			}
		}

		/// <summary>
		/// Draws a summary of a <see cref="PawnCapacityDef"/>.
		/// </summary>
		/// <param name="rect">The <see cref="Rect"/> being targeted for drawing.</param>
		/// <param name="curY">The current Y offset of this row.</param>
		/// <param name="leftLabel">The label used to denote the <see cref="PawnCapacityDef"/> name.</param>
		/// <param name="rightLabel">The label used to denote the <see cref="PawnCapacityDef"/> state.</param>
		/// <param name="rightLabelColour">The colour applied to the <paramref name="rightLabel"/>.</param>
		/// <param name="tipSignal">The tooltip displayed when hovering over this <see cref="PawnCapacityDef"/>.</param>
		/// <remarks>
		/// Emulation of the <see cref="HealthCardUtility"/>.DrawLeftRow private method.
		/// </remarks>
		protected static void DrawHealthCapacity(Rect rect, ref float curY, string leftLabel, string rightLabel, Color rightLabelColour, TipSignal tipSignal)
		{
			Rect rectListing = new Rect(0f, curY, rect.width, Text.LineHeight);

			if (Mouse.IsOver(rectListing))
			{
				using (new TextBlock(new Color(0.5f, 0.5f, 0.5f, 1f)))
				{
					GUI.DrawTexture(rectListing, TexUI.HighlightTex);
				}
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(rectListing, leftLabel);

			GUI.color = rightLabelColour;
			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(rectListing, rightLabel);

			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;

			if (Mouse.IsOver(rectListing))
			{
				TooltipHandler.TipRegion(rectListing, tipSignal);
			}

			curY += rectListing.height;
		}

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			float curY = 0f;

			if (XPP_API.PreviewPawn.def.race.IsFlesh)
			{
				try
				{
					Pair<string, Color> painLabel = HealthCardUtility.GetPainLabel(XPP_API.PreviewPawn);
					string painTip = HealthCardUtility.GetPainTip(XPP_API.PreviewPawn);
					DrawHealthCapacity(
						rect: this.BoundsContents,
						curY: ref curY,
						leftLabel: "PainLevel".Translate(),
						rightLabel: painLabel.First,
						rightLabelColour: painLabel.Second,
						tipSignal: painTip);
				}
				catch (Exception ex)
				{
					Log.Error($"[XPP] Exception whilst drawing pain label.\n{ex}");
					curY = 0f;
				}
			}

			if (!XPP_API.PreviewPawn.Dead)
			{
				List<PawnCapacityDef> capacities = DefDatabase<PawnCapacityDef>.AllDefs
					.Where(x => (XPP_API.PreviewPawn.def.race.Humanlike && x.showOnHumanlikes)
							 || (XPP_API.PreviewPawn.def.race.Animal && x.showOnAnimals)
							 || (XPP_API.PreviewPawn.def.race.IsAnomalyEntity && x.showOnAnomalyEntities)
							 || (XPP_API.PreviewPawn.def.race.IsDrone && x.showOnDrones)
							 || x.showOnMechanoids)
					.OrderBy(x => x.listOrder)
					.ToList();

				foreach (var capacity in capacities)
				{
					float lastY = curY;

					try
					{
						Pair<string, Color> efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(XPP_API.PreviewPawn, capacity);
						DrawHealthCapacity(
							rect: this.BoundsContents,
							curY: ref curY,
							leftLabel: capacity.GetLabelFor(XPP_API.PreviewPawn).CapitalizeFirst(),
							rightLabel: efficiencyLabel.First,
							rightLabelColour: efficiencyLabel.Second,
							tipSignal: HealthCardUtility.GetPawnCapacityTip(XPP_API.PreviewPawn, capacity));
					}
					catch (Exception ex)
					{
						Log.Error($"[XPP] Exception whilst drawing {capacity.label} label.\n{ex}");
						curY = lastY;
					}
				}
			}

			curY += XPP_API.MarginElements;

			Rect hediffRect = new Rect(0f, curY, MinWidth, Text.LineHeight + (XPP_API.PreviewPawn.health.hediffSet.hediffs.Count > 0 ? HediffListingHeight : Text.LineHeight));

			Log.Message($"{XPP_API.PreviewPawn.health.hediffSet.hediffs.Count} {hediffRect.height}");

			try
			{
				HealthCardUtility.DrawHediffListing(hediffRect, XPP_API.PreviewPawn, true);
			}
			catch (Exception ex)
			{
				Log.Error($"[XPP] Exception whilst drawing hediffs.\n{ex}");
			}

			return new Vector2(MinWidth, hediffRect.yMax - Text.LineHeight);
		}
	}
}
