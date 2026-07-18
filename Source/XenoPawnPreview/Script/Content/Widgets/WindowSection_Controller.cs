namespace Karda.XenoPawnPreview
{
	using System.Collections.Generic;
	using System.Linq;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains the control buttons placed at the top of the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Controller : WindowSection
	{
		private readonly WindowButtonSmall buttonClear;

		private readonly WindowButtonSmall buttonInfo;

		private readonly WindowButtonSmall buttonNew;

		private readonly WindowButtonSmall buttonSettings;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Controller"/> class.
		/// </summary>
		public WindowSection_Controller()
			: base()
		{
			this.buttonClear = new WindowButtonSmall(
				icon: ContentFinder<Texture2D>.Get("XPP/UI/Icons/PawnRecycle") ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Controller.Pawn.Button.Recycle".Translate(),
				callback: () => XPP_API.PreviewWindow.PawnClear());

			this.buttonInfo = new WindowButtonSmall(
				icon: TexButton.Info ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Controller.Pawn.Button.Infocard".Translate(),
				callback: () => Find.WindowStack.Add(new Dialog_InfoCard(XPP_API.PreviewPawn)));

			this.buttonNew = new WindowButtonSmall(
				icon: ContentFinder<Texture2D>.Get("XPP/UI/Icons/PawnNew") ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Controller.Pawn.Button.New".Translate(),
				callback: () => XPP_API.PreviewWindow.PawnGenerate());

			this.buttonSettings = new WindowButtonSmall(
				icon: ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral") ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Controller.Settings.Button".Translate(),
				callback: () => Find.WindowStack.Add(new Dialog_ModSettings(XPP_API.Settings.Mod)));
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, MinHeight);

		/// <inheritdoc/>
		public override void Update()
		{
			foreach (var stat in DefDatabase<StatDef>.AllDefsListForReading.Where(x => x.Worker.ShouldShowFor(StatRequest.For(XPP_API.PreviewPawn))))
			{
				stat.Worker.TryClearCache();
			}
		}

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			GenUI.DrawElementStack(
				rect: this.BoundsContents,
				rowHeight: WindowButtonSmall.Size.y,
				elements: new List<WindowButtonSmall>()
				{
					this.buttonSettings,
					this.buttonNew,
					this.buttonClear,
					this.buttonInfo,
				},
				drawer: (background, element) =>
				{
					element.Bounds = background;
					element.DrawComponent();
				},
				widthGetter: x => WindowButtonSmall.Size.x,
				rowMargin: XPP_API.MarginElements,
				elementMargin: XPP_API.MarginElements,
				allowOrderOptimization: false);

			return this.DesiredContentSize;
		}
	}
}
