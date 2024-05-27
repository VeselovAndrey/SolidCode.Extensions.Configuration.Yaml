namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

using Microsoft.Extensions.Configuration;
using SolidCode.Extensions.Configuration.Yaml.Exceptions;
using SolidCode.Extensions.Configuration.Yaml.Extensions;

internal static class YamlConfigurationParser
{
	private const int _noSymbolFoundIndex = -1;
	private const int _emptyLineIndent = -1;

	public static IDictionary<string, string?> Parse(Stream yamlStream, YamlConfigurationOptions options)
	{
		var configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		var nodesStack = new Stack<ActiveYamlNode>(capacity: 10);
		int activeIndent = 0;

		using var yamlReader = new StreamReader(yamlStream);

		int bufferSize = GetBufferSize(yamlStream, options);
		var buffer = new YamlReader(yamlReader, stackalloc char[bufferSize]);

		while (!buffer.EndOfYaml) {
			if (!TryReadNextNode(ref buffer, options, out YamlNode node))
				break;

			// If current line indent is less than the active (previous line) indent,
			// then remove from the stack all opened nodes with the indent bigger than the current line indent.
			if (node.Indent < activeIndent) {
				while (nodesStack.TryPeek(out ActiveYamlNode? parentNode) && parentNode.Indent >= node.Indent)
					nodesStack.Pop();
			}

			// Process the current line.
			switch (node.NodeType) {
				case YamlNodeType.Mapping:
					ProcessMappingNode(node, nodesStack, configuration);
					break;

				case YamlNodeType.Sequence:
					ProcessSequenceNode(node, nodesStack, configuration, buffer.ScanLineNumber);
					break;

				default:
					throw new YamlConfigurationParsingException($"Unexpected node type `{node.NodeType}` at line {buffer.ScanLineNumber}.");
			}

			activeIndent = node.Indent;
		}

		return configuration;
	}

	private static int GetBufferSize(Stream yamlStream, YamlConfigurationOptions options)
	{
		const int defaultBufferSize = 10 * 1024;
		const int maxBufferSize = 50 * 1024;

		int bufferSize = 0 < options.BufferSize
			? options.BufferSize
			: 0 < (int)yamlStream.Length
				? (int)yamlStream.Length
				: defaultBufferSize;

		if (maxBufferSize < bufferSize)
			bufferSize = maxBufferSize;

		return bufferSize;
	}

	private static void ProcessMappingNode(YamlNode currentNode, Stack<ActiveYamlNode> parentNodes, Dictionary<string, string?> configuration)
	{
		if (currentNode.Value.Length == 0) {
			string path = parentNodes.TryPeek(out ActiveYamlNode? parentNode)
				? $"{parentNode.Path}{ConfigurationPath.KeyDelimiter}{currentNode.Key}"
				: currentNode.Key;

			var newNode = new ActiveYamlNode(YamlNodeType.Mapping, currentNode.Indent, path);
			parentNodes.Push(newNode);
		}
		else {
			// If parent node has no value but has same indent, it's a mapping without value. So need to close it.
			if (parentNodes.TryPeek(out ActiveYamlNode? mappingParentNode) && mappingParentNode.Indent == currentNode.Indent)
				parentNodes.TryPop(out ActiveYamlNode? _);

			string path = parentNodes.TryPeek(out ActiveYamlNode? parentNode)
				? $"{parentNode.Path}{ConfigurationPath.KeyDelimiter}{currentNode.Key}"
				: currentNode.Key;

			configuration[path] = currentNode.Value;
		}
	}

	private static void ProcessSequenceNode(YamlNode currentNode, Stack<ActiveYamlNode> parentNodes, Dictionary<string, string?> configuration, int lineNumber)
	{
		if (currentNode.Value.Length == 0) {
			if (!parentNodes.TryPeek(out ActiveYamlNode? parentNode))
				throw new YamlConfigurationParsingException($"The sequence node at line {lineNumber} must have a parent node.");

			parentNode.CurrentArrayIndex++;

			string path = currentNode.Key.Length == 0
				? parentNode.Path
				: $"{parentNode.Path}{ConfigurationPath.KeyDelimiter}{currentNode.Key}";

			var newNode = new ActiveYamlNode(YamlNodeType.Sequence, currentNode.Indent, path);
			parentNodes.Push(newNode);
		}
		else {
			if (!parentNodes.TryPeek(out ActiveYamlNode? parentNode))
				throw new YamlConfigurationParsingException($"The sequence node at line {lineNumber} must have a parent node.");

			parentNode.CurrentArrayIndex++;

			string path = currentNode.Key.Length == 0
				? parentNode.Path
				: $"{parentNode.Path}{ConfigurationPath.KeyDelimiter}{currentNode.Key}";

			configuration[path] = currentNode.Value;
		}
	}

	private static bool TryReadNextNode(ref YamlReader buffer, YamlConfigurationOptions options, out YamlNode node)
	{
		ReadOnlySpan<char> line = buffer.ReadLine();
		ParsedLine parsedLine = ParseLine(ref line);

		while (parsedLine.LineType is YamlLineType.Empty or YamlLineType.Comment) {
			if (buffer.EndOfYaml) {
				node = new YamlNode(YamlNodeType.Unspecified, indent: default, key: string.Empty, value: string.Empty);
				return false;
			}

			line = buffer.ReadLine();
			parsedLine = ParseLine(ref line);
		}

		// Save key to string because buffer can be changed in case of long single line or multiline value.
		string key = parsedLine.Key.Trim().ToString();

		string valueString;
		ReadOnlySpan<char> value = parsedLine.Value;
		value = RemoveComment(value).Trim();

		if (0 < value.Length) {
			// Check if the value is a quoted string.
			if (value[0] is YamlSymbol.DoubleQuote or YamlSymbol.SingleQuote) {
				valueString = RemoveQuotes(value);
			}
			else {
				// Read multi-line value or single line value
				if (value[0] is YamlSymbol.FoldedScalar or YamlSymbol.LiteralScalar)
					valueString = BuildMultilineValue(ref buffer, parsedLine.Indent, value[0], options);
				else
					valueString = value.Trim().ToString();
			}
		}
		else {
			valueString = string.Empty;
		}

		node = new YamlNode(ConvertYamlLineTypeToNodeType(parsedLine.LineType), parsedLine.Indent, key, valueString);
		return true;
	}

	private static YamlNodeType ConvertYamlLineTypeToNodeType(YamlLineType lineType)
		=> lineType switch {
			YamlLineType.Mapping => YamlNodeType.Mapping,
			YamlLineType.Sequence => YamlNodeType.Sequence,
			YamlLineType.Scalar => YamlNodeType.Scalar,
			_ => throw new ArgumentOutOfRangeException(nameof(lineType), $"Unexpected {nameof(YamlLineType)} value: {lineType}.")
		};

	private static ParsedLine ParseLine(ref ReadOnlySpan<char> line)
	{
		int indent = GetLineIndent(ref line);
		if (indent == _emptyLineIndent)
			return new ParsedLine(YamlLineType.Empty, indent, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);

		ReadOnlySpan<char> lineContent = line[indent..];

		if (lineContent[0] == YamlSymbol.Comment)
			return new ParsedLine(YamlLineType.Comment, indent, ReadOnlySpan<char>.Empty, ReadOnlySpan<char>.Empty);

		GetKeyAndValue(lineContent, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value);

		return key.IsEmpty
			? value[0] == YamlSymbol.Sequence
				? new ParsedLine(YamlLineType.Sequence, indent, ReadOnlySpan<char>.Empty, value[1..])
				: new ParsedLine(YamlLineType.Scalar, indent, ReadOnlySpan<char>.Empty, value)
			: key[0] == YamlSymbol.Sequence
				? new ParsedLine(YamlLineType.Sequence, indent, key[1..], value)
				: new ParsedLine(YamlLineType.Mapping, indent, key.Trim(), value);

		static void GetKeyAndValue(ReadOnlySpan<char> line, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
		{
			int separatorIndex = line.IndexOf(YamlSymbol.KeySeparator);
			if (separatorIndex == _noSymbolFoundIndex) {
				key = ReadOnlySpan<char>.Empty;
				value = line;
				return;
			}

			// If the separator is not the last character in the line, then check for the space after the separator.
			if (separatorIndex < (line.Length - 1) && line[separatorIndex + 1] != Symbol.Space) {
				key = ReadOnlySpan<char>.Empty;
				value = line;
				return;
			}

			// Check for the case when the separator is inside the quoted string.
			int quoteIndex = line.IndexOfAny([YamlSymbol.SingleQuote, YamlSymbol.DoubleQuote]);
			if (quoteIndex != _noSymbolFoundIndex && quoteIndex < separatorIndex) {
				key = ReadOnlySpan<char>.Empty;
				value = line;
				return;
			}

			key = line[..separatorIndex];
			value = line[(separatorIndex + 1)..];
		}
	}

	private static string RemoveQuotes(ReadOnlySpan<char> value)
	{
		char quoteChar = value[0];

		// Find the next unescaped quotation mark.
		(int valueEndIndex, int escapesCount) = GetEndIndex(value, quoteChar);
		if (valueEndIndex == _noSymbolFoundIndex)
			throw new YamlConfigurationParsingException("There is no ending quotation mark.");

		value = value[1..valueEndIndex];

		if (0 < escapesCount)
			return CreateUnescapedValue(value, escapesCount, quoteChar);

		// In case of quoted string, the rest of the line can be ignored.
		return value.ToString();

		static (int EndIndex, int EscapesCount) GetEndIndex(ReadOnlySpan<char> value, char quotationMark)
		{
			int escapedQuotesCount = 0;

			for (int i = 1; i < value.Length; i++) {
				if (value[i] == quotationMark) {
					if (value[i - 1] != YamlSymbol.Escape)
						return (i, escapedQuotesCount);

					escapedQuotesCount++;
				}
			}

			return (_noSymbolFoundIndex, escapedQuotesCount);
		}

		static string CreateUnescapedValue(ReadOnlySpan<char> value, int expectedEscapedQuotes, char quoteChar)
		{
			Span<char> unescapedValue = stackalloc char[value.Length - expectedEscapedQuotes];
			int unescapedIndex = 0;

			for (int i = 0; i < value.Length; i++) {
				if (value[i] == YamlSymbol.Escape && i < value.Length && value[i + 1] == quoteChar) {
					unescapedValue[unescapedIndex] = quoteChar;
					i++;
				}
				else {
					unescapedValue[unescapedIndex] = value[i];
				}

				unescapedIndex++;
			}

			return unescapedValue.ToString();
		}
	}

	private static ReadOnlySpan<char> RemoveComment(ReadOnlySpan<char> value)
	{
		int commentIndex = -1;
		do {
			commentIndex = value.IndexOf(YamlSymbol.Comment, commentIndex + 1);
			if (0 == commentIndex || (0 < commentIndex && value[commentIndex - 1] == Symbol.Space)) {
				value = value[..commentIndex];
				break;
			}
		} while (commentIndex != -1);

		return value;
	}

	private static string BuildMultilineValue(ref YamlReader buffer, int baseIndent, char scalarSymbol, YamlConfigurationOptions options)
	{
		MultilineReadResult readResult = buffer.ReadMultiline(baseIndent);
		int scalarBlockIndent = GetLineIndent(ref readResult.Lines);

		// Assuming that incoming text uses 1 char as EOL (LF). So in the case of CRLF need to add 1 character per line.
		int bufferLength = options.EndOfLineType == EndOfLineType.Windows
			? readResult.Lines.Length + readResult.LinesCount
			: readResult.Lines.Length;

		ReadOnlySpan<char> eol = options.EndOfLineType == EndOfLineType.Windows
			? EndOfLine.Windows.AsSpan()
			: EndOfLine.Unix.AsSpan();

		Span<char> value = stackalloc char[bufferLength];
		int writerIndex = 0;
		int readerIndex = 0;
		while (readerIndex < readResult.Lines.Length - 1) {
			ReadOnlySpan<char> currentBlock = readResult.Lines[readerIndex..readResult.Lines.Length];
			int eolIndex = currentBlock.IndexOfAny(Symbol.CR, Symbol.LF);

			// If the EOL index was not found then assume that the current line ends at the scalar block ending.
			if (eolIndex == -1)
				eolIndex = readResult.Lines.Length - readerIndex;

			ReadOnlySpan<char> currentLine = readResult.Lines[readerIndex..(readerIndex + eolIndex)];
			int currentLineIndent = GetLineIndent(ref currentLine);
			if (scalarBlockIndent == 0 && currentLineIndent != _emptyLineIndent)
				scalarBlockIndent = currentLineIndent;

			currentLine = currentLineIndent == _emptyLineIndent
				? Span<char>.Empty
				: currentLine[scalarBlockIndent..].TrimEnd();

			Span<char> currentValue = value[writerIndex..];

			// Add separator for folded scalar
			if (scalarSymbol == YamlSymbol.FoldedScalar && 0 < writerIndex) {
				if (scalarBlockIndent < currentLineIndent) {
					if (value[writerIndex - 1] is not Symbol.CR and not Symbol.LF) {
						eol.CopyTo(currentValue);
						writerIndex += eol.Length;
						currentValue = value[writerIndex..];
					}

					currentLine = currentLine.TrimStart();
				}
				else if (scalarBlockIndent == currentLineIndent && value[writerIndex - 1] is not Symbol.CR and not Symbol.LF) {
					currentValue[0] = Symbol.Space;
					writerIndex++;
					currentValue = value[writerIndex..];
				}
			}

			// Copy line
			currentLine.CopyTo(currentValue);
			writerIndex += currentLine.Length;

			// Add EOL
			if (writerIndex < value.Length - 1) {
				if (scalarSymbol == YamlSymbol.LiteralScalar) {
					currentValue = value[writerIndex..];
					eol.CopyTo(currentValue);
					writerIndex += eol.Length;
				}
				else // scalarSymbol == YamlSymbol.FoldedScalar
				{
					currentValue = value[writerIndex..];
					if (scalarBlockIndent < currentLineIndent) {
						eol.CopyTo(currentValue);
						writerIndex += eol.Length;
					}
				}
			}

			readerIndex += eolIndex + 1; // +1 to skip CR or LF character
			if (readerIndex < (readResult.Lines.Length - 2) && readResult.Lines[readerIndex - 1] == Symbol.CR && readResult.Lines[readerIndex] == Symbol.LF)
				readerIndex++;
		}

		return options.EndingLineBreaks switch {
			ScalarEndingLineBreaks.KeepAsIs => value[..writerIndex].ToString(),
			ScalarEndingLineBreaks.Normalize => NormalizeLineBreaks(value[..writerIndex]).ToString(),
			ScalarEndingLineBreaks.Trim => value[..writerIndex].TrimEnd([Symbol.LF, Symbol.CR]).ToString(),
			_ => throw new ArgumentOutOfRangeException(nameof(options), $"Invalid {nameof(options.EndingLineBreaks)} setting.")
		};
	}

	private static ReadOnlySpan<char> NormalizeLineBreaks(ReadOnlySpan<char> source)
	{
		Span<char> trimElements = stackalloc char[2];
		trimElements[0] = Symbol.CR;
		trimElements[1] = Symbol.LF;

		ReadOnlySpan<char> trimmed = source.TrimEnd(trimElements);
		if (trimmed.Length < source.Length) {
			int index = trimmed.Length; // The length is equal to the index of the next symbol in source.

			// Check for POSIX (Linux, Mac) EOL
			if (source[index] is Symbol.LF)
				return source[..(index + 1)];

			// Check for Windows / DOS EOL
			index++;
			if ((index < source.Length - 1) && source[index - 1] == Symbol.CR && source[index] == Symbol.LF)
				return source[..(index + 1)];
		}

		return source;
	}

	private static int GetLineIndent(ref ReadOnlySpan<char> line) => line.IndexOfAnyExcept(Symbol.Space, Symbol.Tab);
}

internal static class YamlSymbol
{
	public const char KeySeparator = ':';
	public const char Comment = '#';
	public const char Escape = '\\';
	public const char Sequence = '-';
	public const char LiteralScalar = '|';
	public const char FoldedScalar = '>';

	public const char DoubleQuote = '"';
	public const char SingleQuote = '\'';
}

internal static class Symbol
{
	public const char Space = ' ';
	public const char Tab = '\t';

	public const char CR = '\r';
	public const char LF = '\n';
}

internal static class EndOfLine
{
	public const string Windows = "\r\n";
	public const string Unix = "\n";
}