using System.Collections.Concurrent;
using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetPastGuessesRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);

public readonly record struct GetPastGuessesResponse(IEnumerable<GuessDto> PastGuesses);

public class GetPastGuessesHandler(
  DatabaseContext db,
  IGuessRepository guessRepository,
  ITmdbRepository tmdbRepository
) : IRequestHandler<GetPastGuessesRequest, GetPastGuessesResponse>
{
  private DbSet<UserGuess>? _guessTable = null;
  private MovieDto? _targetMovie = null;

  public async Task<ProcessingError?> PreValidate(GetPastGuessesRequest request)
  {
    _guessTable = request.IsAnon ? db.AnonUserGuesses : db.Guesses;
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

  public async Task<RequestValidationError?> Validate(GetPastGuessesRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    if (_targetMovie is null)
    {
      return new RequestValidationError("Unable to find target movie", 404);
    }

    return null;
  }

  public async Task<GetPastGuessesResponse> Handle(GetPastGuessesRequest request)
  {
    var guessIds = _guessTable!
      .Where(x => x.GameId == request.GameId && x.UserId == request.UserId)
      .Select(x => new Tuple<int, int>(x.SequenceId, x.GuessMediaId));

    ConcurrentBag<(int, GuessDto)> guesses = [];

    await Parallel.ForEachAsync(guessIds, async (guess, token) =>
    {
      var (seq, id) = guess;

      var guessDto = await guessRepository.GuessById(id, _targetMovie!, tmdbRepository);
      if (guessDto is not null)
      {
        guesses.Add((seq, guessDto));
      }
    });


    return new(guesses.OrderBy(x => x.Item1).Select(x => x.Item2));
  }
}