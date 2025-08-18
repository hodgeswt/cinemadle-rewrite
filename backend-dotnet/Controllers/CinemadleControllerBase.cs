namespace Cinemadle.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class CinemadleControllerBase : ControllerBase
{
    protected string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    protected bool UserHasRole(string claim)
    {
        return User.Claims.Where(x => x.Type == ClaimTypes.Role && x.Value == claim).Any();
    }
}