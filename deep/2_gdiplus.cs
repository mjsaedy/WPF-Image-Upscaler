using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class GdiPlusImageUpscaler
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: GdiPlusImageUpscaler <inputPath> <outputPath> <scaleFactor>");
            Console.WriteLine("Example: GdiPlusImageUpscaler input.jpg output.png 2");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        float scaleFactor = float.Parse(args[2]);

        try
        {
            using (Bitmap originalImage = new Bitmap(inputPath))
            {
                int newWidth = (int)(originalImage.Width * scaleFactor);
                int newHeight = (int)(originalImage.Height * scaleFactor);

                Console.WriteLine($"Upscaling from {originalImage.Width}x{originalImage.Height} to {newWidth}x{newHeight}");

                using (Bitmap upscaledImage = new Bitmap(newWidth, newHeight, originalImage.PixelFormat))
                {
                    // Configure high-quality GDI+ settings
                    using (Graphics g = Graphics.FromImage(upscaledImage))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;

                        // Perform the scaling
                        g.DrawImage(
                            originalImage,
                            new Rectangle(0, 0, newWidth, newHeight),
                            new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                            GraphicsUnit.Pixel);
                    }

                    // Save with highest quality settings
                    SaveImageWithQuality(upscaledImage, outputPath);
                }
            }

            Console.WriteLine("Upscaling completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void SaveImageWithQuality(Bitmap image, string path, long jpegQuality = 90L) {
        var format = GetImageFormat(Path.GetExtension(path));
        
        if (format == ImageFormat.Jpeg) {
            // Find JPEG encoder without LINQ
            ImageCodecInfo jpegEncoder = null;
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs) {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    jpegEncoder = codec;
                    break;
                }
            }
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);
            image.Save(path, jpegEncoder, encoderParams);
            Console.WriteLine($"jpeg quality={jpegQuality}");
        } else {
            image.Save(path, format);
        }
    }

    static ImageFormat GetImageFormat(string extension)
    {
        return extension.ToLower() switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            ".tif" or ".tiff" => ImageFormat.Tiff,
            _ => ImageFormat.Png // Default
        };
    }

}