# Mario Party Compression Library
Mario Party Compression is a library that allows you to compress and decompress data of *Mario Party* for *Nintendo 64*. It was done in 2011 for my [Mario Party Spanish translation project](http://www.romhacking.net/translations/1648/), but I never got to clean up the code and release it up until now. It's able to provide slightly better compression than the original one used on the official release.

I reversed engineered the compression format myself back in the day, but as much as I would like to document it, it's been so much time since then that I barely remember anything :(. 

## Requirements
This library is written in C# compatible with .NET Standard 2.0 specification, so it is usable in multiple platforms like desktops and phones.

## Changelog
### [1.0.0] - 2020-08-28
- Initial release.

# Mario Party Tools
It is a simple command line program that allows you to decompress or compress a file by using the Mario Party Compression Libray.

It also has a benchmark feature that allows you to check that the compression library works correctly. It will decompress and recompress all compressed data in a *Mario Party* ROM, show the compression ratio compared to the original data, check if there are compression errors, and show how much it takes to do all that. It is compatible with all the versions of the game (NTSC-J, NTSC-U and PAL), but make sure that the ROM doesn't have swapped data, or else the process will fail.

## Requirements
The program requires the .NET Framework 4.7.2.

## Usage
```
MarioPartyTools -d <compressed_file> <uncompressed_file>  :  Decompress a Mario Party file
MarioPartyTools -c <uncompressed_file> <compressed_file>  :  Compress a Mario Party file
MarioPartyTools -b -r<rom_region> <rom_file>              :  Execute some benchmarks and tests

  The benchmark command will try to decompress and recompress all compressed data
  in a Mario Party ROM, show the compression ratio compared to the original data,
  check if there are compression errors, and show how much it took to do all that.
  Also make sure that the ROM is not swapped, or else the process will fail.

  rom_region: the benchmark is compatible with all versions of Mario Party.
              Use -rP to specify the PAL version, -rU for the NTSC-U version,
              and -rJ for the NTSC-J version.
```

## Changelog
### [1.0.0] - 2020-08-28
- Initial release.