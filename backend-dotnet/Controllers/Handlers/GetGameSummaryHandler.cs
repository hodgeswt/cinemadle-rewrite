using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Cinemadle.Mediation;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public readonly record struct GetGameSummaryRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  int GameLength
);
public class GetGameSummaryHandler(
  ITmdbRepository tmdbRepository,
  DatabaseContext db,
  IGuessRepository guessRepository
) : IRequestHandler<GetGameSummaryRequest, GameSummaryDto?>
{
  private DbSet<UserGuess>? _userGuesses = null;
  private GuessDto?[]? _guessDtos = null;
  
  public async Task<List<RequestValidationError>> Validate(GetGameSummaryRequest request)
  {
    if (ValidateUser(request) is RequestValidationError userValidationErr)
    {
      return [userValidationErr];
    }

    if (await UserCanGuess(request) is RequestValidationError userCannotGuessErr)
    {
      return [userCannotGuessErr];
    }

    return [];
  }

  private async Task<RequestValidationError?> UserCanGuess(GetGameSummaryRequest request)
  {
    IEnumerable<UserGuess> userGuesses =
      _userGuesses!.Where(x =>
        x.UserId == request.UserId &&
        x.GameId == request.GameId
      ).OrderBy(x => x.SequenceId);
    
    // Check if user has won
    MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);
    bool hasWon = targetMovie != null && userGuesses.Any(x => x.GuessMediaId == targetMovie.Id);

    // Allow summary if user has completed the game OR won
    if (userGuesses.Count() < request.GameLength && !hasWon)
    {
      // TODO : this should be 403
      return new RequestValidationError("User cannot guess yet", 404);
    }

    return null;
  }

  private RequestValidationError? ValidateUser(GetGameSummaryRequest request)
  {
    if (string.IsNullOrEmpty(request.UserId) || (request.IsAnon && db.AnonUsers.Where(x => x.UserId == request.UserId).FirstOrDefault() is null))
    {
      return new RequestValidationError("Unauthorized access", 401);
    }

    return null;
  }

  public async Task<GameSummaryDto?> Handle(GetGameSummaryRequest request)
  {
    _userGuesses = request.IsAnon ? db.AnonUserGuesses : db.Guesses;
    return await HandleInternal(request);
  }

  public async Task<ProcessingError?> PreProcess(GetGameSummaryRequest request)
  {
    var guessIds = _userGuesses!.Select(x => x.GuessMediaId);
    _guessDtos = await Task.WhenAll(guessIds.Select(x => GuessMovieInternal(x, request.GameId)));

    if (_guessDtos is null || _guessDtos.Any(x => x is null))
    {
      return new ProcessingError("Unable to find movie guesses");
    }

    return null;
  }

  private async Task<GameSummaryDto> HandleInternal(GetGameSummaryRequest request)
  {
    IEnumerable<string> gameSummary = _guessDtos!.Select(
      x => string.Join("", x!.Fields.Select(x => MapColorToEmoji(x.Value.Color)))
    ) ?? [];

    gameSummary = gameSummary.Append($"cinemadle {request.GameId}").Append("play at https://cinemadle.com");

    return new GameSummaryDto
    {
        Summary = [.. gameSummary]
    };
  }

  private async Task<GuessDto?> GuessMovieInternal(int id, string date)
  {
      MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

      if (guessMovie is null)
      {
          return null;
      }

      MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);

      if (targetMovie is null)
      {
          return null;
      }

      GuessDto? guessDto = guessRepository.Guess(guessMovie, targetMovie);

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