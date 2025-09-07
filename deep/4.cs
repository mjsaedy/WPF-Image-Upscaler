using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class WpfImageUpscaler
{
    public static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: WpfImageUpscaler <inputPath> <outputPath> <scaleFactor> [jpeg_quality=85]");
            Console.WriteLine("Example: WpfImageUpscaler input.jpg output.png 2");
            Console.WriteLine("Note: If output file exists, a unique name will be generated automatically.");
            return 1;
        }

        try
        {
            string inputPath = args[0];
            string outputPath = args[1];

            // Validate scale factor with TryParse
            if (!double.TryParse(args[2], out double scaleFactor) || scaleFactor <= 0)
            {
                Console.WriteLine("Error: Scale factor must be a positive number.");
                return 1;
            }

            // Validate quality with TryParse if provided
            int quality = 85;
            if (args.Length == 4 && (!int.TryParse(args[3], out quality) || quality < 1 || quality > 100))
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