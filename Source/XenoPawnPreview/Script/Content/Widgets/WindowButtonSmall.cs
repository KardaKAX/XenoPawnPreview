// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Defines a small button used within the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowButtonSmall
	{
		/// <summary>
		/// The size of a button.
		/// </summary>
		public static readonly Vector2 Size = new Vector2(32f, 32f);

#pragma warning disable SA1401
		/// <summary>
		/// Gets the area in which this component occupies.
		/// </summary>
		public Rect Bounds;
#pragma warning restore SA1401

		private bool highlighted;

		/// <inheritdoc cref="WindowButtonSmall"/>
		public WindowButtonSmall(Texture2D icon, string tooltip = null, Action callback = null, List<FloatMenuOption> options = null)
		{
			this.Bounds = new Rect(Vector2.zero, Size);
			this.Callback = callback;
			this.Icon = icon;
			this.Options = options;
			this.Tooltip = tooltip;
		}

		/// <inheritdoc cref="WindowButtonSmall"/>
		/// <param name="position">The pixel position to place the button at.</param>
		public WindowButtonSmall(Vector2 position, Texture2D icon, string tooltip = null, Action callback = null, List<FloatMenuOption> options = null)
			: this(icon, tooltip, callback, options)
		{
			this.Bounds = new Rect(position, Size);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowButtonSmall"/> class.
		/// </summary>
		/// <param name="bounds">The position of the button.</param>
		/// <param name="icon">The icon to be displayed on the button.</param>
		/// <param name="tooltip">The text to be displayed when the mouse rolls over this button.</param>
		/// <param name="callback">The action to be executed when the button is clicked.</param>
		/// <param name="options">The options to be displayed when this button is right clicked.</param>
		public WindowButtonSmall(Rect bounds, Texture2D icon, string tooltip = null, Action callback = null, List<FloatMenuOption> options = null)
			: this(icon, tooltip, callback, options)
		{
			this.Bounds = new Rect(bounds.position, Size);
		}

		/// <summary>
		/// Gets or sets the action to be executed when the button is clicked.
		/// </summary>
		public Action Callback { get; set; }

		/// <summary>
		/// Gets or sets the action to be executed when the button is highlighted.
		/// </summary>
		public Action<bool> Highlighted { get; set; }

		/// <summary>
		/// Gets or sets any <see cref="FloatMenuOption"/> available when right-clicking this button.
		/// </summary>
		public List<FloatMenuOption> Options { get; set; }

		/// <summary>
		/// Gets or sets the icon to be displayed on the button.
		/// </summary>
		public Texture2D Icon { get; set; }

		/// <summary>
		/// Gets or sets the text to be displayed when the mouse rolls over this button.
		/// </summary>
		public string Tooltip { get; set; }

		/// <summary>
		/// Draws the component.
		/// </summary>
		public void DrawComponent()
		{
			if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(this.Bounds) && this.Options != null)
			{
				Find.WindowStack.Add(new FloatMenu(this.Options));
			}

			if (!this.highlighted && Mouse.IsOver(this.Bounds))
			{
				this.Highlighted?.Invoke(true);
				this.highlighted = true;
			}

			if (this.highlighted && !Mouse.IsOver(this.Bounds))
			{
				this.Highlighted?.Invoke(false);
				this.highlighted = false;
			}

			if (Widgets.ButtonImage(this.Bounds, this.Icon, tooltip: this.Tooltip))
			{
				this.Callback?.Invoke();
			}
		}
	}
}
