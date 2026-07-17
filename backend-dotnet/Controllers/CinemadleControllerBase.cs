namespace Cinemadle.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

public class CinemadleControllerBase : ControllerBase
{
    protected string GetUserId()
    {
        Request.Headers.TryGetValue("X-Cinemadle-UserId", out var userId);
        
        return userId.FirstOrDefault() is not null ? userId.First() ?? string.Empty : string.Empty;
    }

    protected bool UserHasRole(string claim)
    {
        return User.Claims.Any(x => x.Type == ClaimTypes.Role && x.Value == claim);
    }
}