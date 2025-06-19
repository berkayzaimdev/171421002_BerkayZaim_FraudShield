namespace FraudShield.TransactionAnalysis.Domain.Common;

public interface IDomainEventService
{
    Task Publish(DomainEvent domainEvent);
}