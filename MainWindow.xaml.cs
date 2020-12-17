using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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

        public static Label label;
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
        }

        private void exitHotkeyEvent()
        {
            batteryReader.shutdown = true;
            Application.Current.Shutdown();
        }

        private void displayHotkeyEvent()
        {
            this.visible = !visible;
            VoidProBatteryOverlay.Visibility = this.visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void switchModeKeyEvent()
        {
            //log.Info("changed displaymode");
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
                registryKey.SetValue("VoidProBatteryOverlay", System.Reflection.Assembly.GetExecutingAssembly().Location);
                var x = System.Reflection.Assembly.GetExecutingAssembly().Location;
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
