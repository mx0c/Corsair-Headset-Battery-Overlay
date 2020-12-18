using HidLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using log4net;

namespace VoidProOverlay
{
    class BatteryReader
    {
        private const int VOID_BATTERY_MICUP = 128;
        private const int VID = 0x1b1c;
        private const int PID = 0x0a14;
        static private byte[] data_req = { 0xC9, 0x64 };
        public Boolean displayMode;
        public Boolean shutdown = false;

        private int?[] lastValues { get; set; }
        private const int filterLength = 25;

        private Label mainLabel;
        private Image mainImage;
        private Dispatcher dispatcher;

        static private HidReport rep = new HidReport(2, new HidDeviceData(data_req, HidDeviceData.ReadStatus.Success));
        private HidDevice device;

        public BatteryReader()
        {
            this.mainLabel = MainWindow.label;
            this.mainImage = MainWindow.image;
            this.dispatcher = App.Current.Dispatcher;
            this.lastValues = new int?[filterLength];
            this.displayMode = AppSettings.Default.DisplayMode;           
        }

        private int filterValue(int value)
        {
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
            return sum / (i + 1);
        }

        public void setLabelContent(string text)
        {    
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
            else
            {
                Uri imageSrc = null;
                int value = 0;
                try
                {
                    value = Int16.Parse(text);
                    if (value < 5)
                    {
                        imageSrc = new Uri("pack://application:,,,/images/empty.png");
                    }
                    else if (value > 5 && value < 15)
                    {
                        imageSrc = new Uri("pack://application:,,,/images/low.png");
                    }
                    else if (value > 15 && value < 50)
                    {
                        imageSrc = new Uri("pack://application:,,,/images/middle-50.png");
                    }
                    else if (value > 50 && value < 75)
                    {
                        imageSrc = new Uri("pack://application:,,,/images/middle-75.png");
                    }
                    else if (value > 75)
                    {
                        imageSrc = new Uri("pack://application:,,,/images/full.png");
                    }
                }
                catch
                {
                    imageSrc = new Uri("pack://application:,,,/images/charging.png");
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

        private HidDevice getHidDevice() {
            var devs = new List<HidDevice>(HidDevices.Enumerate(VID, PID));
            if (devs.Count == 1) {
                return devs[0];
            }

            foreach (var dev in devs)
            {
                if (dev.DevicePath.Contains("col02"))
                {
                    return dev;
                }
            }

            if (devs.Count > 0) {
                return devs[0];
            }

            return null;
        }

        public void getBatteryStatusViaHID()
        {
            device = getHidDevice();
            if (device != null)
            {
                device.OpenDevice();
                device.WriteReport(rep);
                HidDeviceData data = device.Read();
                handleReport(data);
            }
            else
            {
                setLabelContent("couldn't find device");
                getBatteryStatusViaHID();
            }
        }

        private void handleReport(HidDeviceData data)
        {
            try
            {
                //If MicUp
                if (data.Data[2] > VOID_BATTERY_MICUP)
                {
                    setLabelContent((data.Data[2] - VOID_BATTERY_MICUP).ToString());
                    return;
                }

                //If Charging
                if (data.Data[4] == 0 || data.Data[4] == 4 || data.Data[4] == 5)
                {
                    setLabelContent("Battery Charging");
                    this.lastValues = new int?[filterLength];
                    return;
                }

                setLabelContent(data.Data[2].ToString());
                return;
            }
            catch { return; }
            finally
            {
                if (!shutdown) { 
                    device.WriteReport(rep);
                    Thread.Sleep(250);
                    HidDeviceData d = device.Read();
                    handleReport(d);
                }
            }
        }
    }
}
