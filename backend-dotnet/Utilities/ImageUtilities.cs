using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Cinemadle.Utilities;

/// <summary>
/// Receives an image and returns it with the desired gaussian blur applied
/// </summary>
public static class ImageUtilities
{
    public static string Blur(byte[] imageBytes, float blurFactor)
    {
        using var image = Image.Load(imageBytes);

        if (blurFactor > 0)
        {
            image.Mutate(x => x.GaussianBlur(blurFactor));
        }

        return image.ToBase64String(PngFormat.Instance);
    }
}