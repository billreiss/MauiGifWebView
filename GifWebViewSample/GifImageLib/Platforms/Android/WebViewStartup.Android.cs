using Microsoft.Maui.Handlers;

namespace GifImageLib.Platforms
{
	internal static partial class WebViewStartup
	{
		// On Android, we need to disable the WebKit WebView scrollbars
		public static void Initialize(WebView webView)
		{
			var handler = webView.Handler as IWebViewHandler;
			if (handler != null)
			{
				var wv = handler.PlatformView as Android.Webkit.WebView;
				wv.VerticalScrollBarEnabled = false;
				wv.HorizontalScrollBarEnabled = false;
			}
		}
	}
}