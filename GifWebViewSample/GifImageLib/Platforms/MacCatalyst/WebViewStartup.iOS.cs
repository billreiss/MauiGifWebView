using Microsoft.Maui.Handlers;

namespace GifImageLib.Platforms
{
	internal static partial class WebViewStartup
	{
		// On iOS/Mac, we need to set the WKWebView Opaque property to false in order to support
		// transparent GIFs
		public static void Initialize(WebView webView)
		{
			if (webView != null)
			{
				var handler = webView.Handler as IWebViewHandler;
				if (handler != null)
				{
					var wkWeb = handler.PlatformView as WebKit.WKWebView;
					wkWeb.Opaque = false;
				}
			}
		}
	}
}
