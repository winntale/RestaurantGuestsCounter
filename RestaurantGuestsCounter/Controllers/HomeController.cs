using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using RestaurantGuestsCounter.Domain;
using RestaurantGuestsCounter.Models;
using RestaurantGuestsCounter.Services;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly IPeopleCountingService _peopleCountingService;
    private readonly IRequestHistoryService _historyService;

    public HomeController(
        IWebHostEnvironment env,
        IPeopleCountingService peopleCountingService,
        IRequestHistoryService historyService)
    {
        _env = env;
        _peopleCountingService = peopleCountingService;
        _historyService = historyService;
    }

    [HttpPost]
    public async Task<IActionResult> Index(ImageUploadViewModel model)
    {
        if (model.Image == null || model.Image.Length == 0)
        {
            ModelState.AddModelError("Image", "Файл не выбран");
            return View(model);
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var originalFileName = Path.GetFileNameWithoutExtension(model.Image.FileName);
        var extension = Path.GetExtension(model.Image.FileName);
        var safeFileName = $"{originalFileName}_{DateTime.UtcNow.Ticks}{extension}";

        var filePath = Path.Combine(uploadsFolder, safeFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await model.Image.CopyToAsync(stream);
        }

        model.SavedImagePath = "/uploads/" + safeFileName;


        // 1. считаем гостей
        var guestCount = _peopleCountingService.CountGuests(filePath);
        model.GuestCount = guestCount;

        // логируем именно safeFileName
        var logEntry = new PeopleCountingRequestLog
        {
            Timestamp = DateTime.UtcNow,
            FileName = safeFileName,
            RelativeImagePath = model.SavedImagePath,
            GuestCount = guestCount
        };
        
        await _historyService.AddAsync(logEntry);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var items = await _historyService.GetAllAsync();
        return View(items);
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ImageUploadViewModel());
    }
}
