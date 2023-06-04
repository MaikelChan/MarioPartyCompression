using MarioPartyCompression;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MarioPartyTools;

static class ROM
{
    delegate void ProcessFileDelegate(uint dataPosition, uint filePosition, uint subFilePosition, byte[] subFileData);

    public enum Types { NTSC_J, NTSC_U, PAL, Swapped, Invalid }

    public struct Info
    {
        public string regionName;
        public byte[] crc;
        public uint dataStartPosition;
        public uint dataEndPosition;
        //public uint textsStartPointerOpcode1Position;
        //public uint textsStartPointerOpcode2Position;
        public uint numberOfTexts;
        public Language[] languages;
    }

    public struct Language
    {
        public string languageName;
        public uint textsStartPosition;
        public uint textsEndPosition;
    }

    static readonly Info[] info =
    {
        new Info()
        {
            regionName = "NTSC-J",
            crc = new byte[] { 0xad, 0xa8, 0x15, 0xbe, 0x60, 0x28, 0x62, 0x2f },
            dataStartPosition = 0x31ba80,
            dataEndPosition = 0xfb47a0,
            //textsStartPointerOpcode1Position = 0x1ad9c, // Pointer to texts is hardcoded in two opcodes
            //textsStartPointerOpcode2Position = 0x1ada4,
            numberOfTexts = 0x52C,
            languages = new Language[]
            {
                new Language()
                {
                    languageName = "Japanese",
                    textsStartPosition = 0xfb47a0,
                    textsEndPosition = 0xfc4930
                }
            }
        },
        new Info()
        {
            regionName = "NTSC-U",
            crc = new byte[] { 0x28, 0x29, 0x65, 0x7e, 0xa0, 0x62, 0x18, 0x77 },
            dataStartPosition = 0x31c7e0,
            dataEndPosition = 0xfcb860,
            //textsStartPointerOpcode1Position = 0x1ae6c,
            //textsStartPointerOpcode2Position = 0x1ae74,
            numberOfTexts = 0x52F,
            languages = new Language[]
            {
                new Language()
                {
                    languageName = "English",
                    textsStartPosition = 0xfcb860,
                    textsEndPosition = 0xfe2310
                }
            }
        },
        new Info()
        {
            regionName = "PAL",
            crc = new byte[] { 0x9c, 0x66, 0x30, 0x69, 0x80, 0xf2, 0x4a, 0x80 },
            dataStartPosition = 0x3373c0,
            dataEndPosition = 0xff0850,
            //textsStartPointerOpcode1Position = 0x1ba30,
            //textsStartPointerOpcode2Position = 0x1ba34,
            numberOfTexts = 0x52F,
            languages = new Language[]
            {
                new Language()
                {
                    languageName = "English",
                    textsStartPosition = 0xff0850,
                    textsEndPosition = 0x1007310
                },
                new Language()
                {
                    languageName = "German",
                    textsStartPosition = 0x1007310,
                    textsEndPosition = 0x101f110
                },
                new Language()
                {
                    languageName = "French",
                    textsStartPosition = 0x101f110,
                    textsEndPosition = 0x10357d0
                }
            }
        }
    };

    public static Types GetType(Stream romStream)
    {
        romStream.Seek(0x10, SeekOrigin.Begin);
        byte[] romCRC = new byte[0x8];
        romStream.Read(romCRC, 0, romCRC.Length);
        byte[] swappedRomCRC = SwapCRCBytes(romCRC);

        for (int r = 0; r < info.Length; r++)
        {
            if (Utils.CompareArrays(info[r].crc, swappedRomCRC))
            {
                return Types.Swapped;
            }

            if (Utils.CompareArrays(info[r].crc, romCRC))
            {
                return (Types)r;
            }
        }

        return Types.Invalid;
    }

    public static Info GetInfo(Stream romStream)
    {
        Types type = GetType(romStream);

        if (type == Types.Swapped)
            throw new InvalidDataException("The Mario Party ROM has swapped data. Please unswap it using another tool first.");
        else if (type == Types.Invalid)
            throw new InvalidDataException("The file is not a Mario Party ROM for N64. Please make sure you've specified the correct game.");

        return info[(int)type];
    }

    #region Mass Extraction

