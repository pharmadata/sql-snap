using System;
using System.Runtime.InteropServices;

namespace SqlSnap.Core.Vdi
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct VDC_Command
    {
        public VDCommandCode CommandCode;
        public int Size;
        public long Position;
        public IntPtr Buffer;
    }
}