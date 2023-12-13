using Microsoft.Maui.Graphics.Platform;
using System.Diagnostics;
using Microsoft.Maui.Handlers;

namespace GifImageLib.Controls;

public partial class GifImage : ContentView
{
    int rawImageHeight = 0;
    int rawImageWidth = 0;
    double scalex = 1;
    double scaley = 1;
    string? htmlTemplate = null;
#if !WINDOWS
	bool useWebView = true;
#else
    bool useWebView = false;
#endif
    MemoryStream? imageStream;

    public static readonly BindableProperty AssetProperty = BindableProperty.Create("Asset", typeof(string), typeof(GifImage), "",
        propertyChanged: AssetChanged);

    private static readonly BindableProperty AspectProperty = BindableProperty.Create(nameof(IImageElement.Aspect), typeof(Aspect), typeof(GifImage), Aspect.AspectFit, propertyChanged: OnAspectChanged);

    private static async void OnAspectChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var img = bindable as GifImage;
        if (img != null && img.useWebView)
        {
            await img.OnRerenderAsync();
        }
    }

    public GifImage()
    {
        InitializeComponent();
        this.ControlTemplate = Resources[useWebView ? "WebViewTemplate" : "NativeImageTemplate"] as ControlTemplate;
        this.Unloaded += GifImage_Unloaded;
        this.SizeChanged += GifImage_SizeChanged;
#if IOS || MACCATALYST
        this.Loaded += GifImage_Loaded;
#endif
    }

    private async void GifImage_SizeChanged(object? sender, EventArgs e)
    {
        if (rawImageWidth == 0) return;
        if (WidthRequest != -1)
        {
            scalex = WidthRequest / rawImageWidth;
            if (HeightRequest != -1)
            {
                scaley = HeightRequest / rawImageHeight;
            }
            else
            {
                scaley = scalex;
            }
        }
        else if (HeightRequest != -1)
        {
            scalex = scaley = HeightRequest / rawImageHeight;
        }
        else
        {
            scalex = scaley = 1;
        }
        await OnRerenderAsync();
    }

#if IOS || MACCATALYST
    private void GifImage_Loaded(object? sender, EventArgs e)
    {
        var webView = this.GetTemplateChild("webView") as WebView;
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
#endif

    private void GifImage_Unloaded(object? sender, EventArgs e)
    {
        if (imageStream != null) { imageStream.Dispose(); }
        imageStream = null;
    }

    public string Asset
    {
        get
        {
            return (string)GetValue(AssetProperty);
        }
        set
        {
            SetValue(AssetProperty, value);
        }
    }

    public Aspect Aspect
    {
        get
        {
            return (Aspect)GetValue(AspectProperty);
        }
        set
        {
            SetValue(AspectProperty, value);
        }
    }

    private static async void AssetChanged(BindableObject bindable, object oldValue, object newValue)
    {
        try
        {
            var img = bindable as GifImage;
            if (img != null)
            {
                await img.OnRerenderAsync(true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            throw;
        }
    }

    private async Task OnRerenderAsync(bool sourceChanged = false)
    {
        string fileName = Asset;
        Microsoft.Maui.Graphics.IImage src;

        if (useWebView)
        {
            if (fileName != null && (rawImageHeight == 0 || sourceChanged))
            {
                // Let's load the image and get its width and height, so we know how to size the
                // WebView.
                using Stream inputStream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
                src = PlatformImage.FromStream(inputStream);
                rawImageHeight = (int)src.Height;
                rawImageWidth = (int)src.Width;
            }
            double offsetx = 0;
            double offsety = 0;
            var v = this.GetTemplateChild("webView") as WebView;
            if (v == null) return;
            double minScale = Math.Min(scalex, scaley);
            double maxScale = Math.Max(scalex, scaley);
            double pixelWidth = rawImageWidth;
            double pixelHeight = rawImageHeight;
            await ExtractHtmlDocument();

            var aspect = Aspect;
            switch (aspect)
            {
                case Aspect.AspectFit:
                    pixelWidth = pixelWidth * minScale;
                    pixelHeight = pixelHeight * minScale;
                    offsetx = (this.Width - pixelWidth) / 2;
                    offsety = (this.Height - pixelHeight) / 2;

                    break;
                case Aspect.AspectFill:
                    pixelWidth = pixelWidth * maxScale;
                    pixelHeight = pixelHeight * maxScale;
                    if (scalex > scaley)
                    {
                        offsety = (v.Height - pixelHeight) / 2;
                    }
                    else
                    {
                        offsetx = (v.Width - pixelWidth) / 2;
                    }
                    break;
                case Aspect.Fill:
                    pixelWidth = pixelWidth * scalex;
                    pixelHeight = pixelHeight * scaley;
                    break;
                case Aspect.Center:
                    offsetx = (v.Width - pixelWidth) / 2;
                    offsety = (v.Height - pixelHeight) / 2;
                    break;
            }
            // this is a hack, I couldn't get the scrollbars to go away on the WebView on Android, so
            // in the XAML I set the WebView margins to -5 and then we can add the offsets back here.
            offsetx += 5;
            offsety += 5;
#if IOS
            // I don't know why, but to get the right results on iOS I need to multiply by 4
            offsetx *= 4;
            offsety *= 4;
            pixelWidth = pixelWidth * 4;
            pixelHeight = pixelHeight * 4;
#endif
            string html = htmlTemplate!;
            html = html.Replace("{fileName}", fileName);
            html = html.Replace("{width}", (pixelWidth).ToString());
            html = html.Replace("{height}", (pixelHeight).ToString());
            html = html.Replace("{offsetx}", (offsetx).ToString());
            html = html.Replace("{offsety}", (offsety).ToString());

            v.Source = new HtmlWebViewSource()
            {
                Html = html
            };
        }
        else
        {
            var img = this.GetTemplateChild("image") as Image;
            if (img != null)
            {
                if (fileName != null && sourceChanged)
                {
                    using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
                    // this is a hack, copying into a MemoryStream so we can hold on to it in
                    // this instance. If we were to let the stream dispose when it leaves this
                    // method without copying it, the image doesn't display. I have no idea
                    // how to figure out when the stream is done so it can be disposed, so
                    // we hold on to the stream and dispose it when the control is unloaded.
                    // Definitely open to ideas on how to improve this.
                    imageStream = new MemoryStream();
                    stream.CopyTo(imageStream);
                    imageStream.Seek(0, SeekOrigin.Begin);
                    img.Source = ImageSource.FromStream(() => imageStream);
                }
                else
                {
                    img.Source = null;
                }
            }
        }
    }

    private async Task ExtractHtmlDocument()
    {
        using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync("gifTemplate.html");
        using StreamReader sr = new StreamReader(stream);
        using StringWriter sw = new StringWriter();
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (line == null) break;
            sw.WriteLine(line);
        }
        htmlTemplate = sw.ToString();
    }
}