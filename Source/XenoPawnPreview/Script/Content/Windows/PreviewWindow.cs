// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RimWorld;
	using UnityEngine;
	using Verse;
	using Verse.Sound;

	/// <summary>
	/// The preview window for the pawn within the xenotype editor.
	/// </summary>
	public class PreviewWindow : Window
	{
		private const float MarginWindow = 16f;

		private readonly bool originalPawnOnly;

		private readonly List<WindowSection> windowSections = new List<WindowSection>()
		{
			new WindowSection_Controller(),
			new WindowSection_Renderer(),
			new WindowSection_Backstories(),
			new WindowSection_Traits(),
			new WindowSection_Skills(),
			new WindowSection_Abilities(),
			new WindowSection_Needs(),
			new WindowSection_Resources(),
			new WindowSection_Health(),
		};

		private bool refreshRequired;

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewWindow"/> class.
		/// </summary>
		/// <param name="window">The window this preview is attached to.</param>
		public PreviewWindow()
		{
			// Window
			this.closeOnAccept = false;
			this.closeOnCancel = false;
			this.doCloseButton = false;
			this.doCloseX = false;
			this.draggable = XPP_API.Settings.WindowStandalone;
			this.resizeable = false;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PreviewWindow"/> class with the given <paramref name="pawn"/> as the target.
		/// </summary>
		/// <param name="window"><inheritdoc cref="PreviewWindow(GeneCreationDialogBase)" path="/param[@name='window']"/></param>
		/// <param name="pawn">The <see cref="Pawn"/> being used as the original target of this window.</param>
		public PreviewWindow(Pawn pawn)
			: this()
		{
			this.originalPawnOnly = HarmonyPatches_Core.WindowType == CompatibilityUtility.WindowType.WVC_XaG_Generemover;
			XPP_API.BasePawn = pawn;
		}

		/// <summary>
		/// Gets the initial size of the <see cref="Window"/>.
		/// </summary>
		public override Vector2 InitialSize => Vector2.zero;

		/// <summary>
		/// Gets the maximum height of this window, in pixels.
		/// </summary>
		public float MaxHeight { get => Screen.height * XPP_API.Settings.WindowHeightMax; }

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
			// Skip rendering this frame if the window is being destroyed.
			if (XPP_API.PreviewPawn == null)
			{
				return;
			}

			// Set window transforms.
			XPP_API.BaseWindow.windowRect.position = new Vector2((Screen.width / 2f) - (XPP_API.BaseWindow.windowRect.width / 2f), (Screen.height / 2f) - (XPP_API.BaseWindow.windowRect.height / 2f)) + XPP_API.Settings.WindowOffset;

			if (!XPP_API.Settings.WindowStandalone)
			{
				this.windowRect.x = XPP_API.BaseWindow.windowRect.xMax + MarginWindow;
				this.windowRect.y = XPP_API.BaseWindow.windowRect.BottomHalf().y - (this.windowRect.height / 2f);
			}

			this.draggable = XPP_API.Settings.WindowStandalone;

			// Perform update before rendering.
			if (this.refreshRequired)
			{
				for (int i = 0; i < this.windowSections.Count; i++)
				{
					WindowSection curSection = this.windowSections[i];

					try
					{
						curSection.Update();
					}
					catch (Exception ex)
					{
						Log.ErrorOnce($"[XPP] Exception whilst updating {curSection}.\n{ex}", curSection.GetHashCode());
					}
				}
			}

			// Draw window components.
			float curHeight = 0f;
			float curWidth = 0f;
			Rect curSectionRect = Rect.zero;

			for (int i = 0; i < this.windowSections.Count; i++)
			{
				WindowSection curSection = this.windowSections.ElementAt(i);
				WindowSection lastSection = this.windowSections.ElementAtOrDefault(i - 1);

				curSectionRect.size = curSection.Bounds.size;

				if (curSectionRect.yMax + (lastSection?.Bounds.height ?? 0f) >= this.MaxHeight)
				{
					curSectionRect.y = 0f;
					curSectionRect.x += curWidth + XPP_API.MarginElements;
					curWidth = 0f;
				}
				else
				{
					curSectionRect.y += lastSection?.Bounds.height ?? 0f;
				}

				curHeight = Math.Max(curHeight, curSectionRect.yMax);
				curWidth = Math.Max(curWidth, curSectionRect.width);

				Widgets.BeginGroup(curSectionRect);

				try
				{
					curSection.Draw();
				}
				catch (Exception ex)
				{
					Log.Error($"[XPP] Exception whilst drawing '{curSection}'.\n{ex}");
				}

				Widgets.EndGroup(); // curSectionRect
			}

			this.windowRect.size = new Vector2(curSectionRect.x + curWidth, curHeight);
		}

		/// <summary>
		/// Handles execution as the <see cref="Window"/> is closing.
		/// </summary>
		/// <param name="doCloseSound">If <see langword="true"/>, the closing sound will be played when this <see cref="Window"/> is closed.</param>
		public override void Close(bool doCloseSound = true)
		{
			this.PawnDestroy();

			HarmonyPatches_Core.GenesChanged -= this.UpdateGenes;

			base.Close(doCloseSound);
		}

		/// <summary>
		/// Handles execution after the <see cref="Window"/> has just been opened.
		/// </summary>
		public override void PostOpen()
		{
			if (XPP_API.BasePawn == null && !this.PawnGenerate())
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

			HarmonyPatches_Core.GenesChanged += this.UpdateGenes;

			base.PostOpen();
		}

		/// <summary>
		/// Notifies the preview window that the current genes should be re-applied to the target <see cref="Pawn"/>.
		/// </summary>
		public void UpdateGenes() => this.UpdateGenes(XPP_API.BaseWindow.GetSelectedGenes());

		/// <summary>
		/// Notifies the preview window that the <paramref name="genes"/> should be applied to the target <see cref="Pawn"/>.
		/// </summary>
		/// <param name="genes">The genes being applied to the <see cref="Pawn"/>.</param>
		public virtual void UpdateGenes(List<GeneDefWithType> genes)
		{
			if (HarmonyPatches_Core.WindowType == CompatibilityUtility.WindowType.WVC_XaG_Generemover)
			{
				// Preview must always start with the genes of the original.
				if (XPP_API.PreviewPawn.genes.GenesListForReading.Count == 0)
				{
					foreach (var gene in XPP_API.BasePawn.genes.GetGeneDefs())
					{
						XPP_API.PreviewPawn.genes.AddGene(gene.geneDef, gene.isXenogene);
					}
				}

				// Add genes which are on the original, but not the preview or selected.
				foreach (var newGene in XPP_API.BasePawn.genes.GetGeneDefs().Except(XPP_API.PreviewPawn.genes.GetGeneDefs().Concat(genes)))
				{
					XPP_API.PreviewPawn.genes.AddGene(newGene.geneDef, newGene.isXenogene);
				}

				// Remove genes which are selected.
				foreach (var oldGene in XPP_API.PreviewPawn.genes.GenesListForReading.Where(x => genes.Select(y => y.geneDef).Contains(x.def)))
				{
					XPP_API.PreviewPawn.genes.RemoveGene(oldGene);
					XPP_API.PreviewPawn.story.traits.Notify_GeneRemoved(oldGene);
				}
			}
			else
			{
				// Remove genes which are on the preview, but not selected.
				foreach (var oldGene in XPP_API.PreviewPawn.genes.GenesListForReading.Where(x => XPP_API.PreviewPawn.genes.GetGeneDefs().Except(genes).Select(y => y.geneDef).Contains(x.def)))
				{
					XPP_API.PreviewPawn.genes.RemoveGene(oldGene);
					XPP_API.PreviewPawn.story.traits.Notify_GeneRemoved(oldGene);
				}

				// Add genes which are selected, but not on the preview.
				foreach (var newGene in genes.Except(XPP_API.PreviewPawn.genes.GetGeneDefs()))
				{
					XPP_API.PreviewPawn.genes.AddGene(newGene.geneDef, newGene.isXenogene);
				}
			}

			this.refreshRequired = true;
		}

		/// <summary>
		/// Clears all information about the currently rendered pawn.
		/// </summary>
		public virtual void PawnClear()
		{
			XPP_API.PreviewPawn.ClearData();
			this.UpdateGenes();

			this.refreshRequired = true;
		}

		/// <summary>
		/// Destroys and cleans up the currently active <see cref="Pawn"/>.
		/// </summary>
		public virtual void PawnDestroy()
		{
			if (XPP_API.PreviewPawn != null)
			{
				if (XPP_API.PreviewPawn.Spawned)
				{
					XPP_API.PreviewPawn.DeSpawn();
				}
				else
				{
					XPP_API.PreviewPawn.Destroy();
				}

				Find.World?.worldPawns.RemoveAndDiscardPawnViaGC(XPP_API.PreviewPawn);
			}

			XPP_API.PreviewPawn = null;

			this.refreshRequired = true;
		}

		/// <summary>
		/// Attempts to generate a new <see cref="Pawn"/> and apply it as the target of this <see cref="PreviewWindow"/>.
		/// </summary>
		/// <returns><see langword="true"/> if a new <see cref="Pawn"/> was successfully generated.</returns>
		public virtual bool PawnGenerate()
		{
			if (XPP_API.Settings.PawnGenerateMinimal)
			{
				try
				{
					XPP_API.PreviewPawn = PawnUtility.GenerateMinimalPawn();

					return true;
				}
				catch (Exception ex)
				{
					Log.Error($"[XPP] Exception whilst trying to generate a minimal pawn. Trying again with a regular pawn.\n{ex}");
				}
			}

			if (this.originalPawnOnly)
			{
				return this.PawnRegenerate();
			}

			this.PawnDestroy();

			try
			{
				HarmonyPatches_Core.PrepareGeneration = true;
				XPP_API.PreviewPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);

				XPP_API.PreviewPawn.apparel = new Pawn_ApparelTracker(XPP_API.PreviewPawn);
				XPP_API.PreviewPawn.ideo = new Pawn_IdeoTracker(XPP_API.PreviewPawn);
			}
			catch (Exception ex)
			{
				Log.Error($"[XPP] Exception whilst generating a new pawn. Generating a minimal pawn instead.\n{ex}");

				XPP_API.PreviewPawn = PawnUtility.GenerateMinimalPawn();
			}

			this.UpdateGenes();
			this.refreshRequired = true;

			return XPP_API.PreviewPawn != null;
		}

		/// <summary>
		/// Attempts to regenerate the <see cref="XPP_API.BasePawn"/> and apply it as the target of the preview window.
		/// </summary>
		/// <param name="request">The optional request to use for a pawn.</param>
		/// <returns><see langword="true"/> if a new <see cref="Pawn"/> was successfully generated.</returns>
		public virtual bool PawnRegenerate()
		{
			this.PawnDestroy();

			if (XPP_API.BasePawn == null)
			{
				this.PawnGenerate();
			}
			else
			{
				try
				{
					XPP_API.PreviewPawn = Find.PawnDuplicator.Duplicate(XPP_API.BasePawn.PrepareSafely());

					XPP_API.PreviewPawn.ideo = new Pawn_IdeoTracker(XPP_API.PreviewPawn);
				}
				catch (Exception ex)
				{
					Log.Error($"[XPP] Exception whilst duplicating {XPP_API.BasePawn}. Generating a minimal pawn instead.\n{ex}");

					XPP_API.PreviewPawn = PawnUtility.GenerateMinimalPawn();
				}
			}

			this.UpdateGenes();
			this.refreshRequired = true;

			return XPP_API.PreviewPawn != null;
		}

		/// <summary>
		/// Requests a refresh of the window.
		/// </summary>
		public void RequestRefresh() => this.refreshRequired = true;
	}
}