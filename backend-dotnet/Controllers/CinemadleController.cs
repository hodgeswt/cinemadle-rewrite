using Cinemadle.Datamodel;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace Cinemadle.Controllers;

[Route("api/cinemadle")]
[ApiController]
public class CinemadleController : ControllerBase
{
    private readonly CinemadleConfig _config;
    private ITmdbRepository _tmdbRepo;
    private IGuessRepository _guessRepo;
    private ILogger<CinemadleController> _logger;

    private bool _isDevelopment;

    public CinemadleController(
            ILogger<CinemadleController> logger,
            IConfigRepository configRepository,
            ITmdbRepository tmdbRepository,
            IWebHostEnvironment env,
            IGuessRepository guessRepository
    )
    {
        _logger = logger;
        string type = this.GetType().AssemblyQualifiedName ?? "CinemadleController";
        _logger.LogDebug("+ctor({type})", type);

        _config = configRepository.GetConfig();
        _tmdbRepo = tmdbRepository;
        _guessRepo = guessRepository;
        _isDevelopment = env.IsDevelopment();

        _logger.LogDebug("-ctor({type})", type);
    }

    [HttpGet("heartbeat")]
    public ActionResult<string> Heartbeat()
    {
        _logger.LogDebug("Heartbeat");
        return "Success";
    }

    [HttpGet("target")]
    public async Task<ActionResult> GetTargetMovie(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date
    )
    {
        _logger.LogDebug("+GetTargetMovie({date})", date);
        try
        {
            MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);

            if (targetMovie is null)
            {
                return new NotFoundResult();
            }

            _logger.LogDebug("-GetTargetMovie({date})", date);
            return new OkObjectResult(targetMovie);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTargetMovie Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GetTargetMovie({date}", date);

            return new StatusCodeResult(500);

        }
    }

    [HttpGet("movielist")]
    public async Task<ActionResult> GetMovieList()
    {
        _logger.LogDebug("+GetMovieList");

        try
        {
            Dictionary<string, int> movies = await _tmdbRepo.GetMovieList();
            return new OkObjectResult(movies);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetMovieList Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);

            _logger.LogDebug("-GetMovieList");
            return new StatusCodeResult(500);
        }
    }

    [HttpGet("guess/{id}")]
    public async Task<ActionResult> GuessMovie(
            [FromQuery, Required, StringLength(10), RegularExpression(@"^\d{4}-\d{2}-\d{2}$")] string date,
            int id
    )
    {
        _logger.LogDebug("+GuessMovie({date}, {id})", date, id);
        try
        {
            MovieDto? guessMovie = await _tmdbRepo.GetMovieById(id);

            if (guessMovie is null)
            {
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            MovieDto? targetMovie = await _tmdbRepo.GetTargetMovie(date);

            if (targetMovie is null)
            {
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            GuessDto? guessDto = _guessRepo.Guess(guessMovie, targetMovie);

            if (guessDto is null)
            {
                _logger.LogError("GuessMovie: unable to create guess DTO");
                _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
                return new NotFoundResult();
            }

            _logger.LogDebug("-GuessMovie({date}, {id})", date, id);
            return new OkObjectResult(guessDto);

        }
        catch (Exception ex)
        {
            _logger.LogError("GuessMovie Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            _logger.LogDebug("-GuessMovie({date}, {id})", date, id);

            return new StatusCodeResult(500);
        }
    }

    [HttpGet("movie/{movieName}")]
    public async Task<ActionResult> GetMovie(string movieName)
    {
        if (!_isDevelopment)
        {
            return new NotFoundResult();
        }

        MovieDto? movie = await _tmdbRepo.GetMovie(movieName);
        if (movie is null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(movie!);
    }
}
