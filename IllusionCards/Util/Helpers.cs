﻿using System;
using System.IO;
using System.Linq;

namespace IllusionCards.Util
{
	static class Helpers
	{
		public static long? FindSequence(Stream stream, byte[] sequence)
		{
			long _originalPos = stream.Position;
			long _bufferPos = stream.Position;
			long _currentPos;
			int bufSize = 4096;
			byte[] buffer = new byte[bufSize];
			byte[] potentialMatch = new byte[sequence.Length];
			long _potentialMatchPos;
			int readBytes;

			readBytes = stream.Read(buffer, 0, bufSize);
			while (readBytes > 0)
			{
				_currentPos = stream.Position;
				for (int i = 0; i < readBytes; i++)
				{
					if (buffer[i] != sequence[0]) continue;
					_potentialMatchPos = _bufferPos + i;
					// if ((i + sequence.Length) > (bufSize - 1)) // in case the sequence goes beyond the buffer
					stream.Seek(_potentialMatchPos, SeekOrigin.Begin);
					_ = stream.Read(potentialMatch, 0, sequence.Length);
					if (potentialMatch.SequenceEqual(sequence))
					{
						stream.Seek(_originalPos, SeekOrigin.Begin);
						return _potentialMatchPos;
					}
					else { stream.Seek(_currentPos, SeekOrigin.Begin); continue; }

					// Some bad code that tries to iterate over the buffer bytes instead of pulling from the stream again.
					// The other way is fast enough, so I won't bother to fix this unless speed becomes an issue.
					//bool _sequenceMatch = true;
					//for (int j = 1; j < sequence.Length; j++)
					//{
					//	if (buffer[i + j] != sequence[j]) { _sequenceMatch = false; break; }
					//	Console.WriteLine($"{buffer[i + j]} = {sequence[j]}");
					//}
					//stream.Seek(_originalPos, SeekOrigin.Begin);
					//if (_sequenceMatch) { return _potentialMatchPos; }
				}

				readBytes = stream.Read(buffer, 0, bufSize);
				_bufferPos += bufSize;
			}

			stream.Seek(_originalPos, SeekOrigin.Begin);
			return null;
		}
	}
}