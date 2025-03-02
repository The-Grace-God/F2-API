using System;
using System.Runtime.InteropServices;
using HidSharp;
using MelonLoader;

namespace F2API
{
    public sealed class F2APILib : MelonMod
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RGB_t
        {
            public byte r, g, b;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FaucetwoLedReport
        {
            public byte report_id;
            public RGB_t topLeftLeft,
                        topLeft,
                        topRight,
                        topRightRight,
                        left,
                        reserved_0,
                        right;
            public byte mode0; 
            public byte mode1; 
            public byte mode2; 
            public byte bta, btb, btc, btd, fxl, fxr, start;
        }

        private HidStream ledDevice;
        private FaucetwoLedReport f2Report;

        public override void OnLateInitializeMelon()
        {
            // Initialize the device
            ledDevice = OpenFaucetTwoLEDs();
            if (ledDevice != null)
            {
                MelonLogger.Msg("FaucetTwo device opened successfully.");
            }
            else
            {
                MelonLogger.Error("Could not find FaucetTwo device.");
            }
        }

        private HidStream OpenFaucetTwoLEDs()
        {
            var devices = DeviceList.Local.GetHidDevices();
            foreach (var device in devices)
            {
                if (device.ProductID == 0x1118 && device.VendorID == 0x0E8F)
                {
                    f2Report.mode0 = 0;
                    f2Report.mode1 = 1;
                    f2Report.mode2 = 2;
                    try
                    {
                        return device.Open();
                    }
                    catch
                    {
                        MelonLogger.Error("Connection failed, retrying...");
                        System.Threading.Thread.Sleep(1000);
                        return device.Open();
                    }
                }
            }
            return null;
        }

        public void SetButtons(byte[] bitfield)
        {
            if (bitfield.Length >= 7)
            {
                f2Report.bta = (byte)(bitfield[0] & 1);
                f2Report.btb = (byte)(bitfield[1] & 1);
                f2Report.btc = (byte)(bitfield[2] & 1);
                f2Report.btd = (byte)(bitfield[3] & 1);
                f2Report.fxl = (byte)(bitfield[4] & 1);
                f2Report.fxr = (byte)(bitfield[5] & 1);
                f2Report.start = (byte)(bitfield[6] & 1);
            }
            else
            {
                MelonLogger.Error("Bitfield array is too short. It needs at least 7 bytes.");
            }
        }

        public void SetLights(byte left, byte pos, byte r, byte g, byte b)
        {
            RGB_t col;
            col.r = r;
            col.g = g;
            col.b = b;

            if (left == 1)
            {
                switch (pos)
                {
                    case 0:
                        f2Report.left = col;
                        break;
                    case 1:
                        f2Report.topLeft = col;
                        break;
                    case 2:
                        f2Report.topLeftLeft = col;
                        break;
                }
            }
            else
            {
                switch (pos)
                {
                    case 0:
                        f2Report.right = col;
                        break;
                    case 1:
                        f2Report.topRight = col;
                        break;
                    case 2:
                        f2Report.topRightRight = col;
                        break;
                }
            }
        }

        public override void OnUpdate()
        {
            if (ledDevice != null)
            {
                byte[] reportBytes = StructToByteArray(f2Report);
                try
                {
                    ledDevice.Write(reportBytes);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Error: " + ex.Message);
                }
            }
        }

        private byte[] StructToByteArray<T>(T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, false);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public override void OnDeinitializeMelon()
        {
            if (ledDevice != null)
            {
                ledDevice.Close();
            }
        }
    }
}
