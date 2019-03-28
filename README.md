# SystemMediaTransportControl for Winamp
This plugin integrates Winamp with [ SystemMediaTransportControls](https://docs.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols) of Windows 10.
# Requirements for compiling
* Requires Windows 10 SDK (10.0.17763.0 used)
* .NET Framework v4.5
* [Winamp SDK](http://forums.winamp.com/showthread.php?t=252090)
* Visual Studio 2017 (15.9.9 used)
# Usage
* Put `gen_smtc.dll` in the `Winamp/plugins` folder
* Put `SystemMediaTransportControl.dll` and  in the `Winamp/` root directory
* Use keyboard/mouse to play/pause/etc to show the overlay
# What is SystemMediaTransportControls?

> The system transport controls enable music application developers to hook into a system-wide transport control. The system transport control allows a user to control a music application that is in the background as well as get and set the current information on which track is playing. [(link)](https://docs.microsoft.com/en-us/uwp/api/windows.media.systemmediatransportcontrols#remarks)

