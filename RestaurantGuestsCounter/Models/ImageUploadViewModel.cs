namespace RestaurantGuestsCounter.Models;

public class ImageUploadViewModel
{
    public IFormFile Image { get; set; }

    public int? GuestCount { get; set; }

    public string SavedImagePath { get; set; }
    public string ProcessedImagePath { get; set; }
}