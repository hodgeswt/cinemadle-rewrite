using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;

namespace Cinemadle.Datamodel.Domain;

public abstract record RequestWithUser(
  bool IsAnon,
  string UserId
)
{
  public RequestValidationError? ValidateUser(DatabaseContext db)
  {
    if (string.IsNullOrEmpty(UserId) || (IsAnon && db.AnonUsers.Where(x => x.UserId == UserId).FirstOrDefault() is null))
    {
      return new RequestValidationError("Unauthorized access", 401);
    }

    return null;
  }
}