using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;

namespace Cinemadle.Controllers.Handlers;

public record CreateCustomGameRequest(
  bool IsAnon,
  string UserId,
  int MovieId
) : RequestWithUser(IsAnon, UserId);

public class CreateCustomGameHandler(
  DatabaseContext db,
  ITmdbRepository tmdbRepository
) : IRequestHandler<CreateCustomGameRequest, CustomGameDto>
{
  public async Task<RequestValidationError?> Validate(CreateCustomGameRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    if (await tmdbRepository.GetMovieById(request.MovieId) is null)
    {
      return new RequestValidationError($"Movie {request.MovieId} not found", 404);
    }

    return null;
  }

  public async Task<CustomGameDto> Handle(CreateCustomGameRequest request)
  {
    CustomGame newCustomGame = new()
    {
      Id = Guid.NewGuid().ToString(),
      TargetMovieId = request.MovieId,
      CreatorUserId = request.UserId,
      Inserted = DateTime.UtcNow
    };

    db.CustomGames.Add(newCustomGame);

    await db.SaveChangesAsync();

    return new CustomGameDto
    {
      Id = newCustomGame.Id,
      TargetMovieId = newCustomGame.TargetMovieId
    };
  }
}