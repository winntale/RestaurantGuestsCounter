namespace RestaurantGuestsCounter.Domain;

public class Detection
{
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }
    public float Confidence { get; set; }
    public int ClassId { get; set; }
}