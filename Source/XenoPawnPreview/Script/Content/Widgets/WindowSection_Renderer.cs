// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System.Collections.Generic;
	using RimWorld;
	using UnityEngine;
	using Verse;
	using Verse.Sound;

	/// <summary>
	/// Contains the preview texture of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Renderer : WindowSection
	{
		/// <summary>
		/// The size of the preview render, in pixels.
		/// </summary>
		public const float RenderSize = 256f;

		private readonly WindowButtonSmall buttonRotate;

		private readonly WindowButtonSmall buttonVox;

		private readonly RenderTexture pawnRenderTexture = new RenderTexture((int)RenderSize, (int)RenderSize, 32, RenderTextureFormat.ARGB32);

		private readonly Rect rectRender = new Rect(0f, 0f, RenderSize, RenderSize);

		private readonly Rect rectZoom;

		private readonly List<FloatMenuOption> soundTypeOptions;

		private Rot4 renderAngle = Rot4.South;

		private float renderZoom = 1f;

		private int soundTypeIndex;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Renderer"/> class.
		/// </summary>
		public WindowSection_Renderer()
			: base()
		{
			this.soundTypeOptions = new List<FloatMenuOption>()
			{
				new FloatMenuOption("Karda.XPP.Render.Sound.Type.Call".Translate(), () => this.soundTypeIndex = 0),
				new FloatMenuOption("Karda.XPP.Render.Sound.Type.Wounded".Translate(), () => this.soundTypeIndex = 1),
				new FloatMenuOption("Karda.XPP.Render.Sound.Type.Death".Translate(), () => this.soundTypeIndex = 2),
			};

			this.buttonRotate = new WindowButtonSmall(
				position: new Vector2(0f, RenderSize),
				icon: ContentFinder<Texture2D>.Get("UI/Icons/SwitchFaction") ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Render.Rotate.Button".Translate(),
				callback: () => this.RotateRender());

			this.buttonVox = new WindowButtonSmall(
				position: new Vector2(WindowButtonSmall.Size.x, RenderSize),
				icon: ContentFinder<Texture2D>.Get("UI/Buttons/PreviewSound_NotPlaying") ?? BaseContent.BadTex,
				callback: () => this.PlayCurrentVoice())
			{
				Highlighted = (state) =>
				{
					if (state)
					{
						this.buttonVox.Tooltip =
						$"{string.Format("Karda.XPP.Render.Sound.Play.Button".Translate(), this.soundTypeOptions[this.soundTypeIndex].Label.Colorize(ColoredText.NameColor))}\n\n{"Karda.XPP.Render.Sound.Play.Button.2".Translate()}";
					}
				},
				Options = this.soundTypeOptions,
			};

			this.rectZoom = new Rect(
				x: this.buttonVox.Bounds.xMax,
				y: RenderSize,
				width: RenderSize - this.buttonVox.Bounds.xMax,
				height: WindowButtonSmall.Size.y);
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(RenderSize, RenderSize + WindowButtonSmall.Size.y);

		/// <summary>
		/// Plays the lifestage sound depending on the current <see cref="soundTypeIndex"/>.
		/// </summary>
		public void PlayCurrentVoice() => this.PlayCurrentVoice(this.soundTypeIndex);

		/// <summary>
		/// Plays the lifestage sound depending on the current <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The index of the sound to play.</param>
		public virtual void PlayCurrentVoice(int index)
		{
			SoundDef targetSound;
			LifeStageAge curLSA = XPP_API.PreviewPawn.RaceProps.lifeStageAges[XPP_API.PreviewPawn.ageTracker.CurLifeStageIndex];

			switch (index)
			{
				case 1:
					targetSound = XPP_API.PreviewPawn.mutant?.Def.soundWounded ?? XPP_API.PreviewPawn.genes.GetSoundOverrideFromGenes(x => x.soundWounded, curLSA.soundWounded);
					break;

				case 2:
					targetSound = XPP_API.PreviewPawn.mutant?.Def.soundDeath ?? XPP_API.PreviewPawn.genes.GetSoundOverrideFromGenes(x => x.soundDeath, curLSA.soundDeath);
					break;

				default:
					targetSound = XPP_API.PreviewPawn.mutant?.Def.soundCall ?? XPP_API.PreviewPawn.genes.GetSoundOverrideFromGenes(x => x.soundCall, curLSA.soundCall);
					break;
			}

			if (targetSound == null)
			{
				return;
			}

			for (int i = 0; i < targetSound.subSounds.Count; i++)
			{
				SubSoundDef subSound = targetSound.subSounds[i];
				AudioSource output = Find.SoundRoot.sourcePool.GetSource(true);

				output.clip = ((ResolvedGrain_Clip)subSound.RandomizedResolvedGrain()).clip;
				output.volume = AudioSourceUtility.GetSanitizedVolume(subSound.RandomizedVolume(), targetSound);
				output.pitch = AudioSourceUtility.GetSanitizedPitch(subSound.pitchRange.RandomInRange, targetSound);

				for (int j = 0; j < subSound.filters.Count; j++)
				{
					subSound.filters[j].SetupOn(output);
				}

				output.Play();
			}
		}

		/// <inheritdoc/>
		public override void Update()
		{
			XPP_API.PreviewPawn.Drawer.renderer.EnsureGraphicsInitialized();
			XPP_API.PreviewPawn.Drawer.renderer.SetAllGraphicsDirty();
			PawnCacheCameraManager.PawnCacheRenderer.RenderPawn(XPP_API.PreviewPawn, this.pawnRenderTexture, Vector3.zero, this.renderZoom, 0f, this.renderAngle);
		}

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			GUI.DrawTexture(this.rectRender, this.pawnRenderTexture);

			this.buttonRotate.DrawComponent();
			this.buttonVox.DrawComponent();

			if (Mouse.IsOver(this.rectZoom))
			{
				TooltipHandler.TipRegion(this.rectZoom, "Karda.XPP.Render.Zoom.Slider.Tooltip".Translate());
			}

			float newZoom = Widgets.HorizontalSlider(this.rectZoom, this.renderZoom, 0.1f, 2f, label: $"{"Karda.XPP.Render.Zoom.Slider.Label".Translate()}: {this.renderZoom:F1}x", roundTo: 0.1f);

			if (newZoom != this.renderZoom)
			{
				this.renderZoom = newZoom;
				XPP_API.PreviewWindow.RequestRefresh();
			}

			return this.DesiredContentSize;
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

			XPP_API.PreviewWindow.RequestRefresh();
		}
	}
}
