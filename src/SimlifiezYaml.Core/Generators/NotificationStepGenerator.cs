using SimlifiezYaml.Core.Abstractions;
using SimlifiezYaml.Core.Models;

namespace SimlifiezYaml.Core.Generators;

public sealed class NotificationStepGenerator : IStepGenerator
{
    private readonly INotificationYamlService _notificationService;

    public NotificationStepGenerator(INotificationYamlService notificationService) => _notificationService = notificationService;

    public IReadOnlyList<string> GenerateSteps(PipelineDefinition definition, string? environment = null)
    {
        var steps = new List<string>();
        foreach (var n in definition.Notifications)
        {
            if (n.NotifyOnSuccess)
                steps.AddRange(_notificationService.GenerateNotificationSteps(n, succeeded: true));
            if (n.NotifyOnFailure)
                steps.AddRange(_notificationService.GenerateNotificationSteps(n, succeeded: false));
        }
        return steps;
    }
}
