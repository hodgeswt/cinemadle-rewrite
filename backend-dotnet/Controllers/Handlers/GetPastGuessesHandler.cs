using Cinemadle.Database;
using Cinemadle.Mediation;

namespace Cinemadle.Controllers.Handlers;

public readonly record struct GetPastGuessesRequest(
  bool IsAnon,
  string UserId,
  string GameId
);

public readonly record struct GetPastGuessesResponse(IEnumerable<int> PastGuessMediaIds);

public class GetPastGuessesHandler(DatabaseContext db) : IRequestHandler<GetPastGuessesRequest, GetPastGuessesResponse>
{
  public async Task<RequestValidationError?> Validate(GetPastGuessesRequest request)
  {
    if (string.IsNullOrEmpty(request.UserId) || (request.IsAnon && db.AnonUsers.Where(x => x.UserId == request.UserId).FirstOrDefault() is null))
    {
      return new RequestValidationError("Unauthorized access", 401);
    }

    return null;
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