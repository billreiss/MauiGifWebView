# MauiGifWebView
NOTE: The nightly builds have a fix for GIF on iOS and Android. I fully expect it to be in the next service release, which would make this repo obsolete.

Workaround for GIF not animating issue in MAUI on iOS and Android. Use the GifImage control as a mostly drop in replacement for the Image control for displaying GIFs. On Windows the GifImage control uses the native Image control.

You will need to set the gif image file to build action MauiAsset instead of MauiImage for the GifImage control to find it.

I used the property name "Asset" instead of "Source" because I wanted to make it clear that this isn't an ImageSource, it's just a path to the MauiAsset. 
