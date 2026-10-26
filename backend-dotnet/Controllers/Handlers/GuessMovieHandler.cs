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
  int GameLength
) : RequestWithUser(IsAnon, UserId);

public class GuessMovieHandler(
  ITmdbRepository tmdbRepository,
  DatabaseContext db,
  IGuessRepository guessRepository,
  IHintRepository hintRepository
) : IRequestHandler<GuessMovieRequest, GuessDto?>
{
  private DbSet<UserGuess>? _guessTable = null;

  public async Task<ProcessingError?> PreValidate(GuessMovieRequest request)
  {
    _guessTable = request.IsAnon ? db.AnonUserGuesses : db.Guesses;
    
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

    return null;
  }

  private async Task<RequestValidationError?> UserCanGuess(
    GuessMovieRequest request
  )
  {
    var userGuesses = _guessTable!
      .Where(x => x.GameId == request.GameId && x.UserId == request.UserId)
      .OrderBy(x => x.SequenceId)!;
    MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(request.GameId);
    bool hasWon = targetMovie != null && userGuesses.Any(x => x.GuessMediaId == targetMovie.Id);

    if (userGuesses.Count() >= request.GameLength || hasWon)
    {
      return new RequestValidationError("User cannot guess", 403);
    }

    return null;
  }

  public async Task<GuessDto?> Handle(GuessMovieRequest request)
  {
    GuessDto? guessDto = await GuessMovieInternal(request.GuessId, request.GameId);

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

  private async Task<GuessDto?> GuessMovieInternal(int id, string date)
  {
    MovieDto? guessMovie = await tmdbRepository.GetMovieById(id);

    if (guessMovie is null)
    {
      return null;
    }

    MovieDto? targetMovie = await tmdbRepository.GetTargetMovie(date);

    if (targetMovie is null)
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
}