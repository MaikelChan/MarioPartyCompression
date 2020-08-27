
namespace MarioPartyCompression
{
    internal static class Utils
    {
        internal static ushort SwapU16(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
        }

        internal static uint SwapU32(uint value)
        {
            return (value << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | (value >> 24);
        }
    }
}