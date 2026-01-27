using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RestaurantGuestsCounter.Services;

public class OnnxPeopleCountingService : IPeopleCountingService, IDisposable
{
    private readonly InferenceSession _session;

    public OnnxPeopleCountingService(IWebHostEnvironment env)
    {
        var modelPath = Path.Combine(env.ContentRootPath, "yolov8n.onnx");
        if (File.Exists(modelPath))
        {
            _session = new InferenceSession(modelPath);
        }
    }

    public int CountGuests(string imagePath)
    {
        if (_session == null)
        {
            // ВРЕМЕННО: заглушка, чтобы всё приложение жило
            return 3;
        }

        // Параметры должны соответствовать ONNX модели
        const int targetWidth = 640;
        const int targetHeight = 640;

        // 1. Препроцессинг: читаем изображение и превращаем в тензор
        using var image = Image.Load<Rgb24>(imagePath);
        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        var inputTensor = CreateInputTensorFromImage(image);

        // 2. Собираем входы
        var inputName = _session.InputMetadata.Keys.First();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        // 3. Запускаем инференс
        using var results = _session.Run(inputs);
        var output = results.First().AsEnumerable<float>().ToArray();

        // 4. Постпроцессинг YOLO‑выхода -> список детекций, фильтрация по ROI стола и подсчёт
        var guests = PostprocessYoloOutputAndCountGuests(output, targetWidth, targetHeight);

        return guests;
    }
    
    private static Tensor<float> CreateInputTensorFromImage(Image<Rgb24> image)
    {
        int width = image.Width;
        int height = image.Height;
        var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

        // ImageSharp даёт нам пиксели, проходим по ним и кладём в CHW‑формат
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = pixelRow[x];
                    tensor[0, 0, y, x] = pixel.R / 255f;
                    tensor[0, 1, y, x] = pixel.G / 255f;
                    tensor[0, 2, y, x] = pixel.B / 255f;
                }
            }
        });

        return tensor;
    }

    private int PostprocessYoloOutputAndCountGuests(float[] output, int width, int height)
    {
        // здесь будет:
        // - парсинг YOLOv8‑выхода (bbox, score, class)
        // - фильтрация по class == person
        // - пересечение с ROI стола
        // пока возвращаем заглушку
        return 3;
    }


    public void Dispose()
    {
        _session?.Dispose();
    }
}
