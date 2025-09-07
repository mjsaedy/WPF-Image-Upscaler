using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageFormatConverter
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Usage: ImageFormatConverter.exe <inputFile> <outputFile> [jpegQuality]");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];
            int jpegQuality = 90; // default quality level for JPEG

            if (args.Length == 3 && !int.TryParse(args[2], out jpegQuality))
            {
                Console.WriteLine("Invalid JPEG quality value. It must be an integer between 1 and 100.");
                return;
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

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(inputFile));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                BitmapFrame bitmapFrame = BitmapFrame.Create(bitmap);

                BitmapEncoder encoder = null;
                string extension = Path.GetExtension(outputFile).ToLowerInvariant();

                switch (extension)
                {
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        var jpegEncoder = new JpegBitmapEncoder();
                        jpegEncoder.QualityLevel = jpegQuality;
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

                using (var stream = new FileStream(outputFile, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                Console.WriteLine($"Image converted successfully to {outputFile} with JPEG quality {jpegQuality}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during image conversion: " + ex.Message);
            }
        }
    }
}
