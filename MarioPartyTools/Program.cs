using MarioPartyCompression;
using System;
using System.Reflection;

namespace MarioPartyTools
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowHeader();

            try
            {
                if (args.Length == 2)
                {
                    if (args[0] == "-b")
                    {
                        ROM.Benchmark(args[1]);
                    }
                    else
                    {
                        ShowUsage();
                    }
                }
                else if (args.Length == 3)
                {
                    if (args[0] == "-d")
                    {
                        MPCompression.Decompress(args[1], args[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Decompression process finished successfully.\n");
                    }
                    else if (args[0] == "-c")
                    {
                        MPCompression.Compress(args[1], args[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Compression process finished successfully.\n");
                    }
                    else if (args[0] == "-me")
                    {
                        ROM.MassExtraction(args[1], args[2], false);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Mass extraction process finished successfully.\n");
                    }
                    else if (args[0] == "-md")
                    {
                        ROM.MassExtraction(args[1], args[2], true);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Mass decompression process finished successfully.\n");
                    }
                    else
                    {
                        ShowUsage();
                    }
                }
                else if (args.Length == 4)
                {
                    if (args[0] == "-et")
                    {
                        Table table = new Table(args[3]);
                        Texts.Extract(args[1], args[2], table);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Extracted all texts successfully.\n");
                    }
                    else if (args[0] == "-it")
                    {
                        Table table = new Table(args[3]);
                        Texts.Insert(args[1], args[2], table);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Inserted all texts successfully.\n");
                    }
                    else
                    {
                        ShowUsage();
                    }
                }
                else
                {
                    ShowUsage();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }

            Console.ForegroundColor = ConsoleColor.Gray;

#if DEBUG
            Console.ReadLine();
#endif
        }

        static void ShowHeader()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            string v = $"{version.Major}.{version.Minor}.{version.Build}";

            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine();
            Console.WriteLine("       #------------------------------------------------------------------------#");
            Console.WriteLine("       #                    Mario Party Tools - Version " + v + "                   #");
            Console.Write("       #   By PacoChan - ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("https://github.com/MaikelChan/MarioPartyCompression");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("    #");
            Console.WriteLine("       #------------------------------------------------------------------------#\n\n");
        }

        static void ShowUsage()
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("Mario Party Tools is a simple program that allow you to perform several");
            Console.WriteLine("actions to edit Mario Party data with the help of Mario Party Compression Library.");
            Console.WriteLine("It is compatible with all the versions of the game (NTSC-J, NTSC-U and PAL),");
            Console.WriteLine("but make sure that the ROM doesn't have swapped data, or else it won't work.\n\n");

            Console.WriteLine("Usage:\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -d <compressed_file> <uncompressed_file>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Decompress a Mario Party <compressed_file> and save it into <uncompressed_file>.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -c <uncompressed_file> <compressed_file>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Compress a Mario Party <uncompressed_file> and save it into <compressed_file>.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -et <rom_file> <output_path> <table_file>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Extract text data from <rom_file> and save it into <output_path>.");
            Console.WriteLine("    A <table_file> is needed to make the characters readable.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -it <input_path> <rom_file> <table_file>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Insert text data from <input_path> into <rom_file>. A <table_file> is needed");
            Console.WriteLine("    to convert back all characters so the game can show them properly.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -me <rom_file> <output_path>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Mass extract the main data block of <rom_file> and save all the files");
            Console.WriteLine("    into <output_path>.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -md <rom_file> <output_path>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Mass extract and decompress the main data block of <rom_file> and save");
            Console.WriteLine("    all the files into <output_path>. Uncompressed files will be extracted as is.\n\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  MarioPartyTools -b <rom_file>\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("    Test that will try to decompress and recompress all compressed data in <rom_file>,");
            Console.WriteLine("    show the compression ratio compared to the original data, check if there are");
            Console.WriteLine("    compression errors, and show how much it took to do all that.\n\n");
        }
    }
}