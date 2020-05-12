using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace voidProApp
{
    class BatteryReader
    {
        private const int VOID_BATTERY_MICUP = 128;
        private const int VID = 0x1b1c;
        private const int PID = 0x0a14;
        static private byte[] data_req = { 0xC9, 0x64 };
        static private string imagePath = System.IO.Directory.GetCurrentDirectory() + "\\images\\";
        public Boolean displayMode = true;

        private int?[] lastValues { get; set; }
        private const int filterLength = 25;

        private Label mainLabel;
        private Image mainImage;
        private Dispatcher dispatcher;

        static private HidReport rep = new HidReport(2, new HidDeviceData(data_req, HidDeviceData.ReadStatus.Success));
        private HidDevice device;

        public BatteryReader(MainWindow ctx) {
            this.mainLabel = ctx.mainLabel;
            this.mainImage = ctx.mainImage;
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
            dispatcher.Invoke(() =>
            {
                mainLabel.Visibility = displayMode ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                mainImage.Visibility = displayMode ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            });

            if (this.displayMode)
            {
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
            else {
                Uri imageSrc = null;
                int value = Int16.Parse(text);
                if (value < 5)
                {
                    imageSrc = new Uri(imagePath + "empty.png");
                }
                else if (value > 5 && value < 15) {
                    imageSrc = new Uri(imagePath + "low.png");
                }
                else if (value > 15 && value < 50)
                {
                    imageSrc = new Uri(imagePath + "middle-50.png");
                }
                else if (value > 50 && value < 75)
                {
                    imageSrc = new Uri(imagePath + "middle-75.png");
                }
                else if (value > 75)
                {
                    imageSrc = new Uri(imagePath + "full.png");
                }

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = imageSrc;
                image.EndInit();
                image.Freeze();
                
                dispatcher.Invoke(() =>
                {
                    mainImage.Source = image;
                });
            }           
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
                    this.lastValues = new int?[filterLength];
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
                Thread.Sleep(250);
                device.ReadReport(handleReport);
            }
        }  
    }
}
