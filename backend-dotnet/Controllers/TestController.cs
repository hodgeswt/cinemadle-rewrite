using Cinemadle.Database;
using Cinemadle.Interfaces;
using Cinemadle.ServiceExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Controllers;

[Route("api/test")]
[TestModeInclusion]
[ApiController]
public class TestController(
    ILogger<TestController> logger,
    DatabaseContext db,
    IdentityContext identity,
    ITmdbRepository tmdbRepo
) : ControllerBase
{
    [HttpDelete("destroy")]
    public async Task<ActionResult> DestroyAllData()
    {
        try
        {
            db.Guesses.RemoveRange(db.Guesses);
            db.TargetMovies.RemoveRange(db.TargetMovies);
            db.DataOverrides.RemoveRange(db.DataOverrides);
            db.AnonUsers.RemoveRange(db.AnonUsers);
            db.AnonUserGuesses.RemoveRange(db.AnonUserGuesses);
            db.UserClues.RemoveRange(db.UserClues);
            db.UserAccounts.RemoveRange(db.UserAccounts);
            db.CustomGames.RemoveRange(db.CustomGames);

            await db.SaveChangesAsync();

            identity.UserLogins.RemoveRange(identity.UserLogins);
            identity.Users.RemoveRange(identity.Users);
            identity.UserClaims.RemoveRange(identity.UserClaims);
            identity.UserTokens.RemoveRange(identity.UserTokens);
            identity.UserRoles.RemoveRange(identity.UserRoles);

            await identity.SaveChangesAsync();

            return new OkResult();
        }
        catch (Exception ex)
        {
            logger.LogError("DestroyAllData Exception. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
            return new StatusCodeResult(500);
        }
    }

    [HttpGet("rig/{id:int}")]
    public ActionResult RigMovie(int id)
    {
        tmdbRepo.RigMovie(id);
        return new OkResult();
    }

    [HttpGet("rig/undo")]
    public ActionResult UnrigMovie()
    {
        tmdbRepo.UnrigMovie();
        return new OkResult();
    }
}