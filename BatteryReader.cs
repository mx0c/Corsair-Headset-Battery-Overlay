using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace voidProApp
{
    class BatteryReader
    {
        private const int VOID_BATTERY_MICUP = 128;
        private const int VID = 0x1b1c;
        private const int PID = 0x0a14;
        static private byte[] data_req = { 0xC9, 0x64 };

        private int?[] lastValues { get; set; }
        private const int filterLength = 25;

        private Label mainLabel;
        private Dispatcher dispatcher;

        static private HidReport rep = new HidReport(2, new HidDeviceData(data_req, HidDeviceData.ReadStatus.Success));
        private HidDevice device;

        public BatteryReader(MainWindow ctx) {
            this.mainLabel = ctx.mainLabel;
            this.dispatcher = ctx.Dispatcher;
            this.lastValues = new int?[filterLength];
        }

        private int filterValue(int value) {
            int sum = 0, i;

            if (!lastValues.Contains(null))
            {
                //shift array left
                var newArray = new int?[filterLength];
                Array.Copy(lastValues, 1, newArray, 0, lastValues.Length - 1);
                lastValues = newArray;
            }

            for (i = 0; i < lastValues.Length; i++)
            {
                if (lastValues[i] == null)
                {
                    lastValues[i] = value;
                    sum += value;
                    break;
                }
                else
                {
                    sum += (int)lastValues[i];
                }
            }
            return sum / (i+1);
        }

        public void setLabelContent(string text) {
            string txt;
            try
            {
                txt = filterValue(Int16.Parse(text)).ToString() + "%";
            }
            catch { txt = text; }

            dispatcher.Invoke(() =>
            {
                mainLabel.Content = txt;
            });
        }

        public void getBatteryStatusViaHID()
        {
            var devs = new List<HidDevice>(HidDevices.Enumerate(VID, PID));

            foreach (var dev in devs)
            {
                if (dev.DevicePath.Contains("col02")) {
                    device = dev;
                    break;
                }
            }

            if (device != null)
            {               
                device.OpenDevice();

                device.WriteReport(rep);
                device.ReadReport(handleReport);    
            }
            else
            {
                setLabelContent("couldn't find device");
                getBatteryStatusViaHID();
            }
        }

        private void handleReport(HidReport report)
        {
            try
            {
                //If Charging
                if (report.Data[3] == 0 || report.Data[3] == 4 || report.Data[3] == 5)
                {
                    setLabelContent("Battery Charging");
                    return;
                }

                //If MicUp
                if (report.Data[1] > VOID_BATTERY_MICUP)
                {
                    setLabelContent((report.Data[1] - VOID_BATTERY_MICUP).ToString());
                    return;
                }

                setLabelContent(report.Data[1].ToString());
                return;
            }
            catch { return; }
            finally
            {
                device.WriteReport(rep);
                device.ReadReport(handleReport);
            }
        }  
    }
}
