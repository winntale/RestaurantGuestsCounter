using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using RestaurantGuestsCounter.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RestaurantGuestsCounter.Services;

public class OnnxPeopleCountingService : IPeopleCountingService, IDisposable
{
    private readonly InferenceSession? _session;

    public OnnxPeopleCountingService(IWebHostEnvironment env)
    {
        var modelPath = Path.Combine(env.ContentRootPath, "yolov8n.onnx");
        
        Console.WriteLine(modelPath);
        if (File.Exists(modelPath))
        {
            _session = new InferenceSession(modelPath);
        }
    }

    public int CountGuests(string imagePath)
    {
        if (_session == null)
            return 3;

        const int targetSize = 640;

        using var image = Image.Load<Rgb24>(imagePath);
        image.Mutate(x => x.Resize(targetSize, targetSize));

        var inputTensor = CreateInputTensorFromImage(image);

        var inputName = "images";
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First().AsTensor<float>();

        var detections = ParseYoloV8Output(
            output,
            targetSize,
            targetSize,
            confThreshold: 0.32f,
            iouThreshold: 0.6f);

        Console.WriteLine($"Detections total (after NMS): {detections.Count}");
        foreach (var d in detections)
        {
            Console.WriteLine($"Class={d.ClassId}, Conf={d.Confidence:F3}, Box=({d.X1:F1},{d.Y1:F1})-({d.X2:F1},{d.Y2:F1})");
        }
        
        return detections.Count;
    }


    private static Tensor<float> CreateInputTensorFromImage(Image<Rgb24> image)
    {
        int width = image.Width;
        int height = image.Height;
        var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

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

    private List<Detection> ParseYoloV8Output(
        Tensor<float> output, int imageWidth, int imageHeight,
        float confThreshold, float iouThreshold)
    {
        int channels = output.Dimensions[1];
        int numDetections = output.Dimensions[2];

        var detections = new List<Detection>();

        int numClasses = channels - 4;

        for (int i = 0; i < numDetections; i++)
        {
            float x = output[0, 0, i];
            float y = output[0, 1, i];
            float w = output[0, 2, i];
            float h = output[0, 3, i];

            if (i < 5)
            {
                Console.WriteLine($"raw bbox: x={x}, y={y}, w={w}, h={h}");
            }
            
            int bestClass = -1;
            float bestScore = 0f;

            for (int c = 0; c < numClasses; c++)
            {
                float classScore = output[0, 4 + c, i];
                if (classScore > bestScore)
                {
                    bestScore = classScore;
                    bestClass = c;
                }
            }

            if (bestScore < confThreshold)
                continue;
            
            if (bestClass != 0)
                continue;
            
            float x1 = x;
            float y1 = y;
            float x2 = x + w;
            float y2 = y + h;

            x1 = Math.Clamp(x1, 0, imageWidth);
            y1 = Math.Clamp(y1, 0, imageHeight);
            x2 = Math.Clamp(x2, 0, imageWidth);
            y2 = Math.Clamp(y2, 0, imageHeight);
            
            float boxWidth = x2 - x1;
            float boxHeight = y2 - y1;
            
            if (boxWidth < 10 || boxHeight < 20)
                continue;

            detections.Add(new Detection
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Confidence = bestScore,
                ClassId = bestClass
            });
        }

        var nms = NonMaxSuppression(detections, iouThreshold);
        return nms;
    }


    private static List<Detection> NonMaxSuppression(List<Detection> detections, float iouThreshold)
    {
        var result = new List<Detection>();

        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();

        while (sorted.Count > 0)
        {
            var current = sorted[0];
            result.Add(current);
            sorted.RemoveAt(0);

            sorted = sorted
                .Where(d => IoU(current, d) < iouThreshold)
                .ToList();
        }

        return result;
    }

    private static float IoU(Detection a, Detection b)
    {
        float areaA = (a.X2 - a.X1) * (a.Y2 - a.Y1);
        float areaB = (b.X2 - b.X1) * (b.Y2 - b.Y1);
        if (areaA <= 0 || areaB <= 0) return 0;

        float x1 = Math.Max(a.X1, b.X1);
        float y1 = Math.Max(a.Y1, b.Y1);
        float x2 = Math.Min(a.X2, b.X2);
        float y2 = Math.Min(a.Y2, b.Y2);

        float interW = Math.Max(0, x2 - x1);
        float interH = Math.Max(0, y2 - y1);
        float inter = interW * interH;

        return inter / (areaA + areaB - inter);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}

