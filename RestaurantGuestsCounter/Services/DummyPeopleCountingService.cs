namespace RestaurantGuestsCounter.Services;

public class DummyPeopleCountingService : IPeopleCountingService
{
    public int CountGuests(string imagePath)
    {
        // Здесь позже будет вызов ONNX/YOLO
        // Сейчас для отладки просто считаем «3 гостя»
        return 3;
    }
}