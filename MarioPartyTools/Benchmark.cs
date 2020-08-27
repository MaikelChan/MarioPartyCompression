using MarioPartyCompression;
using System;
using System.Diagnostics;
using System.IO;

namespace MarioPartyTools
{
    static class Benchmark
    {
        public enum RomRegions { NtscU, Pal, NtscJ }

        public static void Start(string romFilePath, RomRegions romRegion)
        {
            if (!File.Exists(romFilePath))
            {
                throw new FileNotFoundException($"File \"{romFilePath}\" does not exist!", romFilePath);
            }

            float compressionRatio = 0f;
            int processedFiles = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            uint dataStartPosition;
            uint dataEndPosition;

            if (romRegion == RomRegions.NtscU)
            {
                dataStartPosition = 0x31c7e0;
                dataEndPosition = 0xfcb860;
            }
            else if (romRegion == RomRegions.Pal)
            {
                dataStartPosition = 0x3373c0;
                dataEndPosition = 0xff0850;
            }
            else
            {
                dataStartPosition = 0x31ba80;
                dataEndPosition = 0xfb47a0;
            }

            using (FileStream romStream = File.OpenRead(romFilePath))
            using (BinaryReader br = new BinaryReader(romStream))
            {
                romStream.Seek(dataStartPosition, SeekOrigin.Begin);

                uint fileCount = SwapU32(br.ReadUInt32());
                uint[] filePointers = new uint[fileCount];

                for (int f = 0; f < fileCount; f++)
                {
                    filePointers[f] = SwapU32(br.ReadUInt32());
                }

                for (int f = 0; f < fileCount; f++)
                {
                    romStream.Seek(filePointers[f] + dataStartPosition, SeekOrigin.Begin);

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
                                subFileLength = (filePointers[f + 1] + dataStartPosition) - (filePointers[f] + subFilePointers[sf] + dataStartPosition);
                            else
                                subFileLength = dataEndPosition - (filePointers[f] + subFilePointers[sf] + dataStartPosition);
                        }

                        uint subFilePosition = filePointers[f] + subFilePointers[sf] + dataStartPosition;
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
            Console.WriteLine($"\nDone! Compression ratio: {compressionRatio}. Total time: {sw.ElapsedMilliseconds}ms.\n");
        }

        static bool ProcessFile(byte[] compressedBuffer, uint offset, out float compressionRatio)
        {
            compressionRatio = 0f;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"Processing data at 0x{offset:X8}...");

            byte[] uncompressedBuffer;

            try
            {
                uncompressedBuffer = MPCompression.Decompress(compressedBuffer);
            }
            catch (FormatException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error processing data at 0x{offset:X8}: {ex.Message}");
                return false;
            }

            byte[] compressedBuffer2 = MPCompression.Compress(uncompressedBuffer);
            byte[] uncompressedBuffer2 = MPCompression.Decompress(compressedBuffer2);

            bool areEqual = CompareArrays(uncompressedBuffer, uncompressedBuffer2);
            if (!areEqual)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Data at 0x{offset:X8} decompression mismatch...");
                return false;
            }

            compressionRatio = (float)compressedBuffer.Length / compressedBuffer2.Length;

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

        static uint SwapU32(uint value)
        {
            return (value << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | (value >> 24);
        }
    }
}
