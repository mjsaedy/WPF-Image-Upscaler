using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class WpfImageUpscaler
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            var arguments = ParseArguments(args);
            
            if (!arguments.ContainsKey("input") || !arguments.ContainsKey("output") || !arguments.ContainsKey("scale"))
            {
                PrintUsage();
                return 1;
            }

            string inputPath = arguments["input"];
            string outputPath = arguments["output"];

            // Parse scale factor
            if (!double.TryParse(arguments["scale"], out double scaleFactor) || scaleFactor <= 0)
            {
                Console.WriteLine("Error: Scale factor must be a positive number.");
                return 1;
            }

            // Parse quality (default to 85 if not specified)
            int quality = 85;
            if (arguments.ContainsKey("quality") && 
                (!int.TryParse(arguments["quality"], out quality) || quality < 1 || quality > 100))
            {
                Console.WriteLine("Error: JPEG quality must be an integer between 1 and 100.");
                return 1;
            }

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Input file not found: {inputPath}");
                return 1;
            }

            // Generate unique output filename if needed
            outputPath = GetUniqueFilename(outputPath);

            // Initialize WPF's rendering engine (required for console app)
            new System.Windows.Application();

            // Load original image
            BitmapImage originalImage = new BitmapImage();
            originalImage.BeginInit();
            originalImage.UriSource = new Uri(Path.GetFullPath(inputPath));
            originalImage.CacheOption = BitmapCacheOption.OnLoad;
            originalImage.EndInit();

            Console.WriteLine($"Upscaling from {originalImage.PixelWidth}x{originalImage.PixelHeight} " +
                            $"to {(int)(originalImage.PixelWidth * scaleFactor)}x{(int)(originalImage.PixelHeight * scaleFactor)}");

            // Create transformed bitmap with high-quality scaling
            TransformedBitmap transformedBitmap = new TransformedBitmap(
                originalImage,
                new ScaleTransform(scaleFactor, scaleFactor));

            // Create encoder based on output file extension
            BitmapEncoder encoder = CreateEncoder(outputPath, quality);

            // Save with highest quality settings
            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                encoder.Save(stream);
            }

            Console.WriteLine($"Upscaling completed successfully! Saved to: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string arg in args)
        {
            if (arg.StartsWith("/"))
            {
                int colonIndex = arg.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = arg.Substring(1, colonIndex - 1).ToLower();
                    string value = arg.Substring(colonIndex + 1);

                    // Map shortcuts to full names
                    switch (key)
                    {
                        case "s": key = "scale"; break;
                        case "q": key = "quality"; break;
                    }

                    arguments[key] = value;
                }
            }
            else
            {
                // Handle positional arguments (input and output)
                if (!arguments.ContainsKey("input"))
                {
                    arguments["input"] = arg;
                }
                else if (!arguments.ContainsKey("output"))
                {
                    arguments["output"] = arg;
                }
            }
        }

        return arguments;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: WpfImageUpscaler <inputPath> <outputPath> /scale:<factor> [/quality:<level>]");
        Console.WriteLine("       WpfImageUpscaler <inputPath> <outputPath> /s:<factor> [/q:<level>]");
        Console.WriteLine("Example: WpfImageUpscaler input.jpg output.png /scale:2 /quality:90");
        Console.WriteLine("Example: WpfImageUpscaler input.jpg output.png /s:1.5 /q:75");
        Console.WriteLine("Note:");
        Console.WriteLine("  - Scale factor must be greater than 0");
        Console.WriteLine("  - JPEG quality must be between 1 and 100 (default: 85)");
        Console.WriteLine("  - If output file exists, a unique name will be generated automatically");
    }

    private static BitmapEncoder CreateEncoder(string filePath, int quality)
    {
        string extension = Path.GetExtension(filePath).ToLower();

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return new JpegBitmapEncoder { QualityLevel = quality };
            case ".png":
                return new PngBitmapEncoder();
            case ".bmp":
                return new BmpBitmapEncoder();
            case ".tiff":
                return new TiffBitmapEncoder();
            default:
                throw new NotSupportedException($"Unsupported file format: {extension}");
        }
    }

    private static string GetUniqueFilename(string originalPath)
    {
        if (!File.Exists(originalPath))
        {
            return originalPath;
        }

        string directory = Path.GetDirectoryName(originalPath);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);

        int counter = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(directory, $"{filenameWithoutExtension}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));

        Console.WriteLine($"Note: Output file exists. Using unique name: {Path.GetFileName(newPath)}");
        return newPath;
    }
}