using RestaurantGuestsCounter.Domain;

namespace RestaurantGuestsCounter.Services;

public interface IStatisticsService
{
    Task<PeopleCountingStatistics> GetStatisticsAsync();
}