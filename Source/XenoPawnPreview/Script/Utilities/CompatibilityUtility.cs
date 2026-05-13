// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Linq;
	using System.Reflection;
	using BigAndSmall;
	using HarmonyLib;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains compatibility and utility functionality for compatibility with other mods.
	/// </summary>
	public static class CompatibilityUtility
	{
		static CompatibilityUtility()
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

			BigAndSmall_Assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "BigAndSmall");
			IdeoFactIcon_Assembly = assemblies.FirstOrDefault(x => x.GetName().Name == "IdeoFactIcon");
		}

		/// <summary>
		/// Gets the mod assembly for <b>Big and Small</b>.
		/// </summary>
		public static Assembly BigAndSmall_Assembly { get; }

		/// <summary>
		/// Gets the mod assembly for <b>Ideoligion Icon as Faction Icon</b>.
		/// </summary>
		public static Assembly IdeoFactIcon_Assembly { get; }

		/// <summary>
		/// Defines a <see cref="HarmonyPrefix"/> patch for Big and Small's 'GetInvalidateLater' method, allowing it to cache pawns on the main menu.
		/// </summary>
		/// <param name="__result">The original result of the targeted method.</param>
		/// <param name="pawn">The <see cref="Pawn"/> target of this method.</param>
		/// <returns><see langword="true"/> if the original method is to be executed.</returns>
		public static bool BigAndSmall_AllowCachingOnEntry(ref BSCache __result, Pawn pawn)
		{
			if (Find.UIRoot is UIRoot_Entry)
			{
				__result = HumanoidPawnScaler.GetCache(pawn, true);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Defines a <see cref="HarmonyPrefix"/> patch for <b>Ideoligion Icon as Faction Icon</b>'s 'Get_FactionIcon_Helper' methor, bypassing the cache query if we're on the main menu.
		/// </summary>
		/// <param name="__result">The original result of the targeted method.</param>
		/// <param name="faction">The <see cref="Faction"/> target of this method.</param>
		/// <returns><see langword="true"/> if the original method is to be executed.</returns>
		public static bool IdeoFactIcon_SkipComponentQuery(ref Texture2D __result, Faction faction)
		{
			if (Find.UIRoot is UIRoot_Entry)
			{
				__result = faction.def.FactionIcon;
				return false;
			}

			return true;
		}
	}
}
