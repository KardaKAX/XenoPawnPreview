// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using RimWorld;
	using UnityEngine;
	using Verse;

	/// <summary>
	/// Contains utility functionality for generating or modifying a <see cref="Pawn"/>.
	/// </summary>
	public static class PawnUtility
	{
		/// <summary>
		/// The backstory definition for children with no data assigned.
		/// </summary>
		public static readonly BackstoryDef BackstoryNoneChild = new BackstoryDef()
		{
			description = TextBackstoryDescNone.Translate(),
			shuffleable = false,
			slot = BackstorySlot.Childhood,
			title = TextBackstoryNameNone.Translate(),
			titleShort = TextBackstoryNameNone.Translate(),
			modContentPack = XPP_Mod.XPPContentPack,
		};

		/// <summary>
		/// The backstory definition for adults with no data assigned.
		/// </summary>
		public static readonly BackstoryDef BackstoryNoneAdult = new BackstoryDef()
		{
			bodyTypeFemale = BodyTypeDefOf.Female,
			bodyTypeMale = BodyTypeDefOf.Male,
			description = TextBackstoryDescNone.Translate(),
			shuffleable = false,
			slot = BackstorySlot.Adulthood,
			title = TextBackstoryNameNone.Translate(),
			titleShort = TextBackstoryNameNone.Translate(),
			modContentPack = XPP_Mod.XPPContentPack,
		};

		private const string TextBackstoryDescNone = "Karda.XPP.Pawn.Backstory.None.Desc";

		private const string TextBackstoryNameNone = "Karda.XPP.Pawn.Backstory.None.Name";

		private const long TicksToYears = 3600000L;

		private static readonly SimpleCurve DefaultAgeGenerationCurve = (SimpleCurve)AccessTools.Field(typeof(PawnGenerator), "DefaultAgeGenerationCurve").GetValue(null);

		/// <summary>
		/// Clears most information about the given <paramref name="pawn"/> and returns most stats to their default state.
		/// </summary>
		/// <param name="pawn">The <see cref="Pawn"/> being targeted.</param>
		public static void ClearData(this Pawn pawn)
		{
			pawn.apparel = new Pawn_ApparelTracker(pawn);

			pawn.story.Childhood = BackstoryNoneChild;
			pawn.story.Adulthood = pawn.ageTracker.Adult ? BackstoryNoneAdult : null;

			pawn.story.traits.allTraits.Clear();

			pawn.health.Reset();

			foreach (var skillRecord in pawn.skills.skills)
			{
				skillRecord.passion = Passion.None;
				skillRecord.levelInt = 0;
				skillRecord.Notify_SkillDisablesChanged();
			}

			pawn.needs.AddOrRemoveNeedsAsAppropriate();
		}

		/// <summary>
		/// Generates a new semi-random <see cref="Pawn"/> with very minimal data.
		/// </summary>
		/// <returns>The newly generated <see cref="Pawn"/>.</returns>
		public static Pawn GenerateMinimalPawn() => GenerateMinimalPawn(ThingDefOf.Human);

		/// <inheritdoc cref="GenerateMinimalPawn()"/>
		/// <param name="thingDef">The <see cref="ThingDef"/> which should be used to generate a <see cref="Pawn"/>.</param>
		/// <remarks>If <paramref name="thingDef"/> cannot be cast to a <see cref="Pawn"/>, then a new <see cref="Pawn"/> will be created based on <see cref="ThingDefOf.Human"/>.</remarks>
		public static Pawn GenerateMinimalPawn(ThingDef thingDef) => GenerateMinimalPawn(ThingMaker.MakeThing(thingDef) as Pawn);

		/// <inheritdoc cref="GenerateMinimalPawn()"/>
		/// <param name="pawn">The <see cref="Pawn"/> used as the target of this method.</param>
		/// <param name="pawnKindDef">The <see cref="PawnKindDef"/> which should be read from.</param>
		/// <returns>The modified input <see cref="Pawn"/>.</returns>
		/// <remarks>
		/// A <see langword="null"/> or non-<see cref="RaceProperties.Humanlike"/> <paramref name="pawn"/> will result in a new <see cref="Pawn"/> being created with the following properties:<br/>
		/// - <see cref="ThingDefOf.Human"/>.<br/>
		/// - <see cref="PawnKindDefOf.Colonist"/>.
		/// </remarks>
		public static Pawn GenerateMinimalPawn(Pawn pawn, PawnKindDef pawnKindDef = null)
		{
			if (pawn == null || !pawn.RaceProps.Humanlike)
			{
				Log.Warning($"{pawn} was not valid when preparing, creating a new pawn.");
				pawn = (Pawn)ThingMaker.MakeThing(ThingDefOf.Human);
			}

			PawnComponentsUtility.CreateInitialComponents(pawn); // Creates and assigns Pawn tracker fields.

			pawn.kindDef = pawnKindDef ?? PawnKindDefOf.Colonist;

			foreach (var abilityDef in pawn.kindDef.abilities ?? new List<AbilityDef>())
			{
				pawn.abilities.GainAbility(abilityDef);
			}

			pawn.ageTracker.AgeBiologicalTicks = (long)(pawn.RaceProps.ageGenerationCurve != null ? Rand.ByCurve(pawn.RaceProps.ageGenerationCurve) : Rand.ByCurve(DefaultAgeGenerationCurve) * pawn.RaceProps.lifeExpectancy) * TicksToYears;
			pawn.ageTracker.AgeChronologicalTicks = pawn.ageTracker.AgeBiologicalTicks;

			foreach (var apparel in pawn.apparel.WornApparel ?? new List<Apparel>())
			{
				pawn.apparel.Remove(apparel);
			}

			pawn.SetFactionDirect(Faction.OfPlayer);

			if (pawn.genes.GetMelaninGene() == null)
			{
				pawn.genes.AddGene(PawnSkinColors.RandomSkinColorGene(pawn), false);
			}

			if (pawn.genes.GetHairColorGene() == null)
			{
				pawn.genes.AddGene(PawnHairColors.RandomHairColorGeneFor(pawn), false);
			}

			pawn.gender = pawn.kindDef.fixedGender ?? (pawn.RaceProps.hasGenders ? (Rand.Value < 0.5 ? Gender.Male : Gender.Female) : Gender.None);

			if (pawn.kindDef.startingHediffs != null && pawn.ageTracker.CurLifeStage != LifeStageDefOf.HumanlikeBaby)
			{
				HealthUtility.AddStartingHediffs(pawn, pawn.kindDef.startingHediffs);
			}

			pawn.Name = new NameTriple("Forename", "Preview", "Surname");

			pawn.needs.SetInitialLevels();

			pawn.story.Adulthood = pawn.ageTracker.Adult ? BackstoryNoneAdult : null;
			pawn.story.Childhood = BackstoryNoneChild;
			pawn.story.bodyType = PawnGenerator.GetBodyTypeFor(pawn);
			pawn.story.hairDef = PawnStyleItemChooser.RandomHairFor(pawn);
			pawn.story.skinColorOverride = pawn.kindDef.skinColorOverride;
			pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs);

			foreach (var trait in pawn.kindDef.forcedTraits ?? new List<TraitRequirement>())
			{
				pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree.GetValueOrDefault(), true));
			}

			return pawn;
		}

		/// <summary>
		/// Prepares the given <paramref name="pawn"/> so that they are able to be generated safely using RimWorld's native methods.
		/// </summary>
		/// <param name="pawn">The <see cref="Pawn"/> to be prepared.</param>
		/// <returns>The modified <paramref name="pawn"/>.</returns>
		public static Pawn PrepareSafely(this Pawn pawn)
		{
			// Fix components
			// - Ensure all default components exist.
			PawnComponentsUtility.CreateInitialComponents(pawn);

			// Fix story tracker
			// - Ensure head is generated.
			// - Ensure backstories are generated.
			if (pawn.story.headType == null)
			{
				pawn.story.TryGetRandomHeadFromSet(DefDatabase<HeadTypeDef>.AllDefs);
			}

			List<BackstoryCategoryFilter> backstoryFilters = (List<BackstoryCategoryFilter>)AccessTools.Method(typeof(PawnBioAndNameGenerator), "GetBackstoryCategoryFiltersFor").Invoke(null, new object[] { pawn, Faction.OfPlayer.def });

			if (pawn.story.Childhood == null)
			{
				try
				{
					PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Childhood, backstoryFilters, Faction.OfPlayer.def);
				}
				catch (Exception ex)
				{
					pawn.story.Childhood = BackstoryNoneChild;
					Log.Warning($"[XPP] Could not fill childhood backstory with random entry, using fallback.\nReason: {ex}");
				}
			}

			if (pawn.story.Adulthood == null)
			{
				try
				{
					PawnBioAndNameGenerator.FillBackstorySlotShuffled(pawn, BackstorySlot.Adulthood, backstoryFilters, Faction.OfPlayer.def);
				}
				catch (Exception ex)
				{
					pawn.story.Childhood = BackstoryNoneAdult;
					Log.Warning($"[XPP] Could not fill adulthood backstory with random entry, using fallback.\nReason: {ex}");
				}
			}

			HarmonyPatches_Core.PrepareGeneration = true;

			return pawn;
		}
	}
}
