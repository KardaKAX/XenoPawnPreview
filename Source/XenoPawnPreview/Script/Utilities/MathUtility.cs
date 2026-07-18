// Copyright KardaKAX - GNU GPLv3.

namespace Karda.XenoPawnPreview
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Verse;

	/// <summary>
	/// Contains utility functionality for math-based equations.
	/// </summary>
	public static class MathUtility
	{
		/// <summary>
		/// Returns the largest value from all given <see cref="float"/> values.
		/// </summary>
		/// <param name="values">The values to query.</param>
		/// <returns>The largest value.</returns>
		public static float Max(params float[] values)
		{
			if (values == null || values.Length == 0)
			{
				throw new ArgumentException("Values cannot be null or empty.");
			}

			float curMax = float.MinValue;

			foreach (float value in values)
			{
				if (value > curMax)
				{
					curMax = value;
				}
			}

			return curMax;
		}

		/// <summary>
		/// Returns the largest value from an <see cref="IEnumerable{T}"/> of <see cref="float"/> values.
		/// </summary>
		/// <param name="values">The enumerable to query.</param>
		/// <returns>The largest value.</returns>
		public static float Max(IEnumerable<float> values)
		{
			List<float> valuesList = values.ToList();

			if (values == null || valuesList.Count == 0)
			{
				throw new ArgumentException("Values cannot be null or empty.");
			}

			float curMax = float.MinValue;

			foreach (float value in valuesList)
			{
				if (value > curMax)
				{
					curMax = value;
				}
			}

			return curMax;
		}

		/// <summary>
		/// Returns the smallest value from all given <see cref="float"/> values.
		/// </summary>
		/// <param name="values"><inheritdoc cref="Max(float[])"/></param>
		/// <returns>The smallest value.</returns>
		public static float Min(params float[] values)
		{
			if (values == null || values.Length == 0)
			{
				throw new ArgumentException("Values cannot be null or empty.");
			}

			float curMin = float.MaxValue;

			foreach (float value in values)
			{
				if (value < curMin)
				{
					curMin = value;
				}
			}

			return curMin;
		}

		/// <summary>
		/// Returns the smallest value from an <see cref="IEnumerable{T}"/> of <see cref="float"/> values.
		/// </summary>
		/// <param name="values"><inheritdoc cref="Max(float[])"/></param>
		/// <returns>The smallest value.</returns>
		public static float Min(IEnumerable<float> values)
		{
			List<float> valuesList = values.ToList();

			if (values == null || valuesList.Count == 0)
			{
				throw new ArgumentException("Values cannot be null or empty.");
			}

			float curMin = float.MaxValue;

			foreach (float value in values)
			{
				if (value < curMin)
				{
					curMin = value;
				}
			}

			return curMin;
		}
	}
}
