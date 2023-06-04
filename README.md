# Mario Party Compression Library
Mario Party Compression is a library that allows you to compress and decompress data of *Mario Party* for *Nintendo 64*. It was done in 2011 for my [Mario Party Spanish translation project](http://www.romhacking.net/translations/1648/), but I never got to clean up the code and release it up until now. It's able to provide slightly better compression than the original one used on the official release.

## Requirements
This library is written in C# compatible with .NET Standard 2.0 specification, so it is usable in multiple platforms like desktops and phones.

# Mario Party Tools
It is a simple program that allow you to perform several actions to edit *Mario Party* data with the help of Mario Party Compression Library. It is compatible with all the versions of the game (NTSC-J, NTSC-U and PAL), but make sure that the ROM doesn't have swapped data, or else it won't work.

## Requirements
The program requires the [.NET Desktop Runtime 6](https://dotnet.microsoft.com/download/dotnet/6.0) and it works on Windows, Linux and MacOS.

## Usage
```
MarioPartyTools -d <compressed_file> <uncompressed_file>

  Decompress a Mario Party <compressed_file> and save it into <uncompressed_file>.


MarioPartyTools -c <uncompressed_file> <compressed_file>

  Compress a Mario Party <uncompressed_file> and save it into <compressed_file>.


MarioPartyTools -et <rom_file> <output_path> <table_file>

  Extract text data from <rom_file> and save it into <output_path>.
  A <table_file> is needed to make the characters readable.


MarioPartyTools -it <input_path> <rom_file> <table_file>

  Insert text data from <input_path> into <rom_file>. A <table_file> is needed
  to convert back all characters so the game can show them properly.


MarioPartyTools -me <rom_file> <output_path>

  Mass extract the main data block of <rom_file> and save all the files
  into <output_path>.


MarioPartyTools -md <rom_file> <output_path>

  Mass extract and decompress the main data block of <rom_file> and save
  all the files into <output_path>. Uncompressed files will be extracted as is.


MarioPartyTools -b <rom_file>

  Test that will try to decompress and recompress all compressed data in <rom_file>,
  show the compression ratio compared to the original data, check if there are
  compression errors, and show how much it took to do all that.
```

# Specification
Mario Party contains a big data block that contains a lot of interesting stuff, like fonts and textures. This data block is divided into multiple files, which they also contain multiple sub-files each. The position in the ROM of this data block varies between versions of the game. Here's a table:

| Region |         ROM CRCs         | Data Start Position | Data End Position |
|:------:|:------------------------:|:-------------------:|:-----------------:|
| NTSC-J | 0xada815be<br>0x6028622f |       0x31ba80      |      0xfb47a0     |
| NTSC-U | 0x2829657e<br>0xa0621877 |       0x31c7e0      |      0xfcb860     |
|   PAL  | 0x9c663069<br>0x80f24a80 |       0x3373c0      |      0xff0850     |

The data block has a simple structure. It starts with 4 bytes that indicates the number of files, then there is a pointer table, with 4 byte pointers that point to the beginning of a file, and then that file is another container with similar structure that contains several sub-files. Hopefully I can explain it better with a bit of pseudo-code.

```c++
struct File
{
  int subFileCount;
  int pointers[subFileCount];
  char subFilesData[subFileCount][];
}

struct DataBlock
{
  int fileCount;
  int pointers[fileCount];
  File files[fileCount];
}
```

Most of the subfiles are compressed and can be decompressed with the Mario Party Compression Library. The rest of the subfiles are either uncompressed or maybe they are in another compression format. Not sure there.

The compressed subfiles that the library supports start with 4 bytes indicating the uncompressed size of the subfile, followed by another 4 bytes that are always 0x00000001.

Regarding the compression format itself, I reverse engineered it back in the day when I did the Spanish translation of the game, but as much as I would like to document it, it's been so much time since then that I barely remember anything :disappointed:. But hopefully you can learn more from the code.