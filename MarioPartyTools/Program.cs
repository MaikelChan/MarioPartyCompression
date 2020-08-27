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

            if (args.Length == 3)
            {
                if (args[0] == "-d")
                {
                    try
                    {
                        MPCompression.Decompress(args[1], args[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Decompression process finished successfully.\n");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                    }
                }
                else if (args[0] == "-c")
                {
                    try
                    {
                        MPCompression.Compress(args[1], args[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Compression process finished successfully.\n");
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.Message);
                    }
                }
                else if (args[0] == "-b")
                {
                    if (args[1] == "-rU")
                    {
                        Benchmark.Start(args[2], Benchmark.RomRegions.NtscU);
                    }
                    else if (args[1] == "-rP")
                    {
                        Benchmark.Start(args[2], Benchmark.RomRegions.Pal);
                    }
                    else if (args[1] == "-rJ")
                    {
                        Benchmark.Start(args[2], Benchmark.RomRegions.NtscJ);
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
            else
            {
                ShowUsage();
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
            Console.WriteLine("       #                    Mario Party Tool - Version " + v + "                    #");
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

            Console.WriteLine("Usage:\n");

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("  MarioPartyTools -d <compressed_file> <uncompressed_file>  :  Decompress a Mario Party file");
            Console.WriteLine("  MarioPartyTools -c <uncompressed_file> <compressed_file>  :  Compress a Mario Party file");
            Console.WriteLine("  MarioPartyTools -b -r<rom_region> <rom_file>              :  Execute some benchmarks and tests\n");

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.WriteLine("    The benchmark command will try to decompress and recompress all compressed data");
            Console.WriteLine("    in a Mario Party ROM, show the compression ratio compared to the original data,");
            Console.WriteLine("    check if there are compression errors, and show how much it took to do all that.");
            Console.WriteLine("    Also make sure that the ROM is not swapped, or else the process will fail.\n");

            Console.WriteLine("    rom_region: the benchmark is compatible with all versions of Mario Party.");
            Console.WriteLine("                Use -rP to specify the PAL version, -rU for the NTSC-U version,");
            Console.WriteLine("                and -rJ for the NTSC-J version.\n");
        }
    }
}