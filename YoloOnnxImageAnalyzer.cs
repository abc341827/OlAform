using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace OlAform;

public sealed class YoloOnnxImageAnalyzer : IDisposable
{
    private const string PreprocessMode = "Letterbox";
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string _outputName;
    private readonly string[] _labels;
    private readonly int _inputWidth;
    private readonly int _inputHeight;

    public YoloOnnxImageAnalyzer(string modelPath)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            throw new ArgumentException("模型路径不能为空。", nameof(modelPath));
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("找不到 ONNX 模型文件。", modelPath);
        }

        _session = new InferenceSession(modelPath);
        _inputName = _session.InputMetadata.Keys.FirstOrDefault()
            ?? throw new InvalidOperationException("模型缺少输入定义。");
        _outputName = _session.OutputMetadata.Keys.FirstOrDefault()
            ?? throw new InvalidOperationException("模型缺少输出定义。");

        var inputMetadata = _session.InputMetadata[_inputName];
        var dims = inputMetadata.Dimensions.ToArray();
        if (dims.Length != 4)
        {
            throw new NotSupportedException($"暂不支持该输入维度: {string.Join(", ", dims)}");
        }

        _inputHeight = dims[2] > 0 ? dims[2] : 640;
        _inputWidth = dims[3] > 0 ? dims[3] : 640;
        _labels = ReadEmbeddedLabels(_session.ModelMetadata.CustomMetadataMap);

        if (_labels.Length == 0)
        {
            throw new InvalidOperationException("模型中未找到可用的标签元数据。请确认 names/classes 元数据已嵌入到 ONNX 文件中。");
        }
    }

    public YoloOnnxModelInfo GetModelInfo()
    {
        var input = _session.InputMetadata[_inputName];
        var output = _session.OutputMetadata[_outputName];

        return new YoloOnnxModelInfo(
            _inputName,
            _outputName,
            input.Dimensions.ToArray(),
            output.Dimensions.ToArray(),
            _labels);
    }

    public IReadOnlyList<YoloDetection> AnalyzeImage(string imagePath, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("图片路径不能为空。", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("找不到图片文件。", imagePath);
        }

        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (image.Empty())
        {
            throw new InvalidOperationException($"无法读取图片: {imagePath}");
        }

        return AnalyzeImage(image, confidenceThreshold, iouThreshold);
    }

    public YoloOnnxAnalysisResult AnalyzeImageDetailed(string imagePath, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("图片路径不能为空。", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException("找不到图片文件。", imagePath);
        }

        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (image.Empty())
        {
            throw new InvalidOperationException($"无法读取图片: {imagePath}");
        }

        return AnalyzeImageDetailed(image, confidenceThreshold, iouThreshold);
    }

    public IReadOnlyList<YoloDetection> AnalyzeImage(Mat image, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f)
    {
        return AnalyzeImageDetailed(image, confidenceThreshold, iouThreshold).Detections;
    }

    public YoloOnnxAnalysisResult AnalyzeImageDetailed(Mat image, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        if (image.Empty())
        {
            throw new ArgumentException("图片内容为空。", nameof(image));
        }

        var (inputData, scale, padX, padY) = Preprocess(image);
        var tensor = new DenseTensor<float>(inputData, new[] { 1, 3, _inputHeight, _inputWidth });

        using var inferenceResults = _session.Run(new[]
        {
            NamedOnnxValue.CreateFromTensor(_inputName, tensor)
        });

        var outputTensor = inferenceResults.First(result => result.Name == _outputName).AsTensor<float>();
        var parseResult = ParseDetections(outputTensor, image.Width, image.Height, scale, padX, padY, confidenceThreshold);
        var detections = ApplyNms(parseResult.Detections, iouThreshold);

        return new YoloOnnxAnalysisResult(
            detections,
            new YoloOnnxDiagnostics(
                PreprocessMode,
                outputTensor.Dimensions.ToArray(),
                parseResult.ParserMode,
                parseResult.Predictions,
                parseResult.Attributes,
                parseResult.HasObjectness,
                parseResult.MaxObjectness,
                parseResult.MaxClassScore,
                parseResult.MaxConfidence));
    }

    public void SaveAnnotatedImage(string imagePath, string outputPath, float confidenceThreshold = 0.25f, float iouThreshold = 0.45f)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空。", nameof(outputPath));
        }

        using var image = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (image.Empty())
        {
            throw new InvalidOperationException($"无法读取图片: {imagePath}");
        }

        var detections = AnalyzeImage(image, confidenceThreshold, iouThreshold);
        DrawDetections(image, detections);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Cv2.ImWrite(outputPath, image);
    }

    public Mat CreateAnnotatedImage(Mat source, IReadOnlyList<YoloDetection> detections)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var annotated = source.Clone();
        DrawDetections(annotated, detections);
        return annotated;
    }

    public void DrawDetections(Mat image, IReadOnlyList<YoloDetection> detections)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(detections);

        foreach (var detection in detections)
        {
            var x = (int)MathF.Round(detection.X1);
            var y = (int)MathF.Round(detection.Y1);
            var width = (int)MathF.Round(detection.X2 - detection.X1);
            var height = (int)MathF.Round(detection.Y2 - detection.Y1);
            var rect = new Rect(x, y, Math.Max(1, width), Math.Max(1, height));

            var boxColor = new Scalar(0, 0, 255);
            var accentColor = new Scalar(0, 255, 255);
            Cv2.Rectangle(image, rect, boxColor, 4);

            var centerX = rect.X + (rect.Width / 2);
            var centerY = rect.Y + (rect.Height / 2);
            Cv2.DrawMarker(image, new OpenCvSharp.Point(centerX, centerY), accentColor, MarkerTypes.Cross, 24, 3);
            Cv2.Circle(image, new OpenCvSharp.Point(centerX, centerY), 12, accentColor, 3);

            if (rect.Width < 20 || rect.Height < 20)
            {
                var guideHalfSize = 18;
                var guideX1 = Math.Max(0, centerX - guideHalfSize);
                var guideY1 = Math.Max(0, centerY - guideHalfSize);
                var guideX2 = Math.Min(image.Width - 1, centerX + guideHalfSize);
                var guideY2 = Math.Min(image.Height - 1, centerY + guideHalfSize);
                Cv2.Rectangle(image, new Rect(guideX1, guideY1, Math.Max(1, guideX2 - guideX1), Math.Max(1, guideY2 - guideY1)), accentColor, 2);
            }

            var caption = $"{detection.Label} {detection.Confidence:P1}";
            var textSize = Cv2.GetTextSize(caption, HersheyFonts.HersheySimplex, 0.7, 2, out var baseline);
            var labelTop = Math.Max(0, rect.Y - 30);
            var labelLeft = Math.Max(0, rect.X);
            var labelRect = new Rect(
                labelLeft,
                labelTop,
                Math.Min(image.Width - labelLeft, Math.Max(1, textSize.Width + 12)),
                Math.Min(image.Height - labelTop, Math.Max(1, textSize.Height + baseline + 10)));
            Cv2.Rectangle(image, labelRect, boxColor, -1);
            var textOrigin = new OpenCvSharp.Point(labelRect.X + 6, labelRect.Y + labelRect.Height - baseline - 4);
            Cv2.PutText(image, caption, textOrigin, HersheyFonts.HersheySimplex, 0.7, Scalar.White, 2);
        }
    }

    private (float[] InputData, float Scale, float PadX, float PadY) Preprocess(Mat source)
    {
        var scale = Math.Min((float)_inputWidth / source.Width, (float)_inputHeight / source.Height);
        var resizedWidth = (int)Math.Round(source.Width * scale);
        var resizedHeight = (int)Math.Round(source.Height * scale);

        using var resized = new Mat();
        Cv2.Resize(source, resized, new OpenCvSharp.Size(resizedWidth, resizedHeight));

        var padWidth = _inputWidth - resizedWidth;
        var padHeight = _inputHeight - resizedHeight;
        var left = padWidth / 2;
        var right = padWidth - left;
        var top = padHeight / 2;
        var bottom = padHeight - top;

        using var padded = new Mat();
        Cv2.CopyMakeBorder(resized, padded, top, bottom, left, right, BorderTypes.Constant, new Scalar(114, 114, 114));

        using var rgb = new Mat();
        Cv2.CvtColor(padded, rgb, ColorConversionCodes.BGR2RGB);

        var inputData = new float[3 * _inputWidth * _inputHeight];
        var area = _inputWidth * _inputHeight;

        for (var y = 0; y < _inputHeight; y++)
        {
            for (var x = 0; x < _inputWidth; x++)
            {
                var pixel = rgb.At<Vec3b>(y, x);
                var pixelIndex = y * _inputWidth + x;
                inputData[pixelIndex] = pixel.Item0 / 255f;
                inputData[area + pixelIndex] = pixel.Item1 / 255f;
                inputData[(2 * area) + pixelIndex] = pixel.Item2 / 255f;
            }
        }

        return (inputData, scale, left, top);
    }

    private ParseDetectionsResult ParseDetections(
        Tensor<float> outputTensor,
        int originalWidth,
        int originalHeight,
        float scale,
        float padX,
        float padY,
        float confidenceThreshold)
    {
        var dims = outputTensor.Dimensions.ToArray();
        var data = outputTensor.ToArray();
        var shape = NormalizeOutputShape(dims);

        if (shape.Attributes is 6 or 7)
        {
            return ParseNmsDetections(data, shape, originalWidth, originalHeight, scale, padX, padY, confidenceThreshold);
        }

        var predictions = shape.Predictions;
        var attributes = shape.Attributes;
        var hasObjectness = InferObjectness(attributes, _labels.Length);
        var classOffset = hasObjectness ? 5 : 4;
        var classCount = attributes - classOffset;

        if (classCount <= 0)
        {
            throw new NotSupportedException($"无法从输出维度解析类别数量: {string.Join(", ", dims)}");
        }

        var detections = new List<YoloDetection>();
        var maxObjectness = 0f;
        var maxClassScore = 0f;
        var maxConfidence = 0f;

        for (var predictionIndex = 0; predictionIndex < predictions; predictionIndex++)
        {
            var cx = GetValue(data, predictionIndex, 0, shape);
            var cy = GetValue(data, predictionIndex, 1, shape);
            var width = GetValue(data, predictionIndex, 2, shape);
            var height = GetValue(data, predictionIndex, 3, shape);
            var objectness = hasObjectness ? GetValue(data, predictionIndex, 4, shape) : 1f;
            if (objectness > maxObjectness)
            {
                maxObjectness = objectness;
            }

            var bestClassId = -1;
            var bestClassScore = 0f;
            for (var classIndex = 0; classIndex < classCount; classIndex++)
            {
                var classScore = GetValue(data, predictionIndex, classOffset + classIndex, shape);
                if (classScore > bestClassScore)
                {
                    bestClassScore = classScore;
                    bestClassId = classIndex;
                }
            }

            if (bestClassScore > maxClassScore)
            {
                maxClassScore = bestClassScore;
            }

            var confidence = bestClassScore * objectness;
            if (confidence > maxConfidence)
            {
                maxConfidence = confidence;
            }

            if (bestClassId < 0 || confidence < confidenceThreshold)
            {
                continue;
            }

            var x1 = (cx - (width / 2f) - padX) / scale;
            var y1 = (cy - (height / 2f) - padY) / scale;
            var x2 = (cx + (width / 2f) - padX) / scale;
            var y2 = (cy + (height / 2f) - padY) / scale;

            x1 = Math.Clamp(x1, 0f, originalWidth - 1f);
            y1 = Math.Clamp(y1, 0f, originalHeight - 1f);
            x2 = Math.Clamp(x2, 0f, originalWidth - 1f);
            y2 = Math.Clamp(y2, 0f, originalHeight - 1f);

            if (x2 <= x1 || y2 <= y1)
            {
                continue;
            }

            detections.Add(new YoloDetection(
                bestClassId,
                bestClassId < _labels.Length ? _labels[bestClassId] : bestClassId.ToString(CultureInfo.InvariantCulture),
                confidence,
                x1,
                y1,
                x2,
                y2));
        }

        return new ParseDetectionsResult(
            detections,
            hasObjectness ? "RawYoloWithObjectness" : "RawYoloWithoutObjectness",
            predictions,
            attributes,
            hasObjectness,
            maxObjectness,
            maxClassScore,
            maxConfidence);
    }

    private ParseDetectionsResult ParseNmsDetections(
        float[] data,
        NormalizedOutputShape shape,
        int originalWidth,
        int originalHeight,
        float scale,
        float padX,
        float padY,
        float confidenceThreshold)
    {
        var detections = new List<YoloDetection>();
        var maxConfidence = 0f;

        for (var predictionIndex = 0; predictionIndex < shape.Predictions; predictionIndex++)
        {
            var x1 = GetValue(data, predictionIndex, 0, shape);
            var y1 = GetValue(data, predictionIndex, 1, shape);
            var x2 = GetValue(data, predictionIndex, 2, shape);
            var y2 = GetValue(data, predictionIndex, 3, shape);
            var confidence = GetValue(data, predictionIndex, 4, shape);
            var classIdValue = GetValue(data, predictionIndex, 5, shape);

            if (confidence > maxConfidence)
            {
                maxConfidence = confidence;
            }

            if (confidence < confidenceThreshold)
            {
                continue;
            }

            var classId = Math.Max(0, (int)MathF.Round(classIdValue));
            x1 = (x1 - padX) / scale;
            y1 = (y1 - padY) / scale;
            x2 = (x2 - padX) / scale;
            y2 = (y2 - padY) / scale;

            x1 = Math.Clamp(x1, 0f, originalWidth - 1f);
            y1 = Math.Clamp(y1, 0f, originalHeight - 1f);
            x2 = Math.Clamp(x2, 0f, originalWidth - 1f);
            y2 = Math.Clamp(y2, 0f, originalHeight - 1f);

            if (x2 <= x1 || y2 <= y1)
            {
                continue;
            }

            detections.Add(new YoloDetection(
                classId,
                classId < _labels.Length ? _labels[classId] : classId.ToString(CultureInfo.InvariantCulture),
                confidence,
                x1,
                y1,
                x2,
                y2));
        }

        return new ParseDetectionsResult(
            detections,
            "NmsBoxes",
            shape.Predictions,
            shape.Attributes,
            false,
            1f,
            maxConfidence,
            maxConfidence);
    }

    private static NormalizedOutputShape NormalizeOutputShape(int[] dims)
    {
        if (dims.Length == 2)
        {
            return new NormalizedOutputShape(dims[0], dims[1], false);
        }

        if (dims.Length != 3)
        {
            throw new NotSupportedException($"暂不支持该输出维度: {string.Join(", ", dims)}");
        }

        var dim1 = dims[1];
        var dim2 = dims[2];
        var attributesFirst = dim1 < dim2;

        return attributesFirst
            ? new NormalizedOutputShape(dim2, dim1, true)
            : new NormalizedOutputShape(dim1, dim2, false);
    }

    private static bool InferObjectness(int attributes, int labelCount)
    {
        if (attributes == labelCount + 5)
        {
            return true;
        }

        if (attributes == labelCount + 4)
        {
            return false;
        }

        return false;
    }

    private static float GetValue(float[] buffer, int predictionIndex, int attributeIndex, NormalizedOutputShape shape)
    {
        if (shape.IsAttributeFirst)
        {
            return buffer[(attributeIndex * shape.Predictions) + predictionIndex];
        }

        return buffer[(predictionIndex * shape.Attributes) + attributeIndex];
    }

    private static IReadOnlyList<YoloDetection> ApplyNms(List<YoloDetection> detections, float iouThreshold)
    {
        var results = new List<YoloDetection>();

        foreach (var grouped in detections.GroupBy(item => item.ClassId))
        {
            var candidates = grouped.OrderByDescending(item => item.Confidence).ToList();
            while (candidates.Count > 0)
            {
                var current = candidates[0];
                results.Add(current);
                candidates.RemoveAt(0);

                candidates = candidates
                    .Where(candidate => CalculateIoU(current, candidate) < iouThreshold)
                    .ToList();
            }
        }

        return results.OrderByDescending(item => item.Confidence).ToList();
    }

    private static float CalculateIoU(YoloDetection left, YoloDetection right)
    {
        var interX1 = Math.Max(left.X1, right.X1);
        var interY1 = Math.Max(left.Y1, right.Y1);
        var interX2 = Math.Min(left.X2, right.X2);
        var interY2 = Math.Min(left.Y2, right.Y2);

        var interWidth = Math.Max(0f, interX2 - interX1);
        var interHeight = Math.Max(0f, interY2 - interY1);
        var interArea = interWidth * interHeight;

        var leftArea = (left.X2 - left.X1) * (left.Y2 - left.Y1);
        var rightArea = (right.X2 - right.X1) * (right.Y2 - right.Y1);
        var unionArea = leftArea + rightArea - interArea;

        return unionArea <= 0f ? 0f : interArea / unionArea;
    }

    private static string[] ReadEmbeddedLabels(IReadOnlyDictionary<string, string> metadata)
    {
        foreach (var key in new[] { "names", "classes", "labels" })
        {
            if (!metadata.TryGetValue(key, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var labels = ParseLabels(rawValue);
            if (labels.Length > 0)
            {
                return labels;
            }
        }

        return Array.Empty<string>();
    }

    private static string[] ParseLabels(string rawValue)
    {
        var trimmed = rawValue.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return Array.Empty<string>();
        }

        if (!LooksLikeStructuredLabels(trimmed))
        {
            return trimmed
                .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        var normalized = NormalizeMetadataJson(trimmed);
        using var jsonDocument = JsonDocument.Parse(normalized);
        if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
        {
            return jsonDocument.RootElement
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
        {
            return jsonDocument.RootElement
                .EnumerateObject()
                .OrderBy(property => ParseNumericPrefix(property.Name))
                .Select(property => property.Value.GetString() ?? string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private static bool LooksLikeStructuredLabels(string value)
    {
        return value.StartsWith('{') || value.StartsWith('[');
    }

    private static string NormalizeMetadataJson(string value)
    {
        var normalized = value.Replace("'", "\"");
        normalized = Regex.Replace(normalized, @"(?<=[\{,]\s*)(\d+)\s*:", "\"$1\":");
        return normalized;
    }

    private static int ParseNumericPrefix(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : int.MaxValue;
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    private readonly record struct NormalizedOutputShape(int Predictions, int Attributes, bool IsAttributeFirst);

    private readonly record struct ParseDetectionsResult(
        List<YoloDetection> Detections,
        string ParserMode,
        int Predictions,
        int Attributes,
        bool HasObjectness,
        float MaxObjectness,
        float MaxClassScore,
        float MaxConfidence);
}

public sealed record YoloOnnxModelInfo(
    string InputName,
    string OutputName,
    IReadOnlyList<int> InputShape,
    IReadOnlyList<int> OutputShape,
    IReadOnlyList<string> Labels);

public sealed record YoloDetection(
    int ClassId,
    string Label,
    float Confidence,
    float X1,
    float Y1,
    float X2,
    float Y2);

public sealed record YoloOnnxAnalysisResult(
    IReadOnlyList<YoloDetection> Detections,
    YoloOnnxDiagnostics Diagnostics);

public sealed record YoloOnnxDiagnostics(
    string PreprocessMode,
    IReadOnlyList<int> OutputShape,
    string ParserMode,
    int Predictions,
    int Attributes,
    bool HasObjectness,
    float MaxObjectness,
    float MaxClassScore,
    float MaxConfidence);
