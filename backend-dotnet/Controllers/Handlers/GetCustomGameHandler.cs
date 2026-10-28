using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetCustomGameRequest(
  bool IsAnon,
  string UserId,
  string GameId
) : RequestWithUser(IsAnon, UserId);

public class GetCustomGameHandler(
  DatabaseContext db
) : IRequestHandler<GetCustomGameRequest, CustomGameDto?>
{
  public async Task<RequestValidationError?> Validate(GetCustomGameRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    return null;
  }

  public async Task<CustomGameDto?> Handle(GetCustomGameRequest request)
  {
    var c = await db.CustomGames.FirstOrDefaultAsync(x => x.Id == request.GameId);
    
    if (c is null)
    {
      return null;
    }

    return new CustomGameDto
    {
        Id = c.Id,
        TargetMovieId = c.TargetMovieId
    };
  }
}