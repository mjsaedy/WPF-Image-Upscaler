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
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ImageFormatConverter.exe <inputFile> <outputFile>");
                return;
            }

            string inputFile = args[0];
            string outputFile = args[1];

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Input file does not exist.");
                return;
            }

            try
            {
                // Load the input image
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(Path.GetFullPath(inputFile));
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Create a BitmapFrame from the BitmapImage
                BitmapFrame bitmapFrame = BitmapFrame.Create(bitmap);

                // Choose encoder based on output file extension
                BitmapEncoder encoder = null;
                string extension = Path.GetExtension(outputFile).ToLowerInvariant();

                switch (extension)
                {
                    case ".png":
                        encoder = new PngBitmapEncoder();
                        break;
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder();
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

                // Save to output file
                using (var stream = new FileStream(outputFile, FileMode.Create))
                {
                    encoder.Save(stream);
                }

                Console.WriteLine($"Image converted successfully to {outputFile}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during image conversion: " + ex.Message);
            }
        }
    }
}
