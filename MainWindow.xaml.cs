using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace VoidProOverlay
{
    public partial class MainWindow : Window
    {
        private Boolean visible;
        private Boolean resizable { get; set; }
        private KeyboardHook exitKeyHook { get; set; }
        private KeyboardHook displayKeyHook { get; set; }
        private KeyboardHook switchModeKeyHook { get; set; }
        private BatteryReader batteryReader { get; set; }

        public static NotifyIcon ni;
        public static System.Windows.Controls.Label label;
        public static Image image;

        public MainWindow()
        {
            InitializeComponent();
            RegisterInStartup(true);

            label = mainLabel;
            image = mainImage;

            this.batteryReader = new BatteryReader();

            displayKeyHook = new KeyboardHook(this, VirtualKeyCodes.B, ModifierKeyCodes.Control, 0);
            displayKeyHook.Triggered += displayHotkeyEvent;

            exitKeyHook = new KeyboardHook(this, VirtualKeyCodes.X, ModifierKeyCodes.Alt, 1);
            exitKeyHook.Triggered += exitHotkeyEvent;

            switchModeKeyHook = new KeyboardHook(this, VirtualKeyCodes.Q, ModifierKeyCodes.Alt, 2);
            switchModeKeyHook.Triggered += switchModeKeyEvent;

            setupTrayIcon();
        }

        private void setupTrayIcon() {
            ni = new NotifyIcon();
            Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon/headset.ico")).Stream;
            ni.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();
            ni.Visible = true;
            ni.ContextMenuStrip = new ContextMenuStrip();
            ni.ContextMenuStrip.Items.Add("Exit", null, (sender, args) => { exitHotkeyEvent(); });
            ni.ContextMenuStrip.Items.Add("Remove from Autostart", null, (sender, args) => { RegisterInStartup(false); });
            ni.ContextMenuStrip.Items.Add("Visibility", null, (sender, args) => { displayHotkeyEvent(); });
            ((ToolStripMenuItem)ni.ContextMenuStrip.Items[2]).CheckOnClick = true;
            ((ToolStripMenuItem)ni.ContextMenuStrip.Items[2]).Checked = true;
            ni.ContextMenuStrip.Items.Add(this.batteryReader.displayMode ? "Activate Imagemode" : "Activate Textmode", null, (sender, args) => { switchModeKeyEvent(); });

            ni.ContextMenuStrip.Items.Add("Manually select Device", null, null);
            var devices = HidApiAdapter.HidDeviceManager.GetManager().SearchDevices(BatteryReader.VID, BatteryReader.PID);
            foreach (var dev in devices) {
                ((ToolStripMenuItem)ni.ContextMenuStrip.Items[4]).DropDownItems.Add(dev.Path(), null, (sender, args) =>
                {
                    BatteryReader.manuallySelectedDevice = ((ToolStripMenuItem)sender).Text;
                    foreach (ToolStripMenuItem item in ((ToolStripMenuItem)sender).GetCurrentParent().Items) {
                        item.Checked = false;
                    }
                    ((ToolStripMenuItem)sender).Checked = true;
                });

            }
        }

        static public void selectDevice(string devicePath) {
            foreach (var item in ((ToolStripMenuItem)ni.ContextMenuStrip.Items[4]).DropDownItems)
            {
                if (item.ToString().Equals(devicePath))
                {
                    App.Current.Dispatcher.Invoke(() => { ((ToolStripMenuItem)item).Checked = true; });
                }
            }
        }

        private void exitHotkeyEvent()
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void displayHotkeyEvent()
        {
            this.visible = !visible;
            VoidProBatteryOverlay.Visibility = this.visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void switchModeKeyEvent()
        {
            ni.ContextMenuStrip.Items[3].Text = this.batteryReader.displayMode ? "Activate Textmode" : "Activate Imagemode";
            this.batteryReader.displayMode = !this.batteryReader.displayMode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = AppSettings.Default.Left;
            this.Top = AppSettings.Default.Top;
            VoidProBatteryOverlay.Width = AppSettings.Default.Width;
            VoidProBatteryOverlay.Height = AppSettings.Default.Height;
            this.visible = true;
            this.resizable = false;
            batteryReader.scanLoop();
        }

        private void MainLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                resize();
            }
            this.DragMove();
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("VoidProBatteryOverlay", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                //log.Info("Registered in Startup");
            }
            else
            {
                registryKey.DeleteValue("VoidProBatteryOverlay");
            }
        }

        private void VoidProBatteryOverlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save window location
            AppSettings.Default.Left = this.Left;
            AppSettings.Default.Top = this.Top;
            AppSettings.Default.Width = VoidProBatteryOverlay.ActualWidth;
            AppSettings.Default.Height = VoidProBatteryOverlay.ActualHeight;
            AppSettings.Default.DisplayMode = this.batteryReader.displayMode;
            AppSettings.Default.Save();
        }

        private void MainImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                resize();
            }
            this.DragMove();
        }

        private void resize()
        {
            this.resizable = !this.resizable;
            if (this.resizable)
            {
                VoidProBatteryOverlay.ResizeMode = ResizeMode.CanResizeWithGrip;
                VoidProBatteryOverlay.BorderBrush = Brushes.White;
                VoidProBatteryOverlay.BorderThickness = new Thickness(2);
            }
            else
            {
                VoidProBatteryOverlay.ResizeMode = ResizeMode.NoResize;
                VoidProBatteryOverlay.BorderThickness = new Thickness(0);
            }
        }
    }
}
