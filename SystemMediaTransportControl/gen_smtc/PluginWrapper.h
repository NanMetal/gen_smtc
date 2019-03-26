////////////////////////////////////////////////////////////////////////////////
/// Winamp C# Plugin Wrapper - Part of Sharpamp C# library
////////////////////////////////////////////////////////////////////////////////
#pragma once
using namespace System;

ref class PluginWrapper
{
public:
	// The plugin itself
	static SMTC::SystemMediaTransportControl^ plugin = gcnew SMTC::SystemMediaTransportControl();

	// Name of the plugin
	static char * Name();
};
