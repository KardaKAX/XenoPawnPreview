// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// The base class for all window sections of the <see cref="PreviewWindow"/>.
	/// </summary>
	/// <remarks>This component does not allow manual positioning. Use <see cref="Widgets.BeginGroup(Rect)"/> to set position.</remarks>
	public abstract class WindowSection
	{
		/// <summary>
		/// The minimum height of this widget.
		/// </summary>
		public const float MinHeight = 32f;

		/// <summary>
		/// The minimum width of this widget.
		/// </summary>
		public const float MinWidth = 256f;

		private readonly WindowButtonSmall buttonMinimise;

		private bool collapsed;

		private bool exceptionThrown;

		private Rect bounds;

		private Rect boundsContents;

		private Rect boundsTitle;

		private TextAnchor oldAnchor;

		private Color oldColour;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection"/> class.
		/// </summary>
		public WindowSection()
		{
			this.boundsTitle = new Rect(0f, 0f, MinWidth, MinHeight);
			this.boundsContents = new Rect(this.boundsTitle.xMin, this.boundsTitle.yMax, 0f, 0f);

			this.buttonMinimise = new WindowButtonSmall(
				position: new Vector2(this.boundsTitle.xMax - WindowButtonSmall.Size.x, 0f),
				icon: this.Collapsed ? ContentFinder<Texture2D>.Get("XPP/UI/Icons/CategoryExpand") ?? BaseContent.BadTex : ContentFinder<Texture2D>.Get("XPP/UI/Icons/CategoryCollapse") ?? BaseContent.BadTex,
				tooltip: "Karda.XPP.Interface.Section.Collapse".Translate(),
				callback: () => this.Collapsed = !this.Collapsed);
		}

		/// <inheritdoc cref="WindowSection"/>
		/// <param name="title">The title text to be displayed on this section.</param>
		public WindowSection(string title)
			: this()
		{
			this.Title = title;
		}

		/// <summary>
		/// Gets or sets a value indicating whether this widget is currently in a collapsed state.
		/// </summary>
		/// <remarks>When collapsed, this widget will not display any content (except the <see cref="Title"/>) and will also be resized down to the <see cref="MinSize"/>.</remarks>
		public bool Collapsed
		{
			get => this.collapsed && !this.Title.NullOrEmpty();
			set
			{
				if (this.collapsed != value)
				{
					if (this.Title.NullOrEmpty())
					{
						this.collapsed = false;
					}
					else
					{
						this.collapsed = value;
					}

					this.buttonMinimise.Icon = this.Collapsed
						? ContentFinder<Texture2D>.Get("XPP/UI/Icons/CategoryExpand") ?? BaseContent.BadTex
						: ContentFinder<Texture2D>.Get("XPP/UI/Icons/CategoryCollapse") ?? BaseContent.BadTex;
					this.buttonMinimise.Tooltip = this.Collapsed
						? "Karda.XPP.Interface.Section.Expand".Translate()
						: "Karda.XPP.Interface.Section.Collapse".Translate();
					XPP_API.PreviewWindow.RequestRefresh();
				}
			}
		}

		/// <summary>
		/// Gets the size this widget wants to occupy.
		/// </summary>
		/// <remarks>The widget will attempt to use all of the available space specified by this value, but will expand beyond this value if required when drawing its contents.</remarks>
		public abstract Vector2 DesiredContentSize { get; }

		/// <summary>
		/// Gets the title of this widget.
		/// </summary>
		/// <remarks>If no <see langword="string"/> is specified, then the title section will not be rendered, and can never be collapsed.</remarks>
		public virtual string Title { get; }

		/// <summary>
		/// Gets the <see cref="Rect"/> which contains the entirety of this widget.
		/// </summary>
		public Rect Bounds { get => this.bounds; }

		/// <summary>
		/// Gets the <see cref="Rect"/> which contains all the contents of this widget with the position <see cref="Vector2.zero"/>.
		/// </summary>
		protected Rect BoundsContents { get => new Rect(Vector2.zero, this.boundsContents.size); }

		/// <summary>
		/// Gets the old text anchor before rendering this widget.
		/// </summary>
		protected TextAnchor OldAnchor { get => this.oldAnchor; }

		/// <summary>
		/// Gets the old GUI colour before rendering this widget.
		/// </summary>
		protected Color OldColour { get => this.oldColour; }

		/// <summary>
		/// Draws this widget.
		/// </summary>
		public void Draw()
		{
			if (this.exceptionThrown)
			{
				Log.ErrorOnce($"[XPP] An exception was thrown whilst drawing '{this}'.\nIt will no longer be rendered.", this.GetHashCode());
				return;
			}

			this.oldAnchor = Text.Anchor;
			this.oldColour = GUI.color;

			Widgets.BeginGroup(this.Bounds);

			try
			{
				if (!this.Title.NullOrEmpty())
				{
					this.DrawTitle();
				}
			}
			catch (Exception ex)
			{
				this.exceptionThrown = true;

				Log.Error($"[XPP] Exception whilst drawing '{this}' (Title)\n{ex}");
				Widgets.EndGroup(); // Bounds

				return;
			}

			if (!this.Collapsed)
			{
				Widgets.BeginGroup(this.boundsContents);

				try
				{
					this.boundsContents.position = (this.Title.NullOrEmpty() ? Vector2.zero : new Vector2(this.boundsTitle.xMin, this.boundsTitle.yMax)) + new Vector2(XPP_API.MarginElements, XPP_API.MarginElements);

					Vector2 actualContentSize = this.DrawContents();

					this.boundsContents.width = MinWidth;
					this.boundsContents.height = MathUtility.Max(actualContentSize.y, this.DesiredContentSize.y, MinHeight);
				}
				catch (Exception ex)
				{
					this.exceptionThrown = true;

					Log.Error($"[XPP] Exception whilst drawing '{this}' (Contents)\n{ex}");
					Widgets.EndGroup(); // rectContents
					Widgets.EndGroup(); // Bounds

					return;
				}

				Widgets.EndGroup(); // rectContents
			}

			Widgets.EndGroup(); // Bounds

			GUI.color = this.oldColour;
			Text.Anchor = this.oldAnchor;

			this.bounds.size = new Vector2(
				MinWidth + (XPP_API.MarginElements * 2f),
				this.Collapsed ? this.boundsTitle.height : (this.Title.NullOrEmpty() ? this.boundsContents.height : this.boundsContents.yMax - this.boundsTitle.yMin) + (XPP_API.MarginElements * 2f));
		}

		/// <summary>
		/// Updates any cached information required during the draw process.
		/// </summary>
		public virtual void Update()
		{
		}

		/// <summary>
		/// Draws the contents of this widget.
		/// </summary>
		/// <returns>The actual size of the contents being drawn.</returns>
		protected abstract Vector2 DrawContents();

		/// <summary>
		/// Draws the title of this widget.
		/// </summary>
		private void DrawTitle()
		{
			this.boundsTitle.width = this.bounds.width;

			Widgets.DrawWindowBackground(this.boundsTitle);

			GUI.color = Color.yellow;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(this.boundsTitle) { x = XPP_API.MarginElements }, this.Title);
			Text.Anchor = this.oldAnchor;
			GUI.color = this.oldColour;

			this.buttonMinimise.Bounds.x = this.boundsTitle.xMax - WindowButtonSmall.Size.x;
			this.buttonMinimise.DrawComponent();
		}
	}
}
