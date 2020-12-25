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

        private Thread readThread;
        private NotifyIcon ni;
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
        }

        private void exitHotkeyEvent()
        {
            batteryReader.shutdown = true;
            System.Windows.Application.Current.Shutdown();
        }

        private void displayHotkeyEvent()
        {
            this.visible = !visible;
            VoidProBatteryOverlay.Visibility = this.visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void switchModeKeyEvent()
        {
            this.ni.ContextMenuStrip.Items[3].Text = this.batteryReader.displayMode ? "Activate Textmode" : "Activate Imagemode";
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
            readThread = new Thread(batteryReader.getBatteryStatusViaHID);
            readThread.Start();          
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
