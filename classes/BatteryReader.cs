using System;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using HidApiAdapter;
using System.Reflection;

namespace VoidProOverlay
{
    class BatteryReader
    {
        private const int VOID_BATTERY_MICUP = 128;
        static public int VID = 0x1b1c;
        static public int PID = 0x0a14;
        static private byte[] data_req = { 0xC9, 0x64 };
        public Boolean displayMode;
        static public String manuallySelectedDevice = null;

        private int?[] lastValues { get; set; }
        private const int filterLength = 25;

        private System.Windows.Controls.Label mainLabel;
        private Image mainImage;
        private Dispatcher dispatcher;
        private HidDevice device;
        private IntPtr devPtr;

        public BatteryReader()
        {
            PID = int.Parse(AppSettings.Default.PID, System.Globalization.NumberStyles.HexNumber);
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

        public async void scanLoop() {
            while (true) { 
                var buffer = await getBatteryStatusViaHID();
                if (buffer != null)
                {
                    handleReport(buffer);
                }
                else {
                    setLabelContent("device not found");
                }
            }
        } 

        public void setLabelContent(string text)
        {
            this.dispatcher.Invoke(() =>
            {
                mainLabel.Visibility = displayMode ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                mainImage.Visibility = displayMode ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                if (this.displayMode)
                {
                    string txt;
                    try
                    {
                        txt = filterValue(Int16.Parse(text)).ToString() + "%";
                    }
                    catch { txt = text; }
                    mainLabel.Content = txt;
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

                    mainImage.Source = image;
                }
            });
        }

        private HidDevice getHidDevice() {
            var devices = HidDeviceManager.GetManager().SearchDevices(VID, PID);

            if (manuallySelectedDevice == null)
            {
                foreach (var dev in devices)
                {
                    if (dev.Path().Contains("col02"))
                    {
                        return dev;
                    }
                }

                if (devices.Count > 0)
                {
                    return devices.FirstOrDefault();
                }

                return null;
            }
            else {
                foreach (HidDevice dev in devices) {
                    if (dev.Path().Equals(manuallySelectedDevice)) {
                        return dev;
                    }
                }
                return null;
            }
        }

        public Task<byte[]> getBatteryStatusViaHID()
        {
            return Task.Run(() =>
            {
                device = getHidDevice();

                if (device != null)
                {
                    device.Connect();

                    //get handle via reflection, because its a private field (oof)
                    var field = typeof(HidDevice).GetField("m_DevicePtr", BindingFlags.NonPublic | BindingFlags.Instance);
                    devPtr = (IntPtr)field.GetValue(device);

                    byte[] buffer = new byte[5];
                    HidApi.hid_write(devPtr, data_req, Convert.ToUInt32(data_req.Length));
                    HidApi.hid_read_timeout(devPtr, buffer, Convert.ToUInt32(buffer.Length),1000);
                    device.Disconnect();
                    Thread.Sleep(250);
                    return buffer;
                }
                else
                {
                    return null;
                }
            });
        }

        private void handleReport(byte[] data)
        {
            try
            {
                //If Charging
                if (data[4] == 0 || data[4] == 4 || data[4] == 5)
                {
                    setLabelContent("Battery Charging");
                    this.lastValues = new int?[filterLength];
                    return;
                }

                //If MicUp
                if (data[2] > VOID_BATTERY_MICUP)
                {
                    setLabelContent((data[2] - VOID_BATTERY_MICUP).ToString());
                    return;
                }

                setLabelContent(data[2].ToString());
            }
            catch { return; }
        }
    }
}
