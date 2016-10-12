using System;
using System.Runtime.InteropServices;

namespace SqlSnap.Core.Vdi
{
    [ComImport, Guid("40700424-0080-11d2-851f-00c04fc21759"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClientVirtualDevice
    {
        IntPtr GetCommand(
            [In] int timeout);

        void CompleteCommand(
            [In] IntPtr cmd,
            [In] int completionCode,
            [In] int bytesTransferred,
            [In] long position);
    }
}