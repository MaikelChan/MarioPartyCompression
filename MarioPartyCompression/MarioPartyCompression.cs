using System;
using System.IO;
using System.Text;

namespace MarioPartyCompression
{
    public static class MPCompression
    {
        const int SEARCH_WINDOW_SIZE = 0x400;

        #region Decompress

        /// <summary>
        /// Open a compressed Mario Party data file, decompress it and save it into "outputFile".
        /// </summary>
        /// <param name="inputFilePath">Path to the compressed file to open.</param>
        /// <param name="outputFilePath">Path where the uncompressed data will be saved.</param>
        public static void Decompress(string inputFilePath, string outputFilePath)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"File \"{inputFilePath}\" does not exist!", inputFilePath);
            }

            using (FileStream compressedStream = File.OpenRead(inputFilePath))
            {
                byte[] uncompressedData = Decompress(compressedStream);
                File.WriteAllBytes(outputFilePath, uncompressedData);
            }
        }

        /// <summary>
        /// Decompress a compressed data buffer from Mario Party.
        /// </summary>
        /// <param name="compressedBuffer">Buffer containing the compressed data.</param>
        /// <returns>The uncompressed data buffer.</returns>
        public static byte[] Decompress(byte[] compressedBuffer)
        {
            using (MemoryStream compressedStream = new MemoryStream(compressedBuffer))
            {
                return Decompress(compressedStream);
            }
        }

        /// <summary>
        /// Decompress a compressed data stream from Mario Party.
        /// </summary>
        /// <param name="compressedStream">Stream containing the compressed data.</param>
        /// <returns>The uncompressed data buffer.</returns>
        public static byte[] Decompress(Stream compressedStream)
        {
            byte[] uncompressedBuffer;

            using (BinaryReader br = new BinaryReader(compressedStream, Encoding.UTF8, true))
            {
                uint uncompressedLength = Utils.SwapU32(br.ReadUInt32());

                if (Utils.SwapU32(br.ReadUInt32()) != 0x1)
                {
                    throw new FormatException("This is not a valid Mario Party compressed data.");
                }

                uncompressedBuffer = new byte[uncompressedLength];
                uint uncompressedBufferPosition = 0;

                byte[] table = new byte[SEARCH_WINDOW_SIZE];
                ushort tablePosition = 0;
                ushort tablePositionToRead;

                byte currentControlCode = br.ReadByte();

                for (; ; )
                {
                    // Check each bit in the currentControlCode
                    for (byte i = 0; i < 8; i++)
                    {
                        if (uncompressedBufferPosition >= uncompressedLength)
                        {
                            // End of file
                            break;
                        }

                        if ((currentControlCode & 1) == 1)
                        {
                            // Uncompressed data
                            uncompressedBuffer[uncompressedBufferPosition] = br.ReadByte();
                            table[tablePosition] = uncompressedBuffer[uncompressedBufferPosition];

                            uncompressedBufferPosition++;
                            tablePosition = (ushort)((tablePosition + 1) & 0x3FF);
                        }
                        else
                        {
                            // Compressed data
                            byte compressedFlag1 = br.ReadByte();
                            byte compressedFlag2 = br.ReadByte();

                            tablePositionToRead = (ushort)((((compressedFlag2 & 0xC0) << 2) | compressedFlag1) & 0x3FF);
                            byte bytesToCopy = (byte)((compressedFlag2 & 0x3F) + 3);

                            for (int n = 0; n < bytesToCopy; n++)
                            {
                                uncompressedBuffer[uncompressedBufferPosition] = table[(tablePositionToRead + 66) & 0x3FF];
                                table[tablePosition] = uncompressedBuffer[uncompressedBufferPosition];

                                tablePositionToRead++;
                                uncompressedBufferPosition++;
                                tablePosition = (ushort)((tablePosition + 1) & 0x3FF);
                            }
                        }

                        currentControlCode >>= 1;
                    }

                    if (compressedStream.Position >= compressedStream.Length && uncompressedBufferPosition < uncompressedLength)
                    {
                        throw new FormatException("Reached the end of the compressed data prematurely.");
                    }

                    if (uncompressedBufferPosition >= uncompressedLength/* || fs1.Position >= fs1.Length*/)
                    {
                        // End of file
                        break;
                    }

                    currentControlCode = br.ReadByte();
                }
            }

            return uncompressedBuffer;
        }

        #endregion

        #region Compress

        /// <summary>
        /// Open an uncompressed Mario Party data file, compress it and save it into "outputFile".
        /// </summary>
        /// <param name="inputFilePath">Path to the uncompressed file to open.</param>
        /// <param name="outputFilePath">Path where the compressed data will be saved.</param>
        public static void Compress(string inputFilePath, string outputFilePath)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException($"File \"{inputFilePath}\" does not exist!", inputFilePath);
            }

            using (FileStream uncompressedStream = File.OpenRead(inputFilePath))
            {
                byte[] compressedData = Compress(uncompressedStream);
                File.WriteAllBytes(outputFilePath, compressedData);
            }
        }

        /// <summary>
        /// Compress an uncompressed data buffer from Mario Party.
        /// </summary>
        /// <param name="uncompressedBuffer">Buffer containing the uncompressed data.</param>
        /// <returns>The compressed data buffer.</returns>
        public static byte[] Compress(byte[] uncompressedBuffer)
        {
            using (MemoryStream compressedStream = new MemoryStream(uncompressedBuffer))
            {
                return Compress(compressedStream);
            }
        }

        /// <summary>
        /// Compress an uncompressed data stream from Mario Party.
        /// </summary>
        /// <param name="uncompressedStream">Stream containing the uncompressed data.</param>
        /// <returns>The compressed data buffer.</returns>
        public static byte[] Compress(Stream uncompressedStream)
        {
            // Create a buffer big enough to contain the search window (SEARCH_WINDOW_SIZE) and the uncompressed data
            byte[] uncompressedBuffer = new byte[uncompressedStream.Length + SEARCH_WINDOW_SIZE];
            uint uncompressedBufferPosition = SEARCH_WINDOW_SIZE;

            using (MemoryStream ms = new MemoryStream(uncompressedBuffer))
            {
                ms.Position = SEARCH_WINDOW_SIZE;
                uncompressedStream.CopyTo(ms);
            }

            using (MemoryStream compressedStream = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(compressedStream))
            {
                bw.Write(Utils.SwapU32((uint)(uncompressedBuffer.Length - SEARCH_WINDOW_SIZE)));
                bw.Write(Utils.SwapU32(1));

                int windowPosition = 0;
                long currentControlCodePosition = compressedStream.Position;
                long compressedFilePosition = compressedStream.Position + 1;
                byte currentControlCode = 0;
                byte bitsToWrite = 0;

                for (; ; )
                {
                    ushort maxCompression = 0;
                    ushort maxCompressionOffset = 0;

                    for (int n = 0; n < SEARCH_WINDOW_SIZE; n++)
                    {
                        ushort bytesToCompress = 0;

                        if (uncompressedBuffer[uncompressedBufferPosition + bytesToCompress] == uncompressedBuffer[windowPosition + n])
                        {
                            while (uncompressedBuffer[uncompressedBufferPosition + bytesToCompress] == uncompressedBuffer[windowPosition + n + bytesToCompress])
                            {
                                bytesToCompress++;
                                if (bytesToCompress >= 0x40) break;
                                if ((uncompressedBufferPosition + bytesToCompress) >= uncompressedBuffer.Length) break;
                            }

                            if (bytesToCompress > maxCompression)
                            {
                                maxCompression = bytesToCompress;
                                maxCompressionOffset = (ushort)n;
                            }
                        }
                    }

                    if (maxCompression > 2)
                    {
                        // Compress
                        compressedStream.Seek(compressedFilePosition, SeekOrigin.Begin);

                        ushort length = (ushort)((maxCompression - 3) & 0x3F);
                        ushort offset = (ushort)((maxCompressionOffset + (uncompressedBufferPosition - SEARCH_WINDOW_SIZE) - 66) & 0x3FF);

                        bw.Write((byte)(offset & 0xFF));
                        bw.Write((byte)(((offset & 0x300) >> 2) | length));
                        compressedFilePosition += 2;

                        uncompressedBufferPosition += maxCompression;
                        windowPosition += maxCompression;
                    }
                    else
                    {
                        // Do not compress
                        currentControlCode = (byte)(currentControlCode | 0x80);
                        compressedStream.Seek(compressedFilePosition, SeekOrigin.Begin);
                        bw.Write(uncompressedBuffer[uncompressedBufferPosition]);
                        compressedFilePosition++;

                        uncompressedBufferPosition++;
                        windowPosition++;
                    }

                    bitsToWrite++;

                    if (bitsToWrite >= 8)
                    {
                        bitsToWrite = 0;
                        compressedStream.Seek(currentControlCodePosition, SeekOrigin.Begin);
                        bw.Write(currentControlCode);

                        currentControlCodePosition = compressedFilePosition;
                        currentControlCode = 0;
                        compressedFilePosition++;
                    }
                    else
                    {
                        currentControlCode >>= 1;
                    }

                    if (uncompressedBufferPosition >= uncompressedBuffer.Length)
                    {
                        break;
                    }
                }

                for (int n = bitsToWrite; n < 7; n++) currentControlCode >>= 1;

                compressedStream.Seek(currentControlCodePosition, SeekOrigin.Begin);
                bw.Write(currentControlCode);

                return compressedStream.ToArray();
            }
        }

        #endregion
    }
}