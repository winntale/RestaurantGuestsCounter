using RestaurantGuestsCounter.Domain;

namespace RestaurantGuestsCounter.Services;

public class StatisticsService : IStatisticsService
{
    private readonly IRequestHistoryService _historyService;

    public StatisticsService(IRequestHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task<PeopleCountingStatistics> GetStatisticsAsync()
    {
        var items = await _historyService.GetAllAsync();
        if (items == null || !items.Any())
        {
            return new PeopleCountingStatistics
            {
                TotalRequests = 0,
                AverageGuests = 0,
                MaxGuests = 0,
                MinGuests = 0
            };
        }

        return new PeopleCountingStatistics
        {
            TotalRequests = items.Count,
            AverageGuests = items.Average(x => x.GuestCount),
            MaxGuests = items.Max(x => x.GuestCount),
            MinGuests = items.Min(x => x.GuestCount)
        };
    }
}
