namespace FiapCloudGames.Contracts.IntegrationEvents;

public record PaymentProcessedIntegrationEvent
{
    public Guid UserId { get; init; }
    public Guid OrderId { get; init; }
    public PaymentStatusEnum Status { get; init; }
}

public enum PaymentStatusEnum
{
    Approved,
    Processing,
    Rejected
}
