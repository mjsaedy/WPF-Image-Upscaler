using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageFormatConverter
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 4)
            {
                Console.WriteLine("Usage: ImageFormatConverter.exe <inputFile> <outputFile> [jpegQuality] [--verbose]");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];
            int jpegQuality = 90; // default JPEG quality
            bool verbose = false;

            // Parse optional arguments
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i].Equals("--verbose", StringComparison.OrdinalIgnoreCase))
                {
                    verbose = true;
                    continue;
                }

                if (!int.TryParse(args[i], out jpegQuality))
                {
                    Console.WriteLine("Invalid JPEG quality value. Must be an integer between 1 and 100.");
                    return;
                }
            }

            if (jpegQuality < 1 || jpegQuality > 100)
            {
                Console.WriteLine("JPEG quality must be between 1 and 100.");
                return;
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Input file does not exist.");
                return;
            }

            // Warn before overwriting
            if (File.Exists(outputFile))
            {
                Console.Write($"Output file '{outputFile}' already exists. Overwrite? (y/N): ");
                var key = Console.ReadKey();
                Console.WriteLine();
                if (key.Key != ConsoleKey.Y)
                {
                    Console.WriteLine("Operation cancelled.");
                    return;
                }
            }

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(inputFile));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                BitmapFrame bitmapFrame = BitmapFrame.Create(bitmap);

                BitmapEncoder encoder;
                string extension = Path.GetExtension(outputFile).ToLowerInvariant();

                switch (extension)
                {
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        var jpegEncoder = new JpegBitmapEncoder { QualityLevel = jpegQuality };
                        encoder = jpegEncoder;
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    case ".gif":
                        encoder = new GifBitmapEncoder();
                        break;
                    case ".tiff":
                        encoder = new TiffBitmapEncoder();
                        break;
                    default:
                        Console.WriteLine("Unsupported output image format.");
                        return;
                }

                encoder.Frames.Add(bitmapFrame);

                using (var stream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }

                if (encoder is JpegBitmapEncoder)
                    Console.WriteLine($"Image converted successfully to {outputFile} (JPEG quality {jpegQuality}).");
                else
                    Console.WriteLine($"Image converted successfully to {outputFile}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during image conversion:");
                Console.WriteLine(verbose ? ex.ToString() : ex.Message);
            }
        }
    }
}
