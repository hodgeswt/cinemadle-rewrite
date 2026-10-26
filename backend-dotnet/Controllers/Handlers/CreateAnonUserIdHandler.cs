using Cinemadle.Database;
using Cinemadle.Datamodel.Domain;
using Cinemadle.Exceptions;

namespace Cinemadle.Controllers.Handlers;

public record CreateAnonUserIdRequest();

public class CreateAnonUserIdHandler(DatabaseContext db) : IRequestHandler<CreateAnonUserIdRequest, string>
{
  public async Task<string> Handle(CreateAnonUserIdRequest request)
  {
    int count = 0;
    string userId = string.Empty;
    bool exists = true;

    while (exists && count < 5)
    {
      userId = Guid.NewGuid().ToString();
      exists = db.AnonUsers.Any(x => x.UserId == userId);
      count++;
    }

    if (string.IsNullOrWhiteSpace(userId))
    {
      throw new AnonUserCreationException();
    }

    db.AnonUsers.Add(new()
    {
      UserId = userId
    });

    await db.SaveChangesAsync();

    return userId;
  }
}