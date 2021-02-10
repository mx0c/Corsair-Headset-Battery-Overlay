# Corsair-Headset-Battery-Overlay
A transparent WPF overlay that displays the battery charge level of different Corsair Headsets. Once started it starts automatically on every system startup. The overlay is always on top of all applications so you dont have to f.e. tab out of a game to check your battery status.

## Usage
Either use The Tray Icon to configure the application or the following Hotkeys:
* `STRG + B` to toggle visibility of the overlay
* `ALT + X` to Exit application
* `ALT + Q` to switch between text- and imagemode

Also:
* Doubleclick overlay to resize
* Click and move overlay to reposition the overlay

## Supported Headsets
* CORSAIR Void RGB wireless
* CORSAIR HS70 Wireless Gaming Headset
* Corsair VOID PRO Wireless Gaming Headset

Let me know if you have another Headset that works with this overlay. 

## Troubleshooting
If your device is not found in the predefined configs selection you probably need to configure the PID of your Device manually. To do so you need to first look up this ID. You can find it under Control Panel ->Device Manager then on the tab Audio, Video and Gamecontroller -> Corsaird Void Pro -> Right click -> properties -> details tab -> Hardware-Id's. You'll probably find multiple strings looking like this: `USB\VID_1B1C&PID_0A14&MI_00`. The PID part of the string needs to be inserted in the change PID option which appears when right clicking the trayicon with a Headset icon.
