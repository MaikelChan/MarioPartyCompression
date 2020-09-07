using System;
using System.IO;

namespace MarioPartyTools
{
    static class Texts
    {
        public static void Extract(string romFile, string outputPath, Table table)
        {
            if (string.IsNullOrEmpty(romFile))
            {
                throw new ArgumentNullException(nameof(romFile));
            }

            if (!File.Exists(romFile))
            {
                throw new FileNotFoundException($"File \"{romFile}\" does not exist!", romFile);
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

            using (FileStream romStream = File.OpenRead(romFile))
            using (BinaryReader br = new BinaryReader(romStream))
            {
                ROM.Info rom = ROM.GetInfo(romStream);

                // Start reading texts for each language

                for (int l = 0; l < rom.languages.Length; l++)
                {
                    uint textsStartPosition = rom.languages[l].textsStartPosition;

                    romStream.Seek(textsStartPosition, SeekOrigin.Begin);

                    uint textsCount = Utils.SwapU32(br.ReadUInt32());
                    uint[] textPointers = new uint[textsCount];

                    for (int t = 0; t < textsCount; t++)
                    {
                        textPointers[t] = Utils.SwapU32(br.ReadUInt32());
                    }

                    string textsFile = Path.Combine(outputPath, $"{l}-{rom.languages[l].languageName}.txt");

                    using (StreamWriter textsStream = File.CreateText(textsFile))
                    {
                        for (int t = 0; t < textsCount; t++)
                        {
                            romStream.Seek(textsStartPosition + textPointers[t], SeekOrigin.Begin);

                            ushort textLength = Utils.SwapU16(br.ReadUInt16());
                            byte[] textBytes = new byte[textLength];

                            romStream.Read(textBytes, 0, textBytes.Length);

                            textsStream.WriteLine(table.GetString(textBytes));
                        }
                    }
                }
            }
        }

        public static void Insert(string inputPath, string romFile, Table table)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            if (!Directory.Exists(inputPath))
            {
                throw new DirectoryNotFoundException($"Directory \"{inputPath}\" does not exist!");
            }

            if (string.IsNullOrEmpty(romFile))
            {
                throw new ArgumentNullException(nameof(romFile));
            }

            if (!File.Exists(romFile))
            {
                throw new FileNotFoundException($"File \"{romFile}\" does not exist!", romFile);
            }

            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            string[] textFiles = Directory.GetFiles(inputPath, "*.txt");

            using (FileStream romStream = new FileStream(romFile, FileMode.Open, FileAccess.ReadWrite))
            {
                ROM.Info rom = ROM.GetInfo(romStream);

                // Check if the provided number of text files corresponds to the number of languages of the ROM

                if (textFiles.Length != rom.languages.Length)
                {
                    throw new ArgumentOutOfRangeException("The number of text files found in the provided path does not correspong to the number of languages of the provided ROM.");
                }

                // Start processing texts for each language

                uint textsCount = rom.numberOfTexts;

                for (int l = 0; l < rom.languages.Length; l++)
                {
                    string[] lines = File.ReadAllLines(textFiles[l]);

                    if (lines.Length != textsCount)
                    {
                        throw new InvalidDataException($"The number of lines in \"{textFiles[l]}\" has to be {textsCount} for the {rom.regionName} ROM.");
                    }

                    using (MemoryStream ms = new MemoryStream())
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        bw.Write(Utils.SwapU32(textsCount));

                        uint dataPosition = (textsCount * 4) + 4;
                        uint pointersPosition = 4;

                        foreach (string line in lines)
                        {
                            if (line.Length == 0)
                            {
                                throw new InvalidDataException("There shouldn't be empty lines in \"{textFiles[l]}\".");
                            }

                            byte[] bytes = table.GetBytes(line);

                            ms.Seek(dataPosition, SeekOrigin.Begin);
                            bw.Write(Utils.SwapU16((ushort)bytes.Length));
                            ms.Write(bytes, 0, bytes.Length);
                            ms.Seek(pointersPosition, SeekOrigin.Begin);
                            bw.Write(Utils.SwapU32(dataPosition));

                            dataPosition += (uint)(bytes.Length) + 2;
                            if ((dataPosition & 1) == 1) dataPosition++;
                            pointersPosition += 4;
                        }

                        uint availableSpace = rom.languages[l].textsEndPosition - rom.languages[l].textsStartPosition;

                        if (ms.Length > availableSpace)
                        {
                            throw new IndexOutOfRangeException($"The amount of text in \"{textFiles[l]}\" does not fit the available text space in the ROM.");
                        }

                        ms.Seek(0, SeekOrigin.Begin);
                        romStream.Seek(rom.languages[l].textsStartPosition, SeekOrigin.Begin);
                        ms.CopyTo(romStream);

                        // Fill with zeroes the remaining available space

                        byte[] zeroes = new byte[availableSpace - ms.Length];
                        romStream.Write(zeroes, 0, zeroes.Length);
                    }
                }
            }
        }

        //static uint GetTextsStartPositionFromCodePointers(BinaryReader br, ROM.Info rom)
        //{
        //    br.BaseStream.Seek(rom.textsStartPointerOpcode1Position, SeekOrigin.Begin);
        //    uint opcode1 = Utils.SwapU32(br.ReadUInt32());
        //    br.BaseStream.Seek(rom.textsStartPointerOpcode2Position, SeekOrigin.Begin);
        //    uint opcode2 = Utils.SwapU32(br.ReadUInt32());
        //    return (uint)((opcode1 << 16) + (short)(opcode2 & 0xffff));
        //}
    }
}