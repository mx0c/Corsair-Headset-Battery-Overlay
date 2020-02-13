using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using Microsoft.Win32;

namespace voidProApp
{
    public partial class MainWindow : Window
    {
        private Boolean visible;
        private Boolean resizable { get; set; }
        public Timer clock { get; set; }
        private KeyboardHook exitKeyHook { get; set; }
        private KeyboardHook displayKeyHook { get; set; }
        private BatteryReader batteryReader { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            RegisterInStartup(true);

            clock = new Timer();
            this.clock.Elapsed += new ElapsedEventHandler(onTimerEvent);
            this.clock.Interval = 2000;
            this.clock.Enabled = true;

            this.batteryReader = new BatteryReader();

            displayKeyHook = new KeyboardHook(this, VirtualKeyCodes.B, ModifierKeyCodes.Control, 0);
            displayKeyHook.Triggered += displayHotkeyEvent;

            exitKeyHook = new KeyboardHook(this, VirtualKeyCodes.X, ModifierKeyCodes.Alt, 1);
            exitKeyHook.Triggered += exitHotkeyEvent;
        }

        public void onTimerEvent(object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("updated");
            batteryReader.getBatteryStatusViaHID();
            this.Dispatcher.Invoke(()=> {
                mainLabel.Content = batteryReader.currentBatteryStatus;
            });         
        }

        private void exitHotkeyEvent() {
            System.Windows.Application.Current.Shutdown();
        }

        private void displayHotkeyEvent() {
            this.visible = !visible;
            if (this.visible) {
                this.mainLabel.Content = batteryReader.currentBatteryStatus;
            }
            VoidProBatteryOverlay.Visibility = this.visible ? Visibility.Visible : Visibility.Hidden; 
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.visible = true;
            this.resizable = false;
            this.mainLabel.Content = batteryReader.currentBatteryStatus;
        }

        private void MainLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void MainLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.resizable = !this.resizable;
            if (this.resizable)
            {
                VoidProBatteryOverlay.ResizeMode = ResizeMode.CanResizeWithGrip;
                VoidProBatteryOverlay.BorderBrush = Brushes.White;
                VoidProBatteryOverlay.BorderThickness = new Thickness(2);
            }
            else {
                VoidProBatteryOverlay.ResizeMode = ResizeMode.NoResize;
                VoidProBatteryOverlay.BorderThickness = new Thickness(0);
            }
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("VoidProBatteryOverlay", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                registryKey.DeleteValue("VoidProBatteryOverlay");
            }
        }
    }
}
