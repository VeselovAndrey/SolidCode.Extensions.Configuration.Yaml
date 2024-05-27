namespace SolidCode.Extensions.Configuration.Yaml.Extensions;

/// <summary>Extensions methods for <see cref="ReadOnlySpan{T}"/>.</summary>
internal static class ReadOnlySpanExtensions
{
	/// <summary>Searches for the specified character in the span starting from the specified index.</summary>
	/// <param name="span">The span to search in.</param>
	/// <param name="value">The character to search for.</param>
	/// <param name="startIndex">The starting index of the search.</param>
	/// <returns>The index of the first occurrence of the character, or -1 if not found.</returns>
	public static int IndexOf(this ReadOnlySpan<char> span, char value, int startIndex)
	{
		ReadOnlySpan<char> slice = span[startIndex..];
		int index = slice.IndexOf(value);

		return index != -1
			? index + startIndex
			: -1;
	}
}
