namespace RestaurantGuestsCounter.Services;

public class DummyPeopleCountingService : IPeopleCountingService
{
    public int CountGuests(string imagePath)
    {
        return 3;
    }
}