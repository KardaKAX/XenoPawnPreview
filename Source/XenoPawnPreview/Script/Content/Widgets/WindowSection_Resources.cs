// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using RimWorld;
	using Unity.Collections.LowLevel.Unsafe;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about skills of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Resources : WindowSection
	{
		private const float ResourceBorderSize = 4f;

		private const float ResourceThresholdWidth = 2f;

		private List<Gene_Resource> resourceGenes;

		private float resourceLabelMaxX;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Resources"/> class.
		/// </summary>
		public WindowSection_Resources()
			: base()
		{
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override string Title => "Karda.XPP.Resources.Label".Translate();

		/// <inheritdoc/>
		public override void Update()
		{
			this.resourceGenes = XPP_API.PreviewPawn.genes.GenesListForReading
				.OfType<Gene_Resource>()
				.OrderBy(x => x.def.displayOrderInCategory)
				.ToList();
			this.resourceLabelMaxX = this.resourceGenes.Count > 0 ? MathUtility.Max(this.resourceGenes.Select(x => Text.CalcSize(x.ResourceLabel).x)) : 0f;
		}

		/// <summary>
		/// Gets the genes which contribute to resource drain through harmony reflection.
		/// </summary>
		/// <param name="gene">The gene to gather the sources for.</param>
		/// <returns>All genes which contribute to the <paramref name="gene"/> resource drain.</returns>
		protected static List<IGeneResourceDrain> GetDrainSources(Gene_Resource gene) => AccessTools.MethodDelegate<Func<Gene_Resource, List<IGeneResourceDrain>>>(AccessTools.PropertyGetter(typeof(Gene_Resource), "DrainGenes"))(gene);

		/// <summary>
		/// Gets the colour of the resource for the specified gene through harmony reflection.
		/// </summary>
		/// <param name="gene">The gene to get the colour for.</param>
		/// <returns>The colour of the resource.</returns>
		protected static Color GetResourceColour(Gene_Resource gene) => AccessTools.MethodDelegate<Func<Gene_Resource, Color>>(AccessTools.PropertyGetter(typeof(Gene_Resource), "BarColor"))(gene);

		/// <summary>
		/// Draws an individual resource listing using a specified <see cref="Gene_Resource"/>.
		/// </summary>
		/// <param name="curY">The current Y value to place this entry at in the list.</param>
		/// <param name="gene">The resource to read information from.</param>
		protected virtual void DrawResourceRow_Gene(ref float curY, Gene_Resource gene)
		{
			Rect boundsRow = new Rect(0f, curY, MinWidth, Text.LineHeight);

			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(boundsRow, gene.def.resourceLabel.CapitalizeFirst());
			Text.Anchor = this.OldAnchor;

			Rect boundsBar = new Rect(boundsRow) { xMin = Math.Max(this.resourceLabelMaxX, MinWidth / 3f) + (XPP_API.MarginElements * 2f) };

			GUI.color = Color.black;
			GUI.DrawTexture(boundsBar, BaseContent.WhiteTex);

			boundsBar = boundsBar.ContractedBy(ResourceBorderSize);

			Rect boundsBarFill = new Rect(boundsBar);
			Color resourceColour = GetResourceColour(gene);

			boundsBarFill.width *= gene.Value / gene.Max;

			GUI.color = resourceColour;
			GUI.DrawTexture(boundsBarFill, BaseContent.WhiteTex);

			Widgets.BeginGroup(boundsBar);

			foreach (float thresholdPerc in gene.def.resourceGizmoThresholds)
			{
				GUI.color = new Color(1f, 1f, 1f, 0.5f);
				GUI.DrawTexture(
					position: new Rect(
						x: (boundsBar.width * thresholdPerc) - (ResourceThresholdWidth / 2f),
						y: boundsBar.height / 2f,
						width: ResourceThresholdWidth,
						height: boundsBarFill.height / 2f),
					image: thresholdPerc > (gene.Value / gene.Max) ? BaseContent.GreyTex : BaseContent.BlackTex);
			}

			Widgets.EndGroup();

			float resourceDrain = GetDrainSources(gene).Sum(x => x.ResourceLossPerDay) * -100f;
			Rect boundsBarText = new Rect(boundsBar.ExpandedBy(ResourceBorderSize)) { xMin = boundsBar.xMin + ResourceBorderSize, xMax = boundsBar.xMax - ResourceBorderSize };

			if (!XPP_API.Settings.ResourceValuesCompact)
			{
				boundsRow.yMax += Text.LineHeight * 0.75f;
				boundsBarText.yMin = boundsBar.yMax + ResourceBorderSize;
				boundsBarText.yMax = boundsRow.yMax;
			}

			Text.Font = GameFont.Tiny;
			GUI.color = XPP_API.Settings.ResourceValuesCompact ? (resourceColour.grayscale > 0.9f ? Color.black : Color.white) : Color.white;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(boundsBarText, $"{gene.ValueForDisplay}/{gene.MaxForDisplay}");

			Text.Anchor = TextAnchor.MiddleRight;
			Widgets.Label(boundsBarText, $"({(resourceDrain > 0f ? "+" : string.Empty)}{Math.Round(resourceDrain)})");

			Text.Font = GameFont.Small;
			Text.Anchor = this.OldAnchor;
			GUI.color = this.OldColour;

			if (Mouse.IsOver(boundsRow))
			{
				Widgets.DrawHighlight(boundsRow);
				TooltipHandler.TipRegion(boundsRow, gene.def.resourceDescription.Formatted(XPP_API.PreviewPawn.Named("PAWN")).Resolve());
			}

			curY += boundsRow.height + XPP_API.MarginElements;
		}

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			float curY = 0f;

			// Psycasts, Mechanators etc. - Display in list when hediff is active, show linked statworkers as values.

			foreach (var resource in this.resourceGenes)
			{
				this.DrawResourceRow_Gene(ref curY, resource);
			}

			return new Vector2(MinWidth, curY);
		}
	}
}
