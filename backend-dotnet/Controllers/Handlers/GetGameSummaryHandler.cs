using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetGameSummaryRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  int GameLength,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);
public class GetGameSummaryHandler(
  ITmdbRepository tmdbRepository,
  DatabaseContext db,
  IGuessRepository guessRepository
) : IRequestHandler<GetGameSummaryRequest, GameSummaryDto?>
{
  private GuessDto?[]? _guessDtos = null;
  private IEnumerable<UserGuess>? _userGuesses = null;
  private MovieDto? _targetMovie = null;

  public async Task<ProcessingError?> PreValidate(GetGameSummaryRequest request)
  {
    var guessTable = request.IsAnon ? db.AnonUserGuesses : db.Guesses;

    _userGuesses =
      guessTable
        .Where(x =>
          x.UserId == request.UserId &&
          x.GameId == request.GameId)
        .OrderBy(x => x.SequenceId);

    if (request.IsCustomGame)
    {
      if (await db.CustomGames.FirstOrDefaultAsync(x => x.Id == request.GameId) is not CustomGame customGame)
      {
        return new ProcessingError("Custom game not found", 404);
      }

      _targetMovie = await tmdbRepository.GetMovieById(customGame.TargetMovieId);
    }
    else
    {
      _targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);
    }
    
    return null;
  }
  
  public async Task<RequestValidationError?> Validate(GetGameSummaryRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    if (await UserCanGetSummary(request) is RequestValidationError userCannotGetSummaryErr)
    {
      return userCannotGetSummaryErr;
    }

    return null;
  }

  private async Task<RequestValidationError?> UserCanGetSummary(GetGameSummaryRequest request)
  {
    bool hasWon = _userGuesses!.Any(x => x.GuessMediaId == _targetMovie!.Id);

    if (_userGuesses!.Count() < request.GameLength && !hasWon)
    {
      return new RequestValidationError("User cannot get summary", 403);
    }

    return null;
  }


  public async Task<GameSummaryDto?> Handle(GetGameSummaryRequest request)
  {
    IEnumerable<string> gameSummary = _guessDtos!.Select(
      x => string.Join("", x!.Fields.Select(x => MapColorToEmoji(x.Value.Color)))
    ) ?? [];

    string url = request.IsCustomGame ? $"https://cinemadle.com/customGame/{request.GameId}" : "https://cinemadle.com";
    string title = request.IsCustomGame ? "cinemadle" : $"cinemadle {request.GameId}";
    gameSummary = gameSummary.Append(title).Append($"play at {url}");

    return new GameSummaryDto
    {
        Summary = [.. gameSummary]
    };
  }

  public async Task<ProcessingError?> PreProcess(GetGameSummaryRequest request)
  {
    var guessIds = _userGuesses!.Select(x => x.GuessMediaId);

    // sequential: TmdbRepository shares a scoped DbContext, which is not thread-safe
    var dtos = new List<GuessDto?>();
    foreach (var id in guessIds)
    {
      dtos.Add(await GuessMovieInternal(id));
    }

    _guessDtos = [.. dtos];

    if (_guessDtos is null || _guessDtos.Any(x => x is null))
    {
      return new ProcessingError("Unable to find movie guesses");
    }

    return null;
  }

  private async Task<GuessDto?> GuessMovieInternal(int id)
  {
      MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

      if (guessMovie is null)
      {
          return null;
      }

      GuessDto? guessDto = guessRepository.Guess(guessMovie, _targetMovie!);

      if (guessDto is null)
      {
          return null;
      }

      return guessDto;
  }

  private static string MapColorToEmoji(string color)
  {
      return color switch
      {
          "green" => "🟩",
          "yellow" => "🟨",
          _ => "⬛",
      };
  }
}