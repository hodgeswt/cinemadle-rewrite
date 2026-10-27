using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetTargetMovieRequest(
  string UserId,
  string GameId,
  bool IsCustomGame
);
public class GetTargetMovieHandler(
  DatabaseContext db,
  ITmdbRepository tmdbRepository
) : IRequestHandler<GetTargetMovieRequest, MovieDto?>
{

  public async Task<RequestValidationError?> Validate(GetTargetMovieRequest request)
  {
    if (request.IsCustomGame && string.IsNullOrWhiteSpace(request.UserId))
    {
      return new RequestValidationError("Unauthorized", 401);
    }

    return null;
  }
  public async Task<MovieDto?> Handle(GetTargetMovieRequest request)
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