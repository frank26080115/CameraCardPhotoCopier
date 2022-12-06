using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UsbEject.Library;

namespace BucketDesktop
{
    /// <summary>
    /// this class exists purely for the card eject functionality
    /// </summary>
    public static class DiskUtils
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr SecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static IntPtr handle = IntPtr.Zero;

        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const int FILE_SHARE_READ = 0x1;
        const int FILE_SHARE_WRITE = 0x2;
        const int FSCTL_LOCK_VOLUME = 0x00090018;
        const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
        const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
        const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

        public static bool Eject(string driveLetter)
        {
#if false
            IntPtr p = USBEject(driveLetter);
            if (p != IntPtr.Zero)
            {
                return Eject(p);
            }
            else
            {
                return false;
            }
#else
            VolumeDeviceClass volumeDeviceClass = new VolumeDeviceClass();
            foreach (Volume device in volumeDeviceClass.Devices)
            {
                // is this volume a logical disk?
                if ((device.LogicalDrive == null) || (device.LogicalDrive.Length == 0))
                {
                    continue;
                }

                if (device.LogicalDrive.ToLower()[0] == driveLetter.ToLower()[0])
                {
                    device.Eject(true); // allow Windows to display any relevant UI
                    device.Eject(false);
                }
            }
            return true;
#endif
        }
        /// <summary>
        /// Constructor for the USBEject class
        /// </summary>
        /// <param name="driveLetter">This should be the drive letter. Format: F:/, C:/..</param>

        public static IntPtr USBEject(string driveLetter)
        {
            string filename = @"\\.\" + driveLetter[0] + ":";
            return CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, 0x3, 0, IntPtr.Zero);
        }

        public static bool Eject(IntPtr handle)
        {
            bool result = false;

            if (LockVolume(handle) && DismountVolume(handle))
            {
                PreventRemovalOfVolume(handle, false);
                result = AutoEjectVolume(handle);
            }
            CloseHandle(handle);
            return result;
        }

        private static bool LockVolume(IntPtr handle)
        {
            uint byteReturned;

            for (int i = 0; i < 10; i++)
            {
                if (DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero))
                {
                    return true;
                }
                Thread.Sleep(500);
            }
            return false;
        }

        private static bool PreventRemovalOfVolume(IntPtr handle, bool prevent)
        {
            byte[] buf = new byte[1];
            uint retVal;

            buf[0] = (prevent) ? (byte)1 : (byte)0;
            return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, IntPtr.Zero, 0, out retVal, IntPtr.Zero);
        }

        private static bool DismountVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }

        private static bool AutoEjectVolume(IntPtr handle)
        {
            uint byteReturned;
            return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
        }

        private static bool CloseVolume(IntPtr handle)
        {
            return CloseHandle(handle);
        }
    }
}