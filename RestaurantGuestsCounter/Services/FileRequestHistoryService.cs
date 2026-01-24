using System.Text.Json;
using RestaurantGuestsCounter.Domain;

namespace RestaurantGuestsCounter.Services;

public class FileRequestHistoryService : IRequestHistoryService
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public FileRequestHistoryService(IWebHostEnvironment env)
    {
        var appData = Path.Combine(env.ContentRootPath, "App_Data");
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }

        _logFilePath = Path.Combine(appData, "requests.json");
    }

    public async Task AddAsync(PeopleCountingRequestLog entry)
    {
        List<PeopleCountingRequestLog> items;

        lock (_lock)
        {
            if (File.Exists(_logFilePath))
            {
                var json = File.ReadAllText(_logFilePath);
                items = string.IsNullOrWhiteSpace(json)
                    ? new List<PeopleCountingRequestLog>()
                    : JsonSerializer.Deserialize<List<PeopleCountingRequestLog>>(json)
                      ?? new List<PeopleCountingRequestLog>();
            }
            else
            {
                items = new List<PeopleCountingRequestLog>();
            }

            items.Add(entry);
            var newJson = JsonSerializer.Serialize(items, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_logFilePath, newJson);
        }
    }

    public async Task<IReadOnlyList<PeopleCountingRequestLog>> GetAllAsync()
    {
        if (!File.Exists(_logFilePath))
            return Array.Empty<PeopleCountingRequestLog>();

        var json = await File.ReadAllTextAsync(_logFilePath);
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<PeopleCountingRequestLog>();

        var items = JsonSerializer.Deserialize<List<PeopleCountingRequestLog>>(json)
                    ?? new List<PeopleCountingRequestLog>();

        return items;
    }
}
