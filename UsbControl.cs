using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ZScannerRecovery
{
    static class UsbControl
    {

        /*        
            public const int DbtDevicearrival = 0x8000; // system detected a new device        
            public const int DbtDeviceremovecomplete = 0x8004; // device is gone      
            public const int WmDevicechange = 0x0219; // device change event      
            private const int DbtDevtypDeviceinterface = 5;
            private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
            private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
            private static IntPtr notificationHandle;


            /// <summary>
            /// Registers a window to receive notifications when USB devices are plugged or unplugged.
            /// </summary>
            /// <param name="windowHandle">Handle to the window receiving notifications.</param>
            public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
            {
                DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface
                {
                    DeviceType = DbtDevtypDeviceinterface,
                    Reserved = 0,
                    ClassGuid = GuidDevinterfaceUSBDevice,
                };

                dbi.Size = (uint)Marshal.SizeOf(dbi);
                IntPtr buffer = Marshal.AllocHGlobal((int)dbi.Size);
                Marshal.StructureToPtr(dbi, buffer, true);

                notificationHandle = RegisterDeviceNotification(windowHandle, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);

                Marshal.FreeHGlobal(buffer);
            }
            
            /// <summary>
            /// Unregisters the window for USB device notifications
            /// </summary>
            public static void UnregisterUsbDeviceNotification()
            {
                UnregisterDeviceNotification(notificationHandle);
            }

            public static bool IsMatchedDevice(DevBroadcastDeviceinterface dvi, short vid, short pid)
            {
                String sDeviceID;

                sDeviceID = "VID_" + vid.ToString("X4") + "&" + "PID_" + pid.ToString("X4");
                if (dvi.Name.ToUpper().Contains(sDeviceID))
                    return true;

                return false;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct DevBroadcastDeviceinterface
            {
                public uint Size;
                public uint DeviceType;
                public uint Reserved;
                public Guid ClassGuid;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
                public string Name;
            }


            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

            [DllImport("user32.dll")]
            private static extern bool UnregisterDeviceNotification(IntPtr handle);
        */

        public const uint error_success = 0x00;
        public const uint error_not_found = 0x0e;
        public const uint error_timeout = 0x10;

        [DllImport("UsbControl.dll")]
        public static extern uint ResetUsbDevice(ushort vid, ushort pid, uint nInterval);

        [DllImport("UsbControl.dll")]
        public static extern uint ResetUsbPort(ushort nPortNumber, uint nInterval);

        [DllImport("UsbControl.dll")]
        public static extern uint FindUsbDevice(ushort vid, ushort pid);
    }

}
