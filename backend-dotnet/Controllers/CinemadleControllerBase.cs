using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Cinemadle.Utilities;

namespace Cinemadle.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class CinemadleControllerBase : ControllerBase
{
    protected string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    protected bool UserHasRole(string claim)
    {
        return User.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == claim);
    }
    
    protected static string MapColorToEmoji(string color)
    {
        return color switch
        {
            "green" => "🟩",
            "yellow" => "🟨",
            _ => "⬛",
        };
    }
    
    protected async Task<ImageDto?> GetBlurredImage(string date, float blurFactor, ITmdbRepository tmdbRepository)
    {
        var targetMovie = await tmdbRepository.GetTargetMovie(date);
        if (targetMovie is null) return null;
        
        return await GetBlurredImageForMovie(targetMovie.Id, blurFactor, tmdbRepository);
    }

    protected async Task<ImageDto?> GetBlurredImageForMovie(int movieId, float blurFactor, ITmdbRepository tmdbRepository)
    {
        var imageBytes = await tmdbRepository.GetMovieImageById(movieId);

        if (imageBytes is null) return null;

        return new ImageDto
        {
            ImageData = ImageUtilities.Blur(imageBytes, blurFactor)
        };
    }
}