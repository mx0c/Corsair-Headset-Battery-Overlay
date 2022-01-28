using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;

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
        private PredefinedConfigs preConfigs { get; set; }

        private NotifyIcon ni = new NotifyIcon();
        public static System.Windows.Controls.Label label;
        public static Image image;
        private List<BatteryIcon> _icons;

        public MainWindow()
        {
            InitializeComponent();

            label = mainLabel;
            image = mainImage;

            batteryReader = new BatteryReader();
            preConfigs = new PredefinedConfigs();

            displayKeyHook = new KeyboardHook(this, VirtualKeyCodes.B, ModifierKeyCodes.Control, 0);
            displayKeyHook.Triggered += ToggleVisibility;

            exitKeyHook = new KeyboardHook(this, VirtualKeyCodes.X, ModifierKeyCodes.Alt, 1);
            exitKeyHook.Triggered += ExitRequested;

            switchModeKeyHook = new KeyboardHook(this, VirtualKeyCodes.Q, ModifierKeyCodes.Alt, 2);
            switchModeKeyHook.Triggered += ModeSwitchRequested;

            SetupTrayIcon();
            LoadBatteryIcons();
            batteryReader.OnBatteryPercentUpdated += (o, e) =>
            {
                ni.Text = $"{e.BatteryPercent}%";
                ni.Icon = FindIconFor(e.BatteryPercent);
            };
        }

        private Icon FindIconFor(int eBatteryPercent)
        {
            return _icons
                .FirstOrDefault(o => o.MinPercent <= eBatteryPercent && o.MaxPercent > eBatteryPercent)
                ?.Icon ?? ni.Icon;
        }

        private void LoadBatteryIcons()
        {
            _icons = new List<BatteryIcon>();
            var prefix = "battery-white";
            _icons.Add(Load($"{prefix}-100", 95, int.MaxValue));
            _icons.Add(Load($"{prefix}-90", 75, 95));
            _icons.Add(Load($"{prefix}-75", 60, 75));
            _icons.Add(Load($"{prefix}-50", 35, 60));
            _icons.Add(Load($"{prefix}-25", 10, 35));
            _icons.Add(Load($"{prefix}-10", 5, 10));
            _icons.Add(Load($"{prefix}-00", int.MinValue, 5));

            BatteryIcon Load(string name, int min, int max)
            {
                return new BatteryIcon(
                    LoadIcon(name),
                    min,
                    max
                );
            }
        }

        private class BatteryIcon
        {
            public Icon Icon { get; }
            public int MinPercent { get; }
            public int MaxPercent { get; }

            public BatteryIcon(
                Icon icon,
                int minPercent,
                int maxPercent
            )
            {
                Icon = icon;
                MinPercent = minPercent;
                MaxPercent = maxPercent;
            }
        }

        private Icon LoadIcon(string name)
        {
            using var stream = Application.GetResourceStream(
                new Uri($"pack://application:,,,/icon/{name}.ico")
            ).Stream;
            return new Icon(
                stream
            );
        }

        private void SetupTrayIcon()
        {
            ni.Icon = LoadIcon("headset");
            ni.Visible = true;
            ni.ContextMenuStrip = new ContextMenuStrip();

            AddVisibilityToggler();
            AddModeToggle();
            AddManualPIDChanger();
            AddKnownDevices();
            AddMenuDivider();
            AddAutoStartMenuItem();
            AddExitMenuItem();

            batteryReader.scanLoop();
            DisplayRequested();
        }

        private void AddMenuDivider()
        {
            ni.ContextMenuStrip.Items.Add("-");
        }

        private void AddAutoStartMenuItem()
        {
            var autostartMenuItem = ni.ContextMenuStrip.Items.Add("Autostart with Windows", null, (sender, args) =>
            {
                var item = sender as ToolStripMenuItem;
                if (item.Checked)
                {
                    RegisterInStartup();
                }
                else
                {
                    UnregisterFromStartup();
                }
            }) as ToolStripMenuItem;
            autostartMenuItem.CheckOnClick = true;
            autostartMenuItem.Checked = IsRegisteredWithWindowsStartup();
        }

        private void AddExitMenuItem()
        {
            ni.ContextMenuStrip.Items.Add("Exit", null, (sender, args) =>
            {
                ExitRequested();
            });
        }

        private void AddVisibilityToggler()
        {
            visible = true;
            var item = ni.ContextMenuStrip.Items.Add("Visibility", null, (sender, args) =>
            {
                ToggleVisibility();
            });
            ((ToolStripMenuItem)item).CheckOnClick = true;
            ((ToolStripMenuItem)item).Checked = true;
        }

        private void AddModeToggle()
        {
            ni.ContextMenuStrip.Items.Add(batteryReader.displayMode
                ? "Activate Imagemode"
                : "Activate Textmode", null, (sender, args) =>
            {
                ModeSwitchRequested();
            });
        }

        private void AddManualPIDChanger()
        {
            var parentItem = ni.ContextMenuStrip.Items.Add("Manually change PID", null, null);
            var ttb = new ToolStripTextBox();
            ttb.Text = BatteryReader.PID.ToString("X4");
            ttb.TextChanged += (sender, args) =>
            {
                var hexStr = ((ToolStripTextBox)sender).Text;
                BatteryReader.PID = int.Parse(hexStr, System.Globalization.NumberStyles.HexNumber);
            };
            ((ToolStripMenuItem)parentItem).DropDownItems.Add(ttb);
        }

        private void AddKnownDevices()
        {
            var parentItem =
                ni.ContextMenuStrip.Items.Add("Select Predefined Device Config", null, null) as ToolStripMenuItem;
            var configMenuItems = new List<ToolStripMenuItem>();
            foreach (var dev in preConfigs.deviceconfigs)
            {
                var item = parentItem?.DropDownItems.Add(dev.Name, null,
                    (sender, args) =>
                    {
                        var senderItem = sender as ToolStripMenuItem;
                        if (senderItem is null)
                        {
                            return;
                        }

                        foreach (ToolStripMenuItem item in configMenuItems)
                        {
                            if (item is not null)
                            {
                                item.Checked = false;
                            }
                        }

                        senderItem.Checked = true;
                        var hexString = preConfigs.deviceconfigs
                            .Find(x => x.Name.Equals(senderItem.Text))
                            ?.PID;
                        if (hexString is null)
                        {
                            return;
                        }

                        BatteryReader.PID = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
                    }) as ToolStripMenuItem;
                configMenuItems.Add(item);
                if (BatteryReader.PID == int.Parse(dev.PID, System.Globalization.NumberStyles.HexNumber))
                {
                    item.Checked = true;
                }
            }
        }

        private void ExitRequested()
        {
            ni.Visible = false;
            Application.Current.Shutdown();
        }

        private void ToggleVisibility()
        {
            visible = !visible;
            DisplayRequested();
        }

        private void DisplayRequested()
        {
            VoidProBatteryOverlay.Visibility = visible
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private void ModeSwitchRequested()
        {
            ni.ContextMenuStrip.Items[1].Text = batteryReader.displayMode
                ? "Activate Textmode"
                : "Activate Imagemode";
            batteryReader.displayMode = !batteryReader.displayMode;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = AppSettings.Default.Left;
            Top = AppSettings.Default.Top;
            VoidProBatteryOverlay.Width = AppSettings.Default.Width;
            VoidProBatteryOverlay.Height = AppSettings.Default.Height;
            visible = true;
            resizable = false;
        }

        private void MainLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                resize();
            }

            DragMove();
        }

        private void RegisterInStartup()
        {
            using var registryKey = OpenAutoRunKey();
            var processModule = System.Diagnostics.Process.GetCurrentProcess().MainModule;
            if (processModule is null)
            {
                return;
            }

            registryKey.SetValue(AutoRunValueName,
                processModule.FileName
            );
        }

        private void UnregisterFromStartup()
        {
            using var registryKey = OpenAutoRunKey();
            registryKey.DeleteValue(AutoRunValueName);
        }

        private RegistryKey OpenAutoRunKey()
        {
            return Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true
            );
        }

        private const string AutoRunValueName = "VoidProBatteryOverlay";

        private bool IsRegisteredWithWindowsStartup()
        {
            using var regKey = OpenAutoRunKey();
            return regKey.GetValue(AutoRunValueName) is not null;
        }

        private void VoidProBatteryOverlay_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save window location
            AppSettings.Default.Left = Left;
            AppSettings.Default.Top = Top;
            AppSettings.Default.Width = VoidProBatteryOverlay.ActualWidth;
            AppSettings.Default.Height = VoidProBatteryOverlay.ActualHeight;
            AppSettings.Default.DisplayMode = batteryReader.displayMode;
            AppSettings.Default.PID = BatteryReader.PID.ToString("X4");
            AppSettings.Default.Save();
        }

        private void MainImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                resize();
            }

            DragMove();
        }

        private void resize()
        {
            resizable = !resizable;
            if (resizable)
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