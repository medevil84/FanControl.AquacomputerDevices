# FanControl.AquacomputerDevices
[FanControl](https://github.com/Rem0o/FanControl.Releases) Plugin for Aquacomputer devices (HighFlowNext, Quadro, D5 Next, and *Highly Experimental* Octo / Farbwerk)

This plugin will interface directly with the usb devices, so *aquasuite is not needed*.

> This code has been tested with firmware version >= 1012 of the **HighFlowNext** device.

> This code has been tested with firmware version == 1032 of the **Quadro** Device.

> This code has been tested with firmware version == 1025 of the **Farbwerk** Device (thanks to [@thomasfifer](https://github.com/thomasfifer))

> **D5 Next** support: status sensors (coolant temp, pump/fan RPM, power, voltage) and fixed-percent pump/fan duty control. Protocol offsets match [liquidctl](https://github.com/liquidctl/liquidctl) and the Linux `aquacomputer_d5next` driver. Close AquaSuite while using control from FanControl.

## USE THIS SOFTWARE AT YOUR OWN RISK!
The _FanControl.AquacomputerDevices_ main developer is running this software, without any negative side effects. However, it is a **highly experimental software**, and it is based on reverse engineering only.

* This software might damage your Aquacomputer devices in a way that it must be repaired or replaced.
* The use of this software could lead to failure of cooling fans or aqua pumps, which might cause permanent damage to heat sensitive components of your system.
* Using this software might void your warranty.

Neither the _FanControl.AquacomputerDevices_ developers nor the manufacturer can be held liable for any damage caused by this software.

It is strongly recommended to use only the official software!


## Installation
Copy the dll files to the \Plugins folder. Make sure that Windows did not automatically block the files (uncheck the "Unblock" property).
### _*** If you are using other FanControl plugins for quadro or highflownext, please uninstall them ***_

## Contribute

* Fork the [Source code at GitHub](https://github.com/medevil84/FanControl.AquacomputerDevices). Feel free to send pull requests.
* Found a bug? [File a bug report!](https://github.com/medevil84/FanControl.AquacomputerDevices/issues)

## License

_FanControl.AquacomputerDevices_ is open source software. The source code is distributed under the terms of [GNU Lesser General Public License (LGPLv3)](https://www.gnu.org/licenses/gpl-3.0.en.html#content).

## Acknowledgements

 - [FanControl](https://github.com/Rem0o/FanControl.Releases) for providing a great software ( _would be greater if it was open sourced too :P_ )
 - [HidLibrary](https://github.com/mikeobrien/HidLibrary) / [HidSharp](https://www.zer7.com/software/hidsharp) for USB HID access
 - [aquacomputer-quadro-control](https://github.com/leoratte/aquacomputer-quadro-control) was used to understand how to apply settings to the device
 - [liquidctl](https://github.com/liquidctl/liquidctl) and [aquacomputer_d5next](https://github.com/aleksamagicka/aquacomputer_d5next-hwmon) for D5 Next HID report offsets and fixed-duty control layout
 - [dnSpy](https://github.com/dnSpy/dnSpy) helped a lot during the reverse engineering
 - [WireShark](https://www.wireshark.org) for reverse engineering. Without this tool, no "control" part of the quadro would be available.
