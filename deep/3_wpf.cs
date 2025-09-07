using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class WpfImageUpscaler
{
    public static void Main(string[] args)
    {
        if (args.Length < 3) {
            Console.WriteLine("Usage: WpfImageUpscaler <inputPath> <outputPath> <scaleFactor> [jpeg_quality=85]]");
            Console.WriteLine("Example: WpfImageUpscaler input.jpg output.png 2");
            return;
        }
        
        int quality = 85;
        if (args.Length == 4)
            quality = int.Parse((args[3]));

        string inputPath = args[0];
        string outputPath = args[1];
        double scaleFactor = double.Parse(args[2]);

        try
        {
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

            Console.WriteLine("Upscaling completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
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
}