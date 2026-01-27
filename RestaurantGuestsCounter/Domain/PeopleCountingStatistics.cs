namespace RestaurantGuestsCounter.Domain;

public class PeopleCountingStatistics
{
    public int TotalRequests { get; set; }
    public double AverageGuests { get; set; }
    public int MaxGuests { get; set; }
    public int MinGuests { get; set; }
}