using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GuessMovieRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  int GuessId,
  int GameLength,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);

public class GuessMovieHandler(
  ITmdbRepository tmdbRepository,
  DatabaseContext db,
  IGuessRepository guessRepository,
  IHintRepository hintRepository
) : IRequestHandler<GuessMovieRequest, GuessDto?>
{
  private DbSet<UserGuess>? _guessTable = null;
  private MovieDto? _targetMovie = null;

  public async Task<ProcessingError?> PreValidate(GuessMovieRequest request)
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

  public async Task<RequestValidationError?> Validate(GuessMovieRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    if (await UserCanGuess(request) is RequestValidationError userCannotGuessErr)
    {
      return userCannotGuessErr;
    }

    if (request.IsCustomGame && await db.CustomGames.FirstOrDefaultAsync(x => x.Id == request.GameId) is null)
    {
      return new RequestValidationError("Custom game not found", 404);
    }

    return null;
  }

  private async Task<RequestValidationError?> UserCanGuess(
    GuessMovieRequest request
  )
  {
    var userGuesses = _guessTable!
      .Where(x => x.GameId == request.GameId && x.UserId == request.UserId)
      .OrderBy(x => x.SequenceId)!;

    bool hasWon = _targetMovie is not null && userGuesses.Any(x => x.GuessMediaId == _targetMovie!.Id);

    if (userGuesses.Count() >= request.GameLength || hasWon)
    {
      return new RequestValidationError("User cannot guess", 403);
    }

    return null;
  }

  public async Task<GuessDto?> Handle(GuessMovieRequest request)
  {
    GuessDto? guessDto = await GuessMovieInternal(request.GuessId);

    UserGuess? x = _guessTable!.FirstOrDefault(
        x => x.GuessMediaId == request.GuessId && x.GameId == request.GameId && x.UserId == request.UserId
    );

    if (x is not null)
    {
      return guessDto;
    }

    int seqNo = (_guessTable!.Where(
        x => x.GameId == request.GameId && x.UserId == request.UserId
    )
    .OrderByDescending(x => x.SequenceId)
    .FirstOrDefault()?.SequenceId ?? 0) + 1;

    _guessTable!.Add(new UserGuess
    {
      GameId = request.GameId,
      UserId = request.UserId,
      GuessMediaId = request.GuessId,
      SequenceId = seqNo,
      Inserted = DateTime.Now,
    });

    await db.SaveChangesAsync();

    // Invalidate hints cache after new guess
    hintRepository.InvalidateHints(request.UserId, request.GameId);

    return guessDto;
  }

  private async Task<GuessDto?> GuessMovieInternal(int id)
  {
    MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

    if (guessMovie is null)
    {
      return null;
    }

    GuessDto? guessDto = guessRepository.Guess(guessMovie, _targetMovie!);

    if (guessDto is null)
    {
      return null;
    }

    return guessDto;
  }
}