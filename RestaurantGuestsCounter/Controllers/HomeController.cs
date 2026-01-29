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
    private readonly IStatisticsService _statisticsService;

    public HomeController(
        IWebHostEnvironment env,
        IPeopleCountingService peopleCountingService,
        IRequestHistoryService historyService,
        IStatisticsService statisticsService)
    {
        _env = env;
        _peopleCountingService = peopleCountingService;
        _historyService = historyService;
        _statisticsService = statisticsService;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View(new ImageUploadViewModel());
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


        var guestCount = _peopleCountingService.CountGuests(filePath);
        model.GuestCount = guestCount;

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
    public async Task<IActionResult> Stats()
    {
        var stats = await _statisticsService.GetStatisticsAsync();
        return View(stats);
    }
    
    [HttpGet]
    public async Task<IActionResult> DownloadCsvReport()
    {
        var items = await _historyService.GetAllAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Timestamp,FileName,GuestCount");

        foreach (var item in items)
        {
            var time = item.Timestamp.ToLocalTime().ToString("s");
            var line = $"{time},{item.FileName},{item.GuestCount}";
            sb.AppendLine(line);
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        var outputFileName = $"people_count_report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(bytes, "text/csv", outputFileName);
    }
}
