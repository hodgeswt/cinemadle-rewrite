using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetTargetMovie(
  bool IsAnon,
  string UserId,
  string GameId,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);
public class GetTargetMovieHandler(
  DatabaseContext db,
  ITmdbRepository tmdbRepository
) : IRequestHandler<GetTargetMovie, MovieDto?>
{

  public async Task<RequestValidationError?> Validate(GetTargetMovie request)
  {
    if (request.IsCustomGame && request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    return null;
  }
  public async Task<MovieDto?> Handle(GetTargetMovie request)
  {
    Func<string, Task<MovieDto?>> handler = request.IsCustomGame switch
    {
      true =>  HandleCustomGame,
      false => HandleNonCustomGame
    };

    return await handler(request.GameId);
  }

  private async Task<MovieDto?> HandleNonCustomGame(string gameId)
  {
    return await tmdbRepository.GetTargetMovie(gameId);
  }

  private async Task<MovieDto?> HandleCustomGame(string gameId)
  {
    if (await db.CustomGames.FirstOrDefaultAsync(x => x.Id == gameId) is not CustomGame customGame)
    {
      return null;
    }

    if (await tmdbRepository.GetMovieById(customGame.TargetMovieId) is not MovieDto targetMovie)
    {
      return null;
    }

    return targetMovie;
  }
}