using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MarioPartyTools
{
    class Table
    {
        readonly Dictionary<byte, string> hexToString = null;
        readonly StringBuilder sb;

        public Table(string tableFile)
        {
            if (!File.Exists(tableFile))
            {
                throw new FileNotFoundException($"Table file \"{tableFile}\" does not exist!", tableFile);
            }

            hexToString = new Dictionary<byte, string>();

            sb = new StringBuilder();

            string[] lines = File.ReadAllLines(tableFile);

            foreach (string line in lines)
            {
                if (line.Length == 0) continue;
                if (line.StartsWith(";")) continue;

                string[] parts = line.Split('=');

                if (parts.Length == 2)
                {
                    // Normal line with the format XX=X

                    if (parts[1] == "<" || parts[1] == ">")
                    {
                        throw new FormatException("The table can't contain any value that is \"<\" or \">\", as they are used as special characters to specify unknown codes.");
                    }

                    if (parts[1] == "\\")
                    {
                        throw new FormatException("The table can't contain any value that is only \"\\\", as it can be confused with special codes that start with \"\\\" like \"\\n\", used to indicate new line.");
                    }

                    byte hex = Convert.ToByte(parts[0], 16);
                    hexToString.Add(hex, parts[1]);
                }
                else if (parts.Length == 3)
                {
                    // Special case for "=" (XX==)

                    byte hex = Convert.ToByte(parts[0], 16);
                    hexToString.Add(hex, "=");
                }
                else
                {
                    // Ignore invalid lines

                    continue;
                }
            }
        }

        public string GetString(byte[] hexData)
        {
            sb.Clear();

            for (int h = 0; h < hexData.Length; h++)
            {
                if (h == hexData.Length - 1)
                {
                    if (hexData[h] != 0) throw new FormatException($"Expected 0x00 code at the end of the binary string: {sb}");
                    break;
                }

                sb.Append(HexToString(hexData[h]));
            }

            return sb.ToString();
        }

        public byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length + 1];
            int bytesPosition = 0;

            for (int s = 0; s < str.Length; s++)
            {
                string currentCharacter = str.Substring(s, 1);

                if (currentCharacter == "<")
                {
                    if (s > str.Length - 4) throw new FormatException($"Found \"<\" character at the end of the text, so there's no room to contain a full code in the format \"<XX>\". Text:\n\n{str}");
                    string closeCodeCharacter = str.Substring(s + 3, 1);
                    if (closeCodeCharacter != ">") throw new FormatException($"There's a malformed code in text:\n\n{str}");
                    string code = str.Substring(s + 1, 2);

                    bytes[bytesPosition++] = Convert.ToByte(code, 16);

                    s += 3;
                }
                else if (currentCharacter == "\\")
                {
                    if (s > str.Length - 2) throw new FormatException($"Found \"\\\" character at the end of the text, so there's no room to contain a full code in the format \"\\X\". Text:\n\n{str}");
                    string code = str.Substring(s, 2);

                    bool found = StringToHex(code, out byte hex);
                    if (found)
                    {
                        bytes[bytesPosition++] = hex;
                    }
                    else
                    {
                        throw new KeyNotFoundException($"\"{code}\" has not been found in the table.");
                    }

                    s++;
                }
                else
                {
                    bool found = StringToHex(currentCharacter, out byte hex);
                    if (found)
                    {
                        bytes[bytesPosition++] = hex;
                    }
                    else
                    {
                        throw new KeyNotFoundException($"\"{currentCharacter}\" has not been found in the table.");
                    }
                }
            }

            bytes[bytesPosition++] = 0; // Make sure there's an end of text code at the end;

            byte[] finalBytes = new byte[bytesPosition];
            Array.Copy(bytes, 0, finalBytes, 0, bytesPosition);
            return finalBytes;
        }

        string HexToString(byte hex)
        {
            bool found = hexToString.TryGetValue(hex, out string str);
            if (!found) str = $"<{hex:X2}>";
            return str;
        }

        bool StringToHex(string str, out byte hex)
        {
            foreach (KeyValuePair<byte, string> pair in hexToString)
            {
                if (pair.Value == str)
                {
                    hex = pair.Key;
                    return true;
                }
            }

            hex = 0;
            return false;
        }
    }
}