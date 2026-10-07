using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Cinemadle.Mediation;

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
  private GuessDto?[]? _guessDtos = null;
  private IEnumerable<UserGuess>? _userGuesses = null;

  public async Task<ProcessingError?> PreValidate(GetGameSummaryRequest request)
  {
    var guessTable = request.IsAnon ? db.AnonUserGuesses : db.Guesses;

    _userGuesses =
      guessTable
        .Where(x =>
          x.UserId == request.UserId &&
          x.GameId == request.GameId)
        .OrderBy(x => x.SequenceId);
    
    return null;
  }
  
  public async Task<RequestValidationError?> Validate(GetGameSummaryRequest request)
  {
    if (ValidateUser(request) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    if (await UserCanGuess(request) is RequestValidationError userCannotGuessErr)
    {
      return userCannotGuessErr;
    }

    return null;
  }

  private async Task<RequestValidationError?> UserCanGuess(GetGameSummaryRequest request)
  {
    // Check if user has won
    MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);
    bool hasWon = targetMovie != null && _userGuesses!.Any(x => x.GuessMediaId == targetMovie.Id);

    // Allow summary if user has completed the game OR won
    if (_userGuesses!.Count() < request.GameLength && !hasWon)
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
    IEnumerable<string> gameSummary = _guessDtos!.Select(
      x => string.Join("", x!.Fields.Select(x => MapColorToEmoji(x.Value.Color)))
    ) ?? [];

    gameSummary = gameSummary.Append($"cinemadle {request.GameId}").Append("play at https://cinemadle.com");

    return new GameSummaryDto
    {
        Summary = [.. gameSummary]
    };
  }

  public async Task<ProcessingError?> PreProcess(GetGameSummaryRequest request)
  {
    var guessIds = _userGuesses!.Select(x => x.GuessMediaId);
    var targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);
    if (targetMovie is null)
    {
      return new ProcessingError("Unable to find target movie");
    }

    // sequential: TmdbRepository shares a scoped DbContext, which is not thread-safe
    var dtos = new List<GuessDto?>();
    foreach (var id in guessIds)
    {
      dtos.Add(await GuessMovieInternal(id, targetMovie));
    }
    _guessDtos = [.. dtos];

    if (_guessDtos is null || _guessDtos.Any(x => x is null))
    {
      return new ProcessingError("Unable to find movie guesses");
    }

    return null;
  }

  private async Task<GuessDto?> GuessMovieInternal(int id, MovieDto targetMovie)
  {
      MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

      if (guessMovie is null)
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