    public static void MassExtraction(string romFilePath, string outputPath, bool decompress)
    {
        if (string.IsNullOrEmpty(romFilePath))
        {
            throw new ArgumentNullException(nameof(romFilePath));
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentNullException(nameof(outputPath));
        }

        if (!File.Exists(romFilePath))
        {
            throw new FileNotFoundException($"File \"{romFilePath}\" does not exist!", romFilePath);
        }

        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        using (FileStream romStream = File.OpenRead(romFilePath))
        {
            ProcessData(romStream, (dataPosition, filePosition, subFilePosition, fileData) =>
            {
                uint subFileAbsolutePosition = dataPosition + filePosition + subFilePosition;

                if (decompress)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"Decompressing data at 0x{subFileAbsolutePosition:X8}... ");

                    string filePath = Path.Combine(outputPath, filePosition.ToString("X8"));
                    if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

                    byte[]? uncompressedData = null;

                    try
                    {
                        uncompressedData = MPCompression.Decompress(fileData);
                    }
                    catch (InvalidDataException)
                    {

                    }

                    if (uncompressedData != null)
                    {
                        string subFilePath = Path.Combine(filePath, subFilePosition.ToString("X8"));
                        File.WriteAllBytes(subFilePath, uncompressedData);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Done!");
                    }
                    else
                    {
                        string subFilePath = Path.Combine(filePath, subFilePosition.ToString("X8"));
                        File.WriteAllBytes(subFilePath, fileData);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Not valid compressed data. Extracted as is.");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write($"Extracting data at 0x{subFileAbsolutePosition:X8}... ");

                    string filePath = Path.Combine(outputPath, filePosition.ToString("X8"));
                    if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

                    string subFilePath = Path.Combine(filePath, subFilePosition.ToString("X8"));
                    File.WriteAllBytes(subFilePath, fileData);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Done!");
                }
            });
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nProcess finished!\n");
    }

    #endregion

    #region Benchmark

    public static void Benchmark(string romFilePath)
    {
        if (string.IsNullOrEmpty(romFilePath))
        {
            throw new ArgumentNullException(nameof(romFilePath));
        }

        if (!File.Exists(romFilePath))
        {
            throw new FileNotFoundException($"File \"{romFilePath}\" does not exist!", romFilePath);
        }

        float compressionRatio = 0f;
        int processedFiles = 0;
        Stopwatch sw = new Stopwatch();
        sw.Start();

        using (FileStream romStream = File.OpenRead(romFilePath))
        {
            ProcessData(romStream, (dataPosition, filePosition, subFilePosition, fileData) =>
            {
                uint subFileAbsolutePosition = dataPosition + filePosition + subFilePosition;
                bool success = BenchmarkFile(fileData, subFileAbsolutePosition, out float ratio);
                if (success)
                {
                    compressionRatio += ratio;
                    processedFiles++;
                }
            });
        }

        sw.Stop();

        compressionRatio /= processedFiles;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nProcess finished! Compression ratio average: {compressionRatio}. Elapsed time: {sw.ElapsedMilliseconds}ms.\n");
    }

    static bool BenchmarkFile(byte[] compressedBuffer, uint offset, out float compressionRatio)
    {
        compressionRatio = 0f;

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"Processing data at 0x{offset:X8}... ");

        byte[] uncompressedBuffer;

        try
        {
            uncompressedBuffer = MPCompression.Decompress(compressedBuffer);
        }
        catch (InvalidDataException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            return false;
        }

        byte[] compressedBuffer2 = MPCompression.Compress(uncompressedBuffer);
        byte[] uncompressedBuffer2 = MPCompression.Decompress(compressedBuffer2);

        bool areEqual = Utils.CompareArrays(uncompressedBuffer, uncompressedBuffer2);
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

    #endregion

    static void ProcessData(Stream romStream, ProcessFileDelegate processFileData)
    {
        if (romStream == null)
        {
            throw new ArgumentNullException(nameof(romStream));
        }

        using (BinaryReader br = new BinaryReader(romStream, Encoding.UTF8, true))
        {
            Info rom = GetInfo(romStream);

            // Start reading the ROM data

            romStream.Seek(rom.dataStartPosition, SeekOrigin.Begin);

            uint fileCount = Utils.SwapU32(br.ReadUInt32());
            uint[] filePointers = new uint[fileCount];

            for (int f = 0; f < fileCount; f++)
            {
                filePointers[f] = Utils.SwapU32(br.ReadUInt32());
            }

            for (int f = 0; f < fileCount; f++)
            {
                uint filePosition = rom.dataStartPosition + filePointers[f];
                romStream.Seek(filePosition, SeekOrigin.Begin);

                uint subFileCount = Utils.SwapU32(br.ReadUInt32());
                uint[] subFilePointers = new uint[subFileCount];

                for (int sf = 0; sf < subFileCount; sf++)
                {
                    subFilePointers[sf] = Utils.SwapU32(br.ReadUInt32());
                }

                for (int sf = 0; sf < subFileCount; sf++)
                {
                    uint subFilePosition = filePosition + subFilePointers[sf];

                    uint subFileLength;

                    if (sf < subFileCount - 1)
                    {
                        subFileLength = subFilePointers[sf + 1] - subFilePointers[sf];
                    }
                    else
                    {
                        if (f < fileCount - 1)
                            subFileLength = (rom.dataStartPosition + filePointers[f + 1]) - subFilePosition;
                        else
                            subFileLength = rom.dataEndPosition - subFilePosition;
                    }

                    romStream.Seek(subFilePosition, SeekOrigin.Begin);
                    byte[] data = new byte[subFileLength];
                    romStream.Read(data, 0, data.Length);

                    processFileData?.Invoke(rom.dataStartPosition, filePointers[f], subFilePointers[sf], data);
                }
            }
        }
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
}