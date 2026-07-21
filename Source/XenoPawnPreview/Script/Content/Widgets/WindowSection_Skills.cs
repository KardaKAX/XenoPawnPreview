// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains information about skills of the <see cref="XPP_API.PreviewPawn"/> displayed in the <see cref="PreviewWindow"/>.
	/// </summary>
	public class WindowSection_Skills : WindowSection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSection_Skills"/> class.
		/// </summary>
		public WindowSection_Skills()
			: base()
		{
		}

		/// <inheritdoc/>
		public override Vector2 DesiredContentSize => new Vector2(MinWidth, (Text.LineHeight * DefDatabase<SkillDef>.AllDefsListForReading.Count) + ((XPP_API.MarginElements / 2f) * Math.Max(0, DefDatabase<SkillDef>.AllDefsListForReading.Count - 1)));

		/// <inheritdoc/>
		public override string Title => "Skills".Translate();

		/// <inheritdoc/>
		protected override Vector2 DrawContents()
		{
			SkillUI.DrawSkillsOf(XPP_API.PreviewPawn, this.BoundsContents.position, SkillUI.SkillDrawMode.Menu, this.BoundsContents);

			return this.DesiredContentSize;
		}
	}
}
