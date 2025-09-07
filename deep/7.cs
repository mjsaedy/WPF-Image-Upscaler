using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class WpfImageUpscaler
{
    private const double DefaultScaleFactor = 2.0;
    private const int DefaultQuality = 85;

    public static int Main(string[] args)
    {
        try
        {
            var parameters = ParseCommandLineArguments(args);
            ValidateInputFile(parameters.InputPath);
            
            parameters.OutputPath ??= GenerateDefaultOutputPath(parameters.InputPath, parameters.ScaleFactor);
            parameters.OutputPath = EnsureUniqueFilename(parameters.OutputPath);

            PrintOperationParameters(parameters);
            
            var upscaledImage = ProcessImage(parameters.InputPath, parameters.ScaleFactor);
            SaveImage(upscaledImage, parameters.OutputPath, parameters.Quality);

            Console.WriteLine($"Upscaling completed successfully! Saved to: {parameters.OutputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private class ProcessingParameters
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public double ScaleFactor { get; set; } = DefaultScaleFactor;
        public int Quality { get; set; } = DefaultQuality;
    }

    private static ProcessingParameters ParseCommandLineArguments(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            throw new ArgumentException("No arguments provided");
        }

        var parameters = new ProcessingParameters();
        var argumentQueue = new Queue<string>(args);

        while (argumentQueue.Count > 0)
        {
            string arg = argumentQueue.Dequeue();

            if (arg.StartsWith("/"))
            {
                ProcessSwitchArgument(arg, parameters);
            }
            else
            {
                ProcessPositionalArgument(arg, parameters);
            }
        }

        return parameters;
    }

    private static void ProcessSwitchArgument(string arg, ProcessingParameters parameters)
    {
        int colonIndex = arg.IndexOf(':');
        if (colonIndex <= 1) return;

        string key = arg.Substring(1, colonIndex - 1).ToLower();
        string value = arg.Substring(colonIndex + 1);

        switch (key)
        {
            case "scale":
            case "s":
                if (double.TryParse(value, out double scale) && scale > 0)
                    parameters.ScaleFactor = scale;
                else
                    throw new ArgumentException("Scale factor must be a positive number");
                break;

            case "quality":
            case "q":
                if (int.TryParse(value, out int quality) && quality >= 1 && quality <= 100)
                    parameters.Quality = quality;
                else
                    throw new ArgumentException("JPEG quality must be between 1 and 100");
                break;

            default:
                throw new ArgumentException($"Unknown switch: {key}");
        }
    }

    private static void ProcessPositionalArgument(string arg, ProcessingParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.InputPath))
        {
            parameters.InputPath = arg;
        }
        else if (string.IsNullOrEmpty(parameters.OutputPath))
        {
            parameters.OutputPath = arg;
        }
        else
        {
            throw new ArgumentException($"Unexpected argument: {arg}");
        }
    }

    private static void ValidateInputFile(string inputPath)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input file not found", inputPath);
    }

    private static string GenerateDefaultOutputPath(string inputPath, double scaleFactor)
    {
        string dir = Path.GetDirectoryName(inputPath);
        string name = Path.GetFileNameWithoutExtension(inputPath);
        string ext = Path.GetExtension(inputPath);
        return Path.Combine(dir, $"{name}_upscaled_{scaleFactor}x{ext}");
    }

    private static string EnsureUniqueFilename(string path)
    {
        if (!File.Exists(path)) return path;

        string dir = Path.GetDirectoryName(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);

        int counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{name}_{counter}{ext}");
            counter++;
        } while (File.Exists(newPath));

        Console.WriteLine($"Note: Output file exists. Using unique name: {Path.GetFileName(newPath)}");
        return newPath;
    }

    private static void PrintOperationParameters(ProcessingParameters parameters)
    {
        Console.WriteLine($"Input file: {parameters.InputPath}");
        Console.WriteLine($"Output file: {parameters.OutputPath}");
        Console.WriteLine($"Using scaling factor: {parameters.ScaleFactor}");
        Console.WriteLine($"Using JPEG quality: {parameters.Quality}");
    }

    private static TransformedBitmap ProcessImage(string inputPath, double scaleFactor)
    {
        InitializeWpfApplication();

        var originalImage = LoadImage(inputPath);
        Console.WriteLine($"Upscaling from {originalImage.PixelWidth}x{originalImage.PixelHeight} " +
                        $"to {(int)(originalImage.PixelWidth * scaleFactor)}x{(int)(originalImage.PixelHeight * scaleFactor)}");

        return new TransformedBitmap(originalImage, new ScaleTransform(scaleFactor, scaleFactor));
    }

    private static void InitializeWpfApplication()
    {
        new Application();
    }

    private static BitmapImage LoadImage(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(Path.GetFullPath(path));
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        return image;
    }

    private static void SaveImage(TransformedBitmap image, string outputPath, int quality)
    {
        using var stream = new FileStream(outputPath, FileMode.Create);
        var encoder = CreateEncoder(outputPath, quality);
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(stream);
    }

    private static BitmapEncoder CreateEncoder(string filePath, int quality)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        return extension switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = quality },
            ".png" => new PngBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".tiff" => new TiffBitmapEncoder(),
            _ => throw new NotSupportedException($"Unsupported file format: {extension}")
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Image Upscaler - WPF-based image scaling tool");
        Console.WriteLine("Usage: WpfImageUpscaler <inputPath> [outputPath] [/scale:<factor>] [/quality:<level>]");
        Console.WriteLine("Options:");
        Console.WriteLine("  inputPath       Path to input image (required)");
        Console.WriteLine("  outputPath      Output path (optional, will generate if not provided)");
        Console.WriteLine("  /scale:<factor> Scaling factor (default: 2.0)");
        Console.WriteLine("  /s:<factor>     Short form for scale");
        Console.WriteLine("  /quality:<1-100> JPEG quality (default: 85)");
        Console.WriteLine("  /q:<1-100>      Short form for quality");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  WpfImageUpscaler input.jpg");
        Console.WriteLine("  WpfImageUpscaler input.jpg output.png /s:1.5");
        Console.WriteLine("  WpfImageUpscaler input.jpg /scale:3 /q:90");
    }
}