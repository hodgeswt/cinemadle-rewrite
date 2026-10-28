using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetPastGuessesRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);

public readonly record struct GetPastGuessesResponse(IEnumerable<int> PastGuessMediaIds);

public class GetPastGuessesHandler(DatabaseContext db) : IRequestHandler<GetPastGuessesRequest, GetPastGuessesResponse>
{
  public async Task<RequestValidationError?> Validate(GetPastGuessesRequest request)
  {
    if (request.IsCustomGame && await db.CustomGames.FirstOrDefaultAsync(x => x.Id == request.GameId) is null)
    {
      return new RequestValidationError("Custom game not found", 404);
    }

    return request.ValidateUser(db);
  }

  public Task<GetPastGuessesResponse> Handle(GetPastGuessesRequest request)
  {
    var userGuesses = request.IsAnon ? db.AnonUserGuesses : db.Guesses;

    IEnumerable<int> guesses = userGuesses
      .Where(x => x.GameId == request.GameId && x.UserId == request.UserId)
      .OrderBy(x => x.SequenceId)
      .Select(x => x.GuessMediaId);

    return Task.FromResult<GetPastGuessesResponse>(new(guesses));
  }
}