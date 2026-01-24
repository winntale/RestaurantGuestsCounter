namespace RestaurantGuestsCounter.Domain;

public class PeopleCountingRequestLog
{
    public DateTime Timestamp { get; set; }
    public string FileName { get; set; }
    public string RelativeImagePath { get; set; }
    public int GuestCount { get; set; }
}