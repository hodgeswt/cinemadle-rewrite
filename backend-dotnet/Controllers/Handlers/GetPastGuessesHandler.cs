using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;

namespace Cinemadle.Controllers.Handlers;

public record GetPastGuessesRequest(
  bool IsAnon,
  string UserId,
  string GameId
) : RequestWithUser(IsAnon, UserId);

public readonly record struct GetPastGuessesResponse(IEnumerable<int> PastGuessMediaIds);

public class GetPastGuessesHandler(DatabaseContext db) : IRequestHandler<GetPastGuessesRequest, GetPastGuessesResponse>
{
  public async Task<RequestValidationError?> Validate(GetPastGuessesRequest request)
  {
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