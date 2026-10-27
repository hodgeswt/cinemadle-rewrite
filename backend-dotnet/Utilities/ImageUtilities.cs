using SkiaSharp;

namespace Cinemadle.Utilities;

public static class ImageUtilities
{
    public static string Blur(byte[] imageBytes, float blurFactor)
    {
        var result = blurFactor > 0
            ? GaussianBlurJpeg(imageBytes, blurFactor)
            : imageBytes;

        return $"data:image/jpeg;base64,{Convert.ToBase64String(result)}";

    }

    public static byte[] GaussianBlurJpeg(byte[] jpg, float sigma = 5f, int quality = 100)
    {
        using var src = SKBitmap.Decode(jpg)
            ?? throw new ArgumentException("Invalid JPEG data.", nameof(jpg));

        using var srcImage = SKImage.FromBitmap(src);

        using var surface = SKSurface.Create(
            new SKImageInfo(src.Width, src.Height, SKColorType.Rgba8888, SKAlphaType.Opaque))
            ?? throw new InvalidOperationException("Could not create SKSurface.");

        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp)
        };

        surface.Canvas.DrawImage(srcImage, 0, 0, new SKSamplingOptions(), paint);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }
}