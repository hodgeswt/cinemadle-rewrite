using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Cinemadle.Utilities;
using Microsoft.Extensions.Options;

namespace Cinemadle.Controllers.Handlers;

public record GetMovieImageRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);

public class GetMovieImageHandler(
  DatabaseContext db,
  ITmdbRepository tmdbRepository,
  IOptions<CinemadleConfig> configRepository
) : IRequestHandler<GetMovieImageRequest, ImageDto?>
{
  private int? _userGuesses = null;
  private CinemadleConfig? _config = null;
  private MovieDto? _targetMovie = null;
  private bool _win = false;

  public async Task<ProcessingError?> PreProcess(GuessMovieRequest request)
  {
    var guessTable = request.IsAnon ? db.AnonUserGuesses : db.Guesses;
    _userGuesses = guessTable.Where(x => x.GameId == request.GameId && x.UserId == request.UserId).Count();

    _config = configRepository.Value;

    _targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);

    if (_targetMovie is null)
    {
      return new ProcessingError("Unable to find target movie", 500);
    }

    _win = guessTable.Where(x => x.GameId == request.GameId && x.GuessMediaId == _targetMovie!.Id).Any();

    // If we've not won, and this is not an approved blurred-image guess, we're not authorized
    if (!_win && !_config!.MovieImageBlurFactors.ContainsKey(_userGuesses.ToString()!))
    {
      return new ProcessingError("Not allowed to see blurred image", 401);
    }

    return null;
  }

  public async Task<RequestValidationError?> Validate(GetMovieImageRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    return null;
  }

  protected async Task<ImageDto?> GetBlurredImage(float blurFactor)
  {
    var imageBytes = await tmdbRepository.GetMovieImageById(_targetMovie!.Id);

    if (imageBytes is null)
    {
      return null;
    }

    return new ImageDto
    {
      ImageData = ImageUtilities.Blur(imageBytes, blurFactor)
    };
  }

  public async Task<ImageDto?> Handle(GetMovieImageRequest request)
  {
    float blurFactor =
      (_userGuesses >= _config!.GameLength || _win)
      ? 0.0F
      : _config.MovieImageBlurFactors[_userGuesses.ToString()!];

    ImageDto? image = await GetBlurredImage(blurFactor);

    if (image is null)
    {
      return null;
    }

    Clue? clue = db.UserClues
      .Where(x =>
        x.GameId == request.GameId &&
        x.UserId == request.UserId &&
        x.ClueType == ClueType.Visual
      )
      .FirstOrDefault();

    if (clue is null)
    {
      db.UserClues.Add(new Clue
      {
        UserId = request.UserId,
        GameId = request.GameId,
        ClueType = ClueType.Visual,
        Inserted = DateTime.Now
      });

      await db.SaveChangesAsync();
    }

    return image;
  }
}