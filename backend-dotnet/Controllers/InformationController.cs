using System.Reflection;
using Cinemadle.Database;
using Cinemadle.Datamodel.DTO;
using Cinemadle.Migrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cinemadle.Controllers;

[Route("api/information")]
[ApiController]
public class InformationController(DatabaseContext mainDb) : CinemadleControllerBase
{
  [HttpGet("version")]
  public ActionResult<DbVersionDto> Version()
  {

    var migrations = 
      typeof(InitialCreate).Assembly.GetTypes().Where(x => x.GetCustomAttribute<MigrationAttribute>() is not null);

    var mainDbMigrations = migrations
      .Where(x => x.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(DatabaseContext))
      .Select(x => x.GetCustomAttribute<MigrationAttribute>()?.Id);

    var identityDbMigrations = migrations
      .Where(x => x.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(IdentityContext))
      .Select(x => x.GetCustomAttribute<MigrationAttribute>()?.Id);
    
    var mainDbVersion = mainDb.Database.GetAppliedMigrations().Where(x => mainDbMigrations.Contains(x)).OrderByDescending(x => x).First();
    var identityDbVersion = mainDb.Database.GetAppliedMigrations().Where(x => identityDbMigrations.Contains(x)).OrderByDescending(x => x).First();
    
    return Ok(new DbVersionDto(mainDbVersion.Split('_')[0], identityDbVersion.Split('_')[0]));
  }
}