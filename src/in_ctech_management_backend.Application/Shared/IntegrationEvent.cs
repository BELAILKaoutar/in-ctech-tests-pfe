namespace in_ctech_management_backend.Application.Shared
{
    public sealed record IntegrationEvent(Guid IntergrationEventId, DateTime OccuredAt, string Type, string AssemblyName, string Payload, DateTime? PublishedAt = null);
}
