using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Controllers.Handlers;

public record GetHintsRequest(
  bool IsAnon,
  string UserId,
  string GameId,
  bool IsCustomGame
) : RequestWithUser(IsAnon, UserId);
public class GetHintsHandler(
  IHintRepository hintRepository,
  DatabaseContext db
) : IRequestHandler<GetHintsRequest, Dictionary<string, HintsDto>>
{
  public async Task<RequestValidationError?> Validate(GetHintsRequest request)
  {
    if (request.ValidateUser(db) is RequestValidationError userValidationErr)
    {
      return userValidationErr;
    }

    return null;
  }
  
  public async Task<Dictionary<string, HintsDto>> Handle(GetHintsRequest request)
  {
    return await hintRepository.GetHints(request.UserId, request.GameId, request.IsAnon, request.IsCustomGame);
  }
}