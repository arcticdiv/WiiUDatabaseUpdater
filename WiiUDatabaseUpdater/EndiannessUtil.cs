using System.Net;

namespace WiiUDatabaseUpdater
{
    public static class EndiannessUtil
    {
        public static ushort SwapEndianness(this ushort value)
        {
            return (ushort)IPAddress.HostToNetworkOrder((short)value);
        }

        public static uint SwapEndianness(this uint value)
        {
            return (uint)IPAddress.HostToNetworkOrder((int)value);
        }

        public static ulong SwapEndianness(this ulong value)
        {
            return (ulong)IPAddress.HostToNetworkOrder((long)value);
        }
    }
}
