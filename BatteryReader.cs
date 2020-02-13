using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace voidProApp
{
    class BatteryReader
    {
        private const int VOID_BATTERY_MICUP = 128;
        private const int VID = 0x1b1c;
        private const int PID = 0x0a14;
        public string currentBatteryStatus;

        public void getBatteryStatusViaHID()
        {
            var devices = HidDevices.Enumerate(VID, PID);

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    device.OpenDevice();

                    //_device.Inserted += DeviceAttachedHandler;
                    //_device.Removed += DeviceRemovedHandler;
                    //_device.MonitorDeviceEvents = true;

                    byte[] data_request = { 0xC9, 0x64 };
                    device.Write(data_request);
                    device.ReadReport(OnReport);

                    device.CloseDevice();
                }

            }
            else
            {
                currentBatteryStatus = "couldn't find device";
            }
        }

        private void OnReport(HidReport report)
        {
            try
            {
                //If Charging
                if (report.Data[3] == 0 || report.Data[3] == 4 || report.Data[3] == 5)
                {
                    currentBatteryStatus = "Battery Charging";
                    return;
                }

                //If MicUp
                if (report.Data[2] > VOID_BATTERY_MICUP)
                {
                    currentBatteryStatus = (report.Data[2] - VOID_BATTERY_MICUP).ToString() + "%";
                    return;
                }

                currentBatteryStatus = report.Data[2].ToString() + "%";
            }
            catch { return; }
        }  
    }
}
