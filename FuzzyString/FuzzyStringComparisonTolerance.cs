namespace FuzzyString
{
	public enum ComparisonTolerance
	{
		/// <summary>
		/// The strings must be an exact match.
		/// </summary>
		Exact,

		/// <summary>
		/// The strings must be extremely similar.
		/// </summary>
		Strong,

		/// <summary>
		/// The strings must be notably similar.
		/// </summary>
		Normal,

		/// <summary>
		/// The strings must be marginally similar.
		/// </summary>
		Weak,

		/// <summary>
		/// The strings must be very different.
		/// </summary>
		Distinct,

		/// <summary>
		/// The strings must be extremely different.
		/// </summary>
		Unique
	}
}
