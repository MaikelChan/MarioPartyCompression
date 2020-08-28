using MarioPartyCompression;
using System;
using System.Diagnostics;
using System.IO;

namespace MarioPartyTools
{
    static class Benchmark
    {
        static readonly byte[][] RomCRCs =
        {
            new byte[] { 0xad, 0xa8, 0x15, 0xbe, 0x60, 0x28, 0x62, 0x2f }, // NTSC-J
            new byte[] { 0x28, 0x29, 0x65, 0x7e, 0xa0, 0x62, 0x18, 0x77 }, // NTSC-U
            new byte[] { 0x9c, 0x66, 0x30, 0x69, 0x80, 0xf2, 0x4a, 0x80 }  // PAL
        };

        static readonly RomData[] romData =
        {
            new RomData() { startPosition = 0x31ba80, endPosition = 0xfb47a0 }, // NTSC-J
            new RomData() { startPosition = 0x31c7e0, endPosition = 0xfcb860 }, // NTSC-U
            new RomData() { startPosition = 0x3373c0, endPosition = 0xff0850 }  // PAL
        };

        struct RomData
        {
            public uint startPosition;
            public uint endPosition;
        }

        public static void Start(string romFilePath)
        {
            if (string.IsNullOrEmpty(romFilePath))
            {
                throw new ArgumentNullException("romFilePath");
            }

            if (!File.Exists(romFilePath))
            {
                throw new FileNotFoundException($"File \"{romFilePath}\" does not exist!", romFilePath);
            }

            float compressionRatio = 0f;
            int processedFiles = 0;
            Stopwatch sw = new Stopwatch();

            using (FileStream romStream = File.OpenRead(romFilePath))
            using (BinaryReader br = new BinaryReader(romStream))
            {
                // Check if it is a valid ROM

                romStream.Seek(0x10, SeekOrigin.Begin);
                byte[] romCRC = new byte[0x8];
                romStream.Read(romCRC, 0, romCRC.Length);
                byte[] swappedRomCRC = SwapCRCBytes(romCRC);

                int romRegion = -1;

                for (int r = 0; r < RomCRCs.Length; r++)
                {
                    if (CompareArrays(RomCRCs[r], swappedRomCRC))
                    {
                        throw new FormatException("The Mario Party ROM has swapped data. Please unswap it using another tool first.");
                    }

                    if (CompareArrays(RomCRCs[r], romCRC))
                    {
                        romRegion = r;
                        break;
                    }
                }

                if (romRegion < 0)
                {
                    throw new FormatException("The file is not a Mario Party ROM for N64. Please make sure you've specified the correct game.");
                }

                // Start reading the ROM data

                sw.Start();

                romStream.Seek(romData[romRegion].startPosition, SeekOrigin.Begin);

                uint fileCount = SwapU32(br.ReadUInt32());
                uint[] filePointers = new uint[fileCount];

                for (int f = 0; f < fileCount; f++)
                {
                    filePointers[f] = SwapU32(br.ReadUInt32());
                }

                for (int f = 0; f < fileCount; f++)
                {
                    romStream.Seek(filePointers[f] + romData[romRegion].startPosition, SeekOrigin.Begin);

                    uint subFileCount = SwapU32(br.ReadUInt32());
                    uint[] subFilePointers = new uint[subFileCount];

                    for (int sf = 0; sf < subFileCount; sf++)
                    {
                        subFilePointers[sf] = SwapU32(br.ReadUInt32());
                    }

                    for (int sf = 0; sf < subFileCount; sf++)
                    {
                        uint subFileLength;

                        if (sf < subFileCount - 1)
                        {
                            subFileLength = subFilePointers[sf + 1] - subFilePointers[sf];
                        }
                        else
                        {
                            if (f < fileCount - 1)
                                subFileLength = (filePointers[f + 1] + romData[romRegion].startPosition) - (filePointers[f] + subFilePointers[sf] + romData[romRegion].startPosition);
                            else
                                subFileLength = romData[romRegion].endPosition - (filePointers[f] + subFilePointers[sf] + romData[romRegion].startPosition);
                        }

                        uint subFilePosition = filePointers[f] + subFilePointers[sf] + romData[romRegion].startPosition;
                        romStream.Seek(subFilePosition, SeekOrigin.Begin);
                        byte[] data = new byte[subFileLength];
                        romStream.Read(data, 0, data.Length);

                        bool success = ProcessFile(data, subFilePosition, out float ratio);
                        if (success)
                        {
                            compressionRatio += ratio;
                            processedFiles++;
                        }
                    }
                }
            }

            sw.Stop();

            compressionRatio /= processedFiles;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nProcess finished! Compression ratio average: {compressionRatio}. Elapsed time: {sw.ElapsedMilliseconds}ms.\n");
        }

        static bool ProcessFile(byte[] compressedBuffer, uint offset, out float compressionRatio)
        {
            compressionRatio = 0f;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"Processing data at 0x{offset:X8}... ");

            byte[] uncompressedBuffer;

            try
            {
                uncompressedBuffer = MPCompression.Decompress(compressedBuffer);
            }
            catch (FormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                return false;
            }

            byte[] compressedBuffer2 = MPCompression.Compress(uncompressedBuffer);
            byte[] uncompressedBuffer2 = MPCompression.Decompress(compressedBuffer2);

            bool areEqual = CompareArrays(uncompressedBuffer, uncompressedBuffer2);
            if (!areEqual)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Decompression mismatch.");
                return false;
            }

            compressionRatio = (float)compressedBuffer2.Length / compressedBuffer.Length;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Done! Compression ratio: {compressionRatio}");

            return true;
        }

        static bool CompareArrays(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;

            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i]) return false;
            }

            return true;
        }

        static byte[] SwapCRCBytes(byte[] bytes)
        {
            if (bytes.Length != 8)
            {
                throw new ArgumentException("The bytes array must have 8 elements.", "bytes");
            }

            byte[] swappedBytes = new byte[8];

            for (int b = 0; b < bytes.Length; b += 2)
            {
                swappedBytes[b] = bytes[b + 1];
                swappedBytes[b + 1] = bytes[b];
            }

            return swappedBytes;
        }

        static uint SwapU32(uint value)
        {
            return (value << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | (value >> 24);
        }
    }
}
