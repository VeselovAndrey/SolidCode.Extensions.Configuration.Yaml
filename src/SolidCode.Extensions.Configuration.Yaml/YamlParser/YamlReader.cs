namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

using System;
using SolidCode.Extensions.Configuration.Yaml.Exceptions;

/// <summary>The reader to read YAML content from the stream.</summary>
/// <param name="yamlReader">The reader for the source YAML stream. Caller side is responsible for creating and disposing the reader instance.</param>
/// <param name="buffer">The buffer to read the source stream. Must be larger than maximum single YAML line / multiline.</param>
internal ref struct YamlReader(StreamReader yamlReader, Span<char> buffer)
{
	private const int _noEolFoundIndex = -1;
	private const int _emptyLineIndent = -1;

	private readonly StreamReader _yamlReader = yamlReader;
	private readonly Span<char> _buffer = buffer;

	private int _scanIndex = int.MinValue; // An index of the current character in the buffer.
	private int _bufferLength = 0; // The actual length of the data in the _buffer. Can be less or equal to the _buffer.Length.

	public int ScanLineNumber = 0;

	/// <summary>Returns <c>true</c> if YAML is fully read.</summary>
	public bool EndOfYaml => (_bufferLength - 1) <= _scanIndex;

	/// <summary>Reads the single line from the YAML source.</summary>
	/// <returns>
	/// The read line or empty <seealso cref="ReadOnlySpan{T}" /> if no more data available.
	/// The result points to the segment of the current buffer and data can be changed after any next read operation.
	/// </returns>
	public ReadOnlySpan<char> ReadLine()
	{
		// Refresh buffer
		if (_bufferLength == 0 || _bufferLength <= _scanIndex) {
			if (_yamlReader.EndOfStream)
				return ReadOnlySpan<char>.Empty;

			_bufferLength = _yamlReader.ReadBlock(_buffer);
			_scanIndex = 0;
		}

		// Look for EOL
		while (_scanIndex < _bufferLength) {
			BufferScanResult nextLine = ScanForNextLine(scanStartIndex: _scanIndex);

			if (nextLine.EolFound) {
				ScanLineNumber++;
				// The next EOL was found, or not found, but there are no more data in the stream.
				_scanIndex = nextLine.EndIndex + nextLine.EolCharsCount + 1; // +1 to move to the next character in the string.
				return _buffer[nextLine.StartIndex..(nextLine.EndIndex + 1)];
			}

			// The EOL was not found and there is no unprocessed data in the buffer.
			if (_scanIndex == 0)
				throw new YamlConfigurationReadingException("The buffer size is smaller than the maximum line length.");

			// The EOL was not found, but there are more characters in the Stream
			Span<char> currentBlock = _buffer[_scanIndex.._bufferLength];
			currentBlock.CopyTo(_buffer);

			_scanIndex = 0;
			Span<char> blockToRead = _buffer[currentBlock.Length..];
			_bufferLength = currentBlock.Length;
			_bufferLength += _yamlReader.ReadBlock(blockToRead);
		}

		throw new YamlConfigurationReadingException("Scan out of buffer range.");
	}

	/// <summary>Reads next lines with indent more than <paramref name="baseIndent"/>.</summary>
	/// <param name="baseIndent">The base indent.</param>
	/// <returns>The structure with multiline value. The result points to the segment of the current buffer and data can be changed after any next read operation.</returns>
	public MultilineReadResult ReadMultiline(int baseIndent)
	{
		int startIndex = _scanIndex;
		int currentIndex = startIndex;
		int linesCount = 0;

		while (true) {
			BufferScanResult nextLine = ScanForNextLine(scanStartIndex: currentIndex);

			if (nextLine.EndIndex != _noEolFoundIndex) {
				Span<char> line = _buffer[nextLine.StartIndex..(nextLine.EndIndex + 1)];
				ScanLineNumber++;
				int indent = GetLineIndent(ref line);

				if (indent == _emptyLineIndent) {
					// Empty line will be converted into line break.
				}
				else if (indent <= baseIndent) {
					_scanIndex = currentIndex;
					return new MultilineReadResult(_buffer[startIndex..(_scanIndex + 1)], linesCount);
				}

				int lineEndingIndex = nextLine.EndIndex + nextLine.EolCharsCount;
				if ((_bufferLength - 1) <= lineEndingIndex) {
					_scanIndex = lineEndingIndex;
					return new MultilineReadResult(_buffer[startIndex..(_scanIndex + 1)], linesCount);
				}

				linesCount++;
				currentIndex = nextLine.EndIndex + nextLine.EolCharsCount + 1; // +1 to move index to the next character
			}
			else {
				// No EOL found but there is more content in the file.
				if (startIndex == 0)
					throw new YamlConfigurationReadingException("The sum of key and multiline value sizes is greater than the buffer size.");

				// Update buffer
				Span<char> currentBlock = _buffer[startIndex.._bufferLength];
				currentBlock.CopyTo(_buffer);

				Span<char> blockToRead = _buffer[currentBlock.Length..];
				_bufferLength = currentBlock.Length;
				_bufferLength += _yamlReader.ReadBlock(blockToRead);

				_scanIndex -= startIndex;
				currentIndex -= startIndex;
				startIndex = _scanIndex;
			}
		}
	}

	private BufferScanResult ScanForNextLine(int scanStartIndex)
	{
		if ((_bufferLength - 1) < _scanIndex)
			throw new YamlConfigurationReadingException("Scan index out of the buffer range.") { Context = new { ScanIndex = _scanIndex } };

		// Look for EOL
		Span<char> currentBlock = _buffer[scanStartIndex.._bufferLength];
		int eolIndex = currentBlock.IndexOfAny(Symbol.CR, Symbol.LF);

		if (eolIndex != _noEolFoundIndex) {
			// EOL found
			int endIndex = scanStartIndex + eolIndex;
			byte eolCharsCount = 1;

			// Check for Windows EOL (CR LF).
			if (_buffer[endIndex] == Symbol.CR) {
				if (endIndex < (_buffer.Length - 1) && _buffer[endIndex + 1] == Symbol.LF)
					eolCharsCount++;

				if (endIndex == (_buffer.Length - 1)) {
					int nextChar = _yamlReader.Peek();
					if (nextChar == Symbol.LF)
						_yamlReader.Read();
				}
			}

			return new BufferScanResult(scanStartIndex, endIndex - 1, eolCharsCount);
		}

		// No EOL found (but stream there are no more data in the stream)
		if (_yamlReader.EndOfStream)
			return new BufferScanResult(scanStartIndex, _bufferLength - 1, eolCharsCount: 0);

		// No EOL found, but buffer can be updated.
		return new BufferScanResult(scanStartIndex, _noEolFoundIndex, eolCharsCount: 0);
	}

	private static int GetLineIndent(ref Span<char> line) => line.IndexOfAnyExcept(Symbol.Space, Symbol.Tab);

	internal readonly struct BufferScanResult(int startIndex, int endIndex, byte eolCharsCount)
	{
		public readonly int StartIndex = startIndex;
		public readonly int EndIndex = endIndex;
		public readonly byte EolCharsCount = eolCharsCount;

		// This property covers 2 cases:
		// - when end of line index was found (but last line can have no EOL characters);
		// - when EOL characters were found.
		public bool EolFound => EndIndex != _noEolFoundIndex || 0 < EolCharsCount;
	}
}

/// <summary>Represents the result of reading multiple lines from a YAML source.</summary>
/// <param name="lines">The lines read from the YAML source.</param>
/// <param name="linesCount">The count of lines read from the YAML source.</param>
internal ref struct MultilineReadResult(ReadOnlySpan<char> lines, int linesCount)
{
	/// <summary>The lines read from the YAML source.</summary>
	public ReadOnlySpan<char> Lines = lines;

	/// <summary>The count of lines read from the YAML source.</summary>
	public readonly int LinesCount = linesCount;
}