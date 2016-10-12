using System.Runtime.InteropServices;

namespace SqlSnap.Core.Vdi
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct VDConfig
    {
        public int DeviceCount;
        public VDFeature Features;
        public int PrefixZoneSize;
        public int Alignment;
        public int SoftFileMarkBlockSize;
        public int EOMWarningSize;
        public int ServerTimeOut;
        public int BlockSize;
        public int MaxIODepth;
        public int MaxTransferSize;
        public int BufferAreaSize;
    }
}