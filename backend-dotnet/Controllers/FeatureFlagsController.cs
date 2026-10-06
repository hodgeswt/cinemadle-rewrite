using Cinemadle.Datamodel.DTO;
using Cinemadle.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cinemadle.Controllers;

[Route("api/flags")]
[ApiController]
public class FeatureFlagsController(IFeatureFlagRepository flagRepository, ILogger<FeatureFlagsController> logger) : CinemadleControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult> GetFeatureFlags()
    {
        logger.LogDebug("+GetFeatureFlags()");

        try
        {
            return new OkObjectResult(new FeatureFlagsDto
            {
                FeatureFlags = await flagRepository.GetAll()
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetFeatureFlags Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", ex.Message, ex.StackTrace, ex.InnerException?.Message);
            logger.LogDebug("-GetFeatureFlags()");

            return new StatusCodeResult(500);
        }
    }

    [HttpGet("{name}")]
    public async Task<ActionResult> GetFeatureFlag(string name)
    {
        try
        {
            return new OkObjectResult(new FeatureFlagDto
            {
                Name = name,
                Value = await flagRepository.Get(name)
            });
        }
        catch (Exception ex)
        {
            logger.LogError("GetFeatureFlag({name}) Exception. Message: {message}, StackTrace: {stackTrace}, InnerException: {innerException}", name, ex.Message, ex.StackTrace, ex.InnerException?.Message);
            return new StatusCodeResult(500);
        }
    }
}