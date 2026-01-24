using RestaurantGuestsCounter.Domain;

namespace RestaurantGuestsCounter.Services;

public interface IRequestHistoryService
{
    Task AddAsync(PeopleCountingRequestLog entry);
    Task<IReadOnlyList<PeopleCountingRequestLog>> GetAllAsync();
}