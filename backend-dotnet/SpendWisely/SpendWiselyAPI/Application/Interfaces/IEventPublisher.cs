using SpendWiselyAPI.Application.Events;


namespace SpendWiselyAPI.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishEventAsync<T>(T Event , CancellationToken ct);
    }
}
