using SkiaSharp;

namespace Cinemadle.Utilities;

public static class ImageUtilities
{
    public static string Blur(byte[] imageBytes, float blurFactor)
    {
        return $"data:image/png;base64,{Convert.ToBase64String(blurFactor > 0
            ? GaussianBlurPng(imageBytes, blurFactor)
            : imageBytes)}";
    }

    public static byte[] GaussianBlurPng(byte[] png, float sigma = 5f)
    {
        using var src = SKBitmap.Decode(png)
            ?? throw new ArgumentException("Invalid PNG data.", nameof(png));

        using var srcImage = SKImage.FromBitmap(src);

        using var surface = SKSurface.Create(
            new SKImageInfo(src.Width, src.Height, SKColorType.Rgba8888, SKAlphaType.Premul))
            ?? throw new InvalidOperationException("Could not create SKSurface.");

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp)
        };

        canvas.DrawImage(srcImage, 0, 0, new SKSamplingOptions(), paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}