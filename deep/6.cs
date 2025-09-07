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
            
            if (!arguments.ContainsKey("input"))
            {
                PrintUsage();
                return 1;
            }

            string inputPath = arguments["input"];
            
            // Set default values
            double scaleFactor = 2.0;  // Default scale factor
            int quality = 85;         // Default quality
            
            // Parse scale factor if provided
            if (arguments.ContainsKey("scale"))
            {
                if (!double.TryParse(arguments["scale"], out scaleFactor) || scaleFactor <= 0)
                {
                    Console.WriteLine("Error: Scale factor must be a positive number.");
                    return 1;
                }
            }

            // Parse quality if provided
            if (arguments.ContainsKey("quality"))
            {
                if (!int.TryParse(arguments["quality"], out quality) || quality < 1 || quality > 100)
                {
                    Console.WriteLine("Error: JPEG quality must be an integer between 1 and 100.");
                    return 1;
                }
            }

            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Input file not found: {inputPath}");
                return 1;
            }

            // Generate output filename if not provided
            string outputPath = arguments.ContainsKey("output") 
                ? arguments["output"] 
                : GenerateDefaultOutputPath(inputPath, scaleFactor);

            Console.WriteLine($"Using scaling factor: {scaleFactor}");
            Console.WriteLine($"Using JPEG quality: {quality}");

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

    private static string GenerateDefaultOutputPath(string inputPath, double scaleFactor)
    {
        string directory = Path.GetDirectoryName(inputPath);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        string extension = Path.GetExtension(inputPath);
        
        return Path.Combine(directory, $"{filenameWithoutExtension}_upscaled_{scaleFactor}x{extension}");
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
        Console.WriteLine("Usage: WpfImageUpscaler <inputPath> [outputPath] [/scale:<factor>] [/quality:<level>]");
        Console.WriteLine("       WpfImageUpscaler <inputPath> [outputPath] [/s:<factor>] [/q:<level>]");
        Console.WriteLine("Options:");
        Console.WriteLine("  inputPath    : Path to the input image (required)");
        Console.WriteLine("  outputPath   : Path for the output image (optional)");
        Console.WriteLine("  /scale:<n>   : Scaling factor (default: 2)");
        Console.WriteLine("  /quality:<n> : JPEG quality 1-100 (default: 85)");
        Console.WriteLine("  /s:<n>       : Shortcut for /scale");
        Console.WriteLine("  /q:<n>       : Shortcut for /quality");
        Console.WriteLine("Examples:");
        Console.WriteLine("  WpfImageUpscaler input.jpg /scale:1.5");
        Console.WriteLine("  WpfImageUpscaler input.jpg output.png /s:3 /q:90");
        Console.WriteLine("  WpfImageUpscaler input.jpg /q:75");
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