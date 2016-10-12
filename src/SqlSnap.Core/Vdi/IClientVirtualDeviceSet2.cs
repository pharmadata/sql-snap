using System;
using System.Runtime.InteropServices;

namespace SqlSnap.Core.Vdi
{
    [ComImport, Guid("d0e6eb07-7a62-11d2-8573-00c04fc21759"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IClientVirtualDeviceSet2
    {
        void Create([In] [MarshalAs(UnmanagedType.LPWStr)] string name,
            [In] [MarshalAs(UnmanagedType.Struct)] ref VDConfig config);

        void GetConfiguration([In] int timeout,
            [In] [MarshalAs(UnmanagedType.Struct)] ref VDConfig config);

        [return: MarshalAs(UnmanagedType.Interface)]
        IClientVirtualDevice OpenDevice(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string name);

        void Close();

        void SignalAbort();

        void OpenInSecondary(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string setName);

        void GetBufferHandle(
            [In] IntPtr buffer,
            [Out] IntPtr bufferHandle);

        void MapBufferHandle(
            [In] IntPtr buffer,
            [Out] IntPtr outBuffer);

        void CreateEx([In] [MarshalAs(UnmanagedType.LPWStr)] string instanceName,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string name,
            [In] [MarshalAs(UnmanagedType.Struct)] ref VDConfig config);
    }
}