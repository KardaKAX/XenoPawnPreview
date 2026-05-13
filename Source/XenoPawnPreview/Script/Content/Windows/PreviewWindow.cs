// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Data.SqlTypes;
	using System.Linq;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// The preview window for the pawn within the xenotype editor.
	/// </summary>
	public class PreviewWindow : Window
	{
		private const float ColumnWidth = 256f;

		private const float MarginSmall = 6f;

		private const float MarginMedium = 8f;

		private const float MarginLarge = 16f;

		private const float MarginKeyValues = 90f;

		private const float MarginSectionTitle = 24f;

		private const float PawnPreviewSize = 256f;

		private readonly GeneCreationDialogBase baseWindow;

		private readonly Rect rectSettings = new Rect(
			x: 0f,
			y: 0f,
			width: ColumnWidth,
			height: WindowButtonSmall.Size.y + (MarginSmall * 2f));

		private readonly RenderTexture texPawnRender = new RenderTexture((int)PawnPreviewSize, (int)PawnPreviewSize, 32, RenderTextureFormat.ARGB32);

		private readonly WindowButtonSmall buttonSettingsClear = new WindowButtonSmall(
			icon: ContentFinder<Texture2D>.Get("XPP/UI/Icons/PawnRecycle") ?? BaseContent.BadTex,
			tooltip: "Karda.XPP.Controller.Pawn.Button.Recycle".Translate());

		private readonly WindowButtonSmall buttonSettingsInfo = new WindowButtonSmall(
			icon: TexButton.Info ?? BaseContent.BadTex,
			tooltip: "Karda.XPP.Controller.Pawn.Button.Infocard".Translate());

		private readonly WindowButtonSmall buttonSettingsNew = new WindowButtonSmall(
			icon: ContentFinder<Texture2D>.Get("XPP/UI/Icons/PawnNew") ?? BaseContent.BadTex,
			tooltip: "Karda.XPP.Controller.Pawn.Button.New".Translate());

		private readonly WindowButtonSmall buttonSettingsSettings = new WindowButtonSmall(
			icon: ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral") ?? BaseContent.BadTex,
			tooltip: "Karda.XPP.Controller.Settings.Button".Translate());

		private readonly WindowButtonSmall buttonRenderRotate = new WindowButtonSmall(
			icon: ContentFinder<Texture2D>.Get("UI/Icons/SwitchFaction") ?? BaseContent.BadTex,
			tooltip: "Karda.XPP.Render.Rotate.Button.Tooltip".Translate());

		private float needsLabelMaxX;

		private Pawn pawn;

		private Rect rectAbilities = new Rect(
			x: MarginMedium,
			y: default,
			width: ColumnWidth - MarginMedium,
			height: MarginSectionTitle);

		private Rect rectBackstories = new Rect(
			x: MarginMedium,
			y: default,
			width: ColumnWidth - MarginMedium,
			height: MarginSectionTitle);

		private Rect rectHealth = new Rect(Rect.zero);

		private Rect rectNeeds = new Rect(
			x: ColumnWidth,
			y: MarginSmall,
			width: ColumnWidth,
			height: default);

		private Rect rectRender = new Rect(Rect.zero);

		private Rect rectSkills = new Rect(
			x: MarginMedium,
			y: default,
			width: ColumnWidth - MarginMedium,
			height: default);

		private Rect rectTraits = new Rect(
			x: MarginMedium,
			y: default,
			width: ColumnWidth - MarginMedium,
			height: default);

		private Rot4 renderAngle = Rot4.South;

		private float renderZoom = 1f;

		private bool refreshRequired;

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewWindow"/> class.
		/// </summary>
		/// <param name="targetWindow">The window this preview is attached to.</param>
		public PreviewWindow(GeneCreationDialogBase targetWindow)
		{
			// Window
			this.closeOnAccept = false;
			this.closeOnCancel = false;
			this.doCloseButton = false;
			this.doCloseX = false;
			this.draggable = XPP_Mod.ModSettings.WindowStandalone;
			this.resizeable = false;

			// PreviewWindow
			this.baseWindow = targetWindow;
			this.buttonRenderRotate.Callback = () => this.RotateRender();
			this.buttonSettingsSettings.Callback = () => Find.WindowStack.Add(new Dialog_ModSettings(XPP_Mod.ModSettings.Mod));
			this.buttonSettingsClear.Callback = () => this.PawnClear();
			this.buttonSettingsNew.Callback = () => this.PawnGenerate();
			this.buttonSettingsInfo.Callback = () => Find.WindowStack.Add(new Dialog_InfoCard(this.pawn));
		}

		/// <summary>
		/// Gets the initial size of the <see cref="Window"/>.
		/// </summary>
		public override Vector2 InitialSize => Vector2.zero;

		/// <summary>
		/// Gets the pixel margin to the edge of the screen.
		/// </summary>
		protected override float Margin => 0f;

		/// <summary>
		/// Draws the contents of the <see cref="Window"/>.
		/// </summary>
		/// <param name="inRect">The bounds of the <see cref="Window"/>.</param>
		public override void DoWindowContents(Rect inRect)
		{
			// Set window transform.
			if (!XPP_Mod.ModSettings.WindowStandalone)
			{
				this.windowRect.x = this.baseWindow.windowRect.xMax + MarginLarge;
				this.windowRect.y = this.baseWindow.windowRect.BottomHalf().y - (this.windowRect.height / 2f);
			}

			this.windowRect.size = new Vector2(
				x: ColumnWidth * 2f,
				y: this.rectAbilities.yMax - this.rectSettings.yMin);

			this.draggable = XPP_Mod.ModSettings.WindowStandalone;

			// Perform update before rendering.
			if (this.refreshRequired)
			{
				this.RefreshWindow();
			}

			// Draw window components.
			this.DrawSettings();
			this.DrawPawnRender();
			this.DrawPawnBackstories();
			this.DrawPawnTraits();
			this.DrawPawnSkills();
			this.DrawPawnAbilities();
			this.DrawPawnNeeds();
			this.DrawPawnHealth();
		}

		/// <summary>
		/// Handles execution as the <see cref="Window"/> is closing.
		/// </summary>
		/// <param name="doCloseSound">If <see langword="true"/>, the closing sound will be played when this <see cref="Window"/> is closed.</param>
		public override void Close(bool doCloseSound = true)
		{
			this.pawn?.Destroy();

			HarmonyPatches_Core.GenesChanged -= this.OnGenesChanged;

			base.Close(doCloseSound);
		}

		/// <summary>
		/// Handles execution after the <see cref="Window"/> has just been opened.
		/// </summary>
		public override void PostOpen()
		{
			if (HarmonyPatches_Core.OriginalPawn == null && !this.PawnGenerate())
			{
				Log.Error($"XPP: Failed to generate a new pawn, closing preview window.");
				this.Close(false);
			}
			else if (!this.PawnRegenerate())
			{
				Log.Warning($"XPP: Failed to regenerate targeted pawn, trying with new pawn...");

				if (!this.PawnGenerate())
				{
					Log.Error($"XPP: Failed to generate a fallback pawn, closing preview window.");
					this.Close(false);
				}
			}

			this.refreshRequired = true;

			HarmonyPatches_Core.GenesChanged += this.OnGenesChanged;

			base.PostOpen();
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
			Rect rectListing = new Rect(MarginSmall, curY, rect.width - (MarginSmall * 2f), MarginSectionTitle);

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

		/// <summary>
		/// Draws the pawn abilities segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnAbilities()
		{
			this.rectAbilities.y = this.rectSkills.yMax;
			this.rectAbilities.height = MarginSectionTitle;

			Color tmpColour = GUI.color;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectAbilities, "Abilities".Translate());
			GUI.color = tmpColour;

			this.rectAbilities.yMin = this.rectSkills.yMax + MarginSectionTitle;
			this.rectAbilities.height = WindowButtonSmall.Size.y;

			Rect result = GenUI.DrawElementStack(
				rect: this.rectAbilities,
				rowHeight: WindowButtonSmall.Size.y,
				elements: this.pawn.abilities.abilities,
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
				rowMargin: MarginSmall,
				elementMargin: MarginSmall,
				allowOrderOptimization: true);

			this.rectAbilities.height = result.height + MarginSmall;
		}

		/// <summary>
		/// Draws the pawn backstory segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnBackstories()
		{
			this.rectBackstories.y = this.rectRender.yMax + MarginSmall;
			this.rectBackstories.height = MarginSectionTitle;

			Color tmpColour = GUI.color;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectBackstories, "Backstory".Translate());
			GUI.color = tmpColour;

			this.rectBackstories.y += MarginSectionTitle;

			Widgets.Label(this.rectBackstories, this.pawn.def.LabelCap.ToString());

			if (Mouse.IsOver(this.rectBackstories))
			{
				TooltipHandler.TipRegion(this.rectBackstories, this.pawn.def.description);
			}

			this.rectBackstories.y += MarginSectionTitle;

			Widgets.Label(this.rectBackstories, this.pawn.MainDesc(false, true));

			foreach (BackstorySlot bsSlot in Enum.GetValues(typeof(BackstorySlot)))
			{
				this.rectBackstories.y += MarginSectionTitle + MarginSmall;

				BackstoryDef bsDef = this.pawn.story.GetBackstory(bsSlot);

				if (bsDef == null)
				{
					continue;
				}

				string bsValueText = bsDef.TitleCapFor(this.pawn.gender);
				Rect bsValueRect = new Rect(this.rectBackstories)
				{
					x = MarginKeyValues,
					width = Text.CalcSize(bsValueText).x + MarginMedium,
				};

				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(this.rectBackstories, bsDef.slot.ToString().Translate());

				GUI.color = CharacterCardUtility.StackElementBackground;
				GUI.DrawTexture(bsValueRect, BaseContent.WhiteTex);
				GUI.color = tmpColour;

				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(bsValueRect, bsValueText.Truncate(bsValueRect.width));
				Text.Anchor = TextAnchor.UpperLeft;

				if (Mouse.IsOver(bsValueRect))
				{
					Widgets.DrawHighlight(bsValueRect);
					TooltipHandler.TipRegion(bsValueRect, bsDef.FullDescriptionFor(this.pawn).Resolve());
				}
			}

			this.rectBackstories.yMax += MarginSmall;
		}

		/// <summary>
		/// Draws the pawn health segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnHealth()
		{
			Color tmpColour = GUI.color;
			float curY = 0f;

			this.rectHealth.x = ColumnWidth;
			this.rectHealth.y = Mathf.Max(this.rectRender.yMax + MarginSmall, this.rectNeeds.yMax);
			this.rectHealth.width = ColumnWidth;
			this.rectHealth.yMax = this.rectAbilities.yMax;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectHealth, "Health".Translate());
			GUI.color = tmpColour;

			this.rectHealth.yMin += MarginSectionTitle;

			Widgets.BeginGroup(this.rectHealth);

			// Capacities
			if (this.pawn.def.race.IsFlesh)
			{
				Pair<string, Color> painLabel = HealthCardUtility.GetPainLabel(this.pawn);
				string painTip = HealthCardUtility.GetPainTip(this.pawn);
				DrawHealthCapacity(
					rect: this.rectHealth,
					curY: ref curY,
					leftLabel: "PainLevel".Translate(),
					rightLabel: painLabel.First,
					rightLabelColour: painLabel.Second,
					tipSignal: painTip);
			}

			if (!this.pawn.Dead)
			{
				List<PawnCapacityDef> capacities = DefDatabase<PawnCapacityDef>.AllDefs
					.Where(x => (this.pawn.def.race.Humanlike && x.showOnHumanlikes)
							 || (this.pawn.def.race.Animal && x.showOnAnimals)
							 || (this.pawn.def.race.IsAnomalyEntity && x.showOnAnomalyEntities)
							 || (this.pawn.def.race.IsDrone && x.showOnDrones)
							 || x.showOnMechanoids)
					.OrderBy(x => x.listOrder)
					.ToList();

				foreach (var capacity in capacities)
				{
					Pair<string, Color> efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(this.pawn, capacity);
					DrawHealthCapacity(
						rect: this.rectHealth,
						curY: ref curY,
						leftLabel: capacity.GetLabelFor(this.pawn).CapitalizeFirst(),
						rightLabel: efficiencyLabel.First,
						rightLabelColour: efficiencyLabel.Second,
						tipSignal: HealthCardUtility.GetPawnCapacityTip(this.pawn, capacity));
				}
			}

			Widgets.EndGroup(); // this.rectHealth

			// Hediffs
			this.rectHealth.xMin += MarginSmall;
			this.rectHealth.xMax -= MarginSmall;
			this.rectHealth.yMin += curY + MarginSectionTitle;
			HealthCardUtility.DrawHediffListing(this.rectHealth, this.pawn, true);
		}

		/// <summary>
		/// Draws the pawn needs segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnNeeds()
		{
			Color tmpColor = GUI.color;
			float tmpX = this.rectNeeds.x;
			float tmpWidth = this.rectNeeds.width;

			this.rectNeeds.y = MarginSmall;
			this.rectNeeds.height = MarginSectionTitle;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectNeeds, "TabNeeds".Translate());
			GUI.color = tmpColor;

			this.rectNeeds.y += MarginSectionTitle + MarginSmall;

			foreach (var need in this.pawn.needs.AllNeeds.Where(x => x.ShowOnNeedList || x is Need_Mood))
			{
				this.rectNeeds.xMin += MarginSmall;

				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(this.rectNeeds, need.LabelCap);
				Text.Anchor = TextAnchor.UpperLeft;

				this.needsLabelMaxX = Mathf.Max(this.needsLabelMaxX, Text.CalcSize(need.LabelCap).x + MarginSmall);
				this.rectNeeds.xMin += this.needsLabelMaxX;
				this.rectNeeds.xMax -= MarginSmall;

				need.DrawOnGUI(rect: this.rectNeeds, customMargin: 0f, drawArrows: false, drawLabel: false);

				this.rectNeeds.x = tmpX;
				this.rectNeeds.y += MarginSectionTitle;
				this.rectNeeds.width = tmpWidth;
				this.rectNeeds.height = MarginSectionTitle;
			}
		}

		/// <summary>
		/// Draws the pawn rendering segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnRender()
		{
			this.rectRender.x = 0f;
			this.rectRender.y = this.rectSettings.yMax;
			this.rectRender.width = PawnPreviewSize;
			this.rectRender.height = PawnPreviewSize;

			GUI.DrawTexture(this.rectRender, this.texPawnRender);

			this.rectRender.xMin = MarginSmall;
			this.rectRender.xMax = ColumnWidth - MarginSmall;
			this.rectRender.yMin = this.rectRender.yMax - WindowButtonSmall.Size.y;

			this.buttonRenderRotate.Bounds.x = this.rectRender.x;
			this.buttonRenderRotate.Bounds.y = this.rectRender.y;
			this.buttonRenderRotate.DrawComponent();

			this.rectRender.xMin += WindowButtonSmall.Size.x + MarginSmall;

			if (Mouse.IsOver(this.rectRender))
			{
				TooltipHandler.TipRegion(this.rectRender, "Karda.XPP.Render.Zoom.Slider.Tooltip".Translate());
			}

			float newZoom = Widgets.HorizontalSlider(this.rectRender, this.renderZoom, 0.1f, 2f, label: $"{"Karda.XPP.Render.Zoom.Slider.Label".Translate()}: {this.renderZoom:F1}x", roundTo: 0.1f);

			if (newZoom != this.renderZoom)
			{
				this.renderZoom = newZoom;
				this.refreshRequired = true;
			}
		}

		/// <summary>
		/// Draws the pawn skills segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnSkills()
		{
			Color tmpColour = GUI.color;

			this.rectSkills.y = this.rectTraits.yMax;
			this.rectSkills.height = (MarginSectionTitle + MarginSmall) * DefDatabase<SkillDef>.AllDefsListForReading.Count;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectSkills, "Skills".Translate());
			GUI.color = tmpColour;

			this.rectSkills.yMin = this.rectTraits.yMax + MarginSectionTitle + MarginSmall;

			SkillUI.DrawSkillsOf(this.pawn, this.rectSkills.position, SkillUI.SkillDrawMode.Menu, this.rectSkills);
		}

		/// <summary>
		/// Draws the pawn traits segment of the preview window.
		/// </summary>
		protected virtual void DrawPawnTraits()
		{
			Color tmpColour = GUI.color;

			this.rectTraits.y = this.rectBackstories.yMax;

			GUI.color = Color.yellow;
			Widgets.Label(this.rectTraits, "Traits".Translate());
			GUI.color = tmpColour;

			this.rectTraits.yMin = this.rectBackstories.yMax + MarginSectionTitle;

			Rect rectTraitsStack = GenUI.DrawElementStack(
				rect: this.rectTraits,
				rowHeight: MarginSectionTitle,
				elements: this.pawn.story.traits.TraitsSorted,
				drawer: (r, t) =>
				{
					GUI.color = CharacterCardUtility.StackElementBackground;
					GUI.DrawTexture(r, BaseContent.WhiteTex);
					GUI.color = tmpColour;

					if (Mouse.IsOver(r))
					{
						Widgets.DrawHighlight(r);
					}

					if (t.Suppressed)
					{
						GUI.color = ColoredText.SubtleGrayColor;
					}
					else if (t.sourceGene != null)
					{
						GUI.color = ColoredText.GeneColor;
					}

					Widgets.Label(new Rect(r.x + MarginSmall, r.y, r.width + (MarginSmall * 2f), r.height), t.LabelCap);
					GUI.color = tmpColour;

					if (Mouse.IsOver(r))
					{
						TooltipHandler.TipRegion(r, t.TipString(this.pawn));
					}
				},
				widthGetter: x => Text.CalcSize(x.LabelCap).x + (MarginSmall * 2f),
				allowOrderOptimization: false);

			this.rectTraits.height = rectTraitsStack.height + (MarginSmall * 2f);
		}

		/// <summary>
		/// Draws the settings button segment of the preview window.
		/// </summary>
		protected virtual void DrawSettings()
		{
			GenUI.DrawElementStack(
				rect: this.rectSettings.ContractedBy(MarginSmall),
				rowHeight: WindowButtonSmall.Size.y,
				elements: new List<WindowButtonSmall>()
				{
					this.buttonSettingsSettings,
					this.buttonSettingsNew,
					this.buttonSettingsClear,
					this.buttonSettingsInfo,
				},
				drawer: (background, element) =>
				{
					element.Bounds = background;
					element.DrawComponent();
				},
				widthGetter: x => WindowButtonSmall.Size.x,
				rowMargin: MarginSmall,
				elementMargin: MarginSmall,
				allowOrderOptimization: false);
		}

		/// <summary>
		/// Clears all information about the currently rendered pawn.
		/// </summary>
		protected virtual void PawnClear()
		{
			this.pawn.ClearData();

			this.refreshRequired = true;
		}

		/// <summary>
		/// Attempts to generate a new <see cref="Pawn"/> and apply it as the target of this <see cref="PreviewWindow"/>.
		/// </summary>
		/// <returns><see langword="true"/> if a new <see cref="Pawn"/> was successfully generated.</returns>
		protected virtual bool PawnGenerate()
		{
			this.pawn?.Destroy();
			this.pawn = null;

			if (CompatibilityUtility.BigAndSmall_Assembly != null && Find.UIRoot is UIRoot_Entry)
			{
				this.pawn = PawnUtility.GenerateMinimalPawn();
			}
			else
			{
				this.pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);

				this.pawn.apparel = new Pawn_ApparelTracker(this.pawn);
				this.pawn.ideo = new Pawn_IdeoTracker(this.pawn);
			}

			HarmonyPatches_Core.GeneCreationDialogBase_OnGenesChanged(this.baseWindow);
			this.refreshRequired = true;

			return this.pawn != null;
		}

		/// <summary>
		/// Attempts to regenerate the <see cref="HarmonyPatches_Core.OriginalPawn"/> and apply it as the target of the preview window.
		/// </summary>
		/// <param name="request">The optional request to use for a pawn.</param>
		/// <returns><see langword="true"/> if a new <see cref="Pawn"/> was successfully generated.</returns>
		protected virtual bool PawnRegenerate()
		{
			this.pawn?.Destroy();
			this.pawn = null;

			if (HarmonyPatches_Core.OriginalPawn == null)
			{
				this.PawnGenerate();
			}
			else if (CompatibilityUtility.BigAndSmall_Assembly != null && Find.UIRoot is UIRoot_Entry)
			{
				this.pawn = PawnUtility.GenerateMinimalPawn();
			}
			else
			{
				this.pawn = Find.PawnDuplicator.Duplicate(HarmonyPatches_Core.OriginalPawn);

				this.pawn.ideo = new Pawn_IdeoTracker(this.pawn);
			}

			HarmonyPatches_Core.GeneCreationDialogBase_OnGenesChanged(this.baseWindow);
			this.refreshRequired = true;

			return this.pawn != null;
		}

		/// <summary>
		/// Handles invocations of the <see cref="HarmonyPatches_Core.GenesChanged"/> event.
		/// </summary>
		/// <param name="newGenes">The genes being applied to the <see cref="Pawn"/>.</param>
		protected virtual void OnGenesChanged(List<GeneDefWithType> newGenes)
		{
			foreach (var oldGene in this.pawn.genes.GenesListForReading)
			{
				this.pawn.genes.RemoveGene(oldGene);
				this.pawn.story.traits.Notify_GeneRemoved(oldGene);
			}

			foreach (var newGene in newGenes)
			{
				this.pawn.genes.AddGene(newGene.geneDef, newGene.isXenogene);
			}

			this.refreshRequired = true;
		}

		/// <summary>
		/// Refreshes the window with the new state of the pawn.
		/// </summary>
		protected virtual void RefreshWindow()
		{
			// Render
			this.pawn.Drawer.renderer.EnsureGraphicsInitialized();
			this.pawn.Drawer.renderer.SetAllGraphicsDirty();
			PawnCacheCameraManager.PawnCacheRenderer.RenderPawn(this.pawn, this.texPawnRender, Vector3.zero, this.renderZoom, 0f, this.renderAngle);

			// Skills
			this.rectSkills.height = MarginLarge + (this.pawn.skills.skills.Count * (SkillUI.SkillHeight + SkillUI.SkillYSpacing));

			// Needs
			this.pawn.needs.AddOrRemoveNeedsAsAppropriate();

			// Health
			foreach (var hediff in this.pawn.health.hediffSet.hediffs)
			{
				this.pawn.health.Notify_HediffChanged(hediff);
			}

			this.refreshRequired = false;
		}

		/// <summary>
		/// Rotates the currently rendered pawn.
		/// </summary>
		protected virtual void RotateRender()
		{
			switch (this.renderAngle.AsInt)
			{
				// South -> West
				case 0:
					this.renderAngle = Rot4.West;
					break;

				// West -> North
				case 1:
					this.renderAngle = Rot4.North;
					break;

				// North -> East
				case 2:
					this.renderAngle = Rot4.East;
					break;

				// East / Other -> South
				default:
					this.renderAngle = Rot4.South;
					break;
			}

			this.refreshRequired = true;
		}
	}
}
