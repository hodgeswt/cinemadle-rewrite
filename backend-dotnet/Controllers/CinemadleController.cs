using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Controllers;

[Route("api/cinemadle")]
[ApiController]
public class CinemadleController : ControllerBase
{
    IConfigRepository _configRepo;
    ITmdbRepository _tmdbRepo;

    public CinemadleController(IConfigRepository config, ITmdbRepository tmdbRepository)
    {
        _configRepo = config;
        _tmdbRepo = tmdbRepository;
    }

    [HttpGet("heartbeat")]
    public ActionResult<string> Heartbeat()
    {
        return _configRepo.IsLoaded().ToString();
    }

    [HttpGet("movie/{movieName}")]
    public async Task<ActionResult> GetMovie(string movieName)
    {
        MovieDto? movie = await _tmdbRepo.GetMovie(movieName);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(movie!);
    }
}
