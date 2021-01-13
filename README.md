# Corsair-Void-Pro-Battery-Overlay
A transparent WPF overlay that displays the battery charge level of the Corsair Void Pro headset. Once started it starts automatically on every system startup. The overlay is always on top of all applications so you dont have to f.e. tab out of a game to check your battery status.

## Usage
Either use The Tray Icon to configure the application or the following Hotkeys:
* `STRG + B` to toggle visibility of the overlay
* `ALT + X` to Exit application
* `ALT + Q` to switch between text- and imagemode

Also:
* Doubleclick overlay to resize
* Click and move overlay to reposition the overlay

## Troubleshooting
If your device is not found you probably need to configure the PID of your Device. To do so you need to first look up this ID. You can find it under Control Panel ->Device Manager then on the tab Audio, Video and Gamecontroller -> Corsaird Void Pro -> Right click -> properties -> details tab -> Hardware-Id's. You'll probably find multiple strings looking like this: `USB\VID_1B1C&PID_0A14&MI_00`. The PID part of the string needs to be inserted in the change PID option which appears when right clicking the trayicon with a Headset icon.
