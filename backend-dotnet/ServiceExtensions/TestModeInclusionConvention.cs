using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cinemadle.ServiceExtensions;

[AttributeUsage(AttributeTargets.Class)]
public class TestModeInclusionAttribute : Attribute;

public class TestModeInclusionConvention(bool isTestMode) : IApplicationModelConvention
{
    private static List<string> Removed { get; } = [];

    public static IReadOnlyCollection<string> ExcludedControllers => Removed.AsReadOnly();

    public void Apply(ApplicationModel application)
    {
        if (isTestMode) return;

        var controllersToRemove = application.Controllers
            .Where(c => c.ControllerType.IsDefined(typeof(TestModeInclusionAttribute), false))
            .ToList();

        foreach (var controller in controllersToRemove)
        {
            application.Controllers.Remove(controller);
            Removed.Add(controller.GetType().Name);
        }
    }
}