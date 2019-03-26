////////////////////////////////////////////////////////////////////////////////
/// Wrapper for HelloWorldGUI
////////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "gen_smtc.h"
#include "PluginWrapper.h"
#include <windows.h>
#include <shlwapi.h>
#include "../winamp/wa_ipc.h"


static api_service* WASABI_API_SVC;
static api_albumart* AGAVE_API_ALBUMART;

static BITMAPINFO bmi;
static HBITMAP srcBMP;
static ARGB32 * cur_image;

using namespace System;

// this structure contains plugin information, version, name...
winampGeneralPurposePlugin plugin =
{
	GPPHDR_VER,  // version of the plugin, defined in "gen_HelloWorldPlugin.h"
	PluginWrapper::Name(),
	init,        // function name which will be executed on init event
	config,      // function name which will be executed on config event
	quit,        // function name which will be executed on quit event
	0,           // handle to Winamp main window, loaded by winamp when this dll is loaded
	0            // hinstance to this dll, loaded by winamp when this dll is loaded
};

// Called when Winamp initializes the plugin
int init()
{
	// Load AlbumArt service
	WASABI_API_SVC = (api_service*)SendMessage(plugin.hwndParent, WM_WA_IPC, 0, IPC_GET_API_SERVICE);
	if (WASABI_API_SVC == (api_service*)1 || WASABI_API_SVC == NULL)
		return 1; // No api service?

	waServiceFactory *sf = WASABI_API_SVC->service_getServiceByGuid(memMgrApiServiceGuid);
	WASABI_API_MEMMGR = reinterpret_cast<api_memmgr *>(sf->getInterface());

	// Album service
	sf = WASABI_API_SVC->service_getServiceByGuid(albumArtGUID);
	AGAVE_API_ALBUMART = reinterpret_cast<api_albumart *>(sf->getInterface());

	try
	{
		PluginWrapper::plugin->SetAlbumArtFunc(IntPtr(GetAlbumArt)); // Send GetAlbumArt function pointer to C#
		PluginWrapper::plugin->Init(safe_cast<IntPtr>(plugin.hwndParent));
	}
	catch (Exception^ ex)
	{
		System::Windows::Forms::MessageBox::Show(L"An error occured while initialising! \r\n" + ex->Message);
		return 1;
	}

	return 0;
}
 
// Called when Configure button in Winamp is clicked
void config()
{
	PluginWrapper::plugin->Config();
}
 
// Called when Winamp is quitting
void quit()
{
	PluginWrapper::plugin->Quit();
	delete PluginWrapper::plugin;
}

static void* GetAlbumArt(const wchar_t *filename, const wchar_t *type)
{
	static int cur_w, cur_h;
	if (cur_image) WASABI_API_MEMMGR->sysFree(cur_image); cur_image = 0;

	if (AGAVE_API_ALBUMART->GetAlbumArt(filename, type, &cur_w, &cur_h, &cur_image) == ALBUMART_SUCCESS)
	{
		if(srcBMP) DeleteObject(srcBMP);

		ZeroMemory(&bmi, sizeof bmi);
		bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		bmi.bmiHeader.biWidth = cur_w;
		bmi.bmiHeader.biHeight = -cur_h;
		bmi.bmiHeader.biPlanes = 1;
		bmi.bmiHeader.biBitCount = 32;
		bmi.bmiHeader.biCompression = BI_RGB;
		bmi.bmiHeader.biSizeImage = 0;
		bmi.bmiHeader.biXPelsPerMeter = 0;
		bmi.bmiHeader.biYPelsPerMeter = 0;
		bmi.bmiHeader.biClrUsed = 0;
		bmi.bmiHeader.biClrImportant = 0;
		void *bits = 0;

		srcBMP = CreateDIBSection(NULL, &bmi, DIB_RGB_COLORS, &bits, NULL, 0);
		memcpy(bits, cur_image, cur_w * cur_h * 4);

		return srcBMP;
	}

	return 0;
}

// Return data about the plugin to Winamp
extern "C" __declspec(dllexport) winampGeneralPurposePlugin * winampGetGeneralPurposePlugin()
{
	return &plugin;
}