using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class ImageUpscaler
{
    public static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: ImageUpscaler <inputPath> <outputPath> <scaleFactor>");
            Console.WriteLine("Example: ImageUpscaler input.jpg output.png 2");
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

                using (Bitmap upscaledImage = UpscaleImage(originalImage, newWidth, newHeight))
                {
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

    private static Bitmap UpscaleImage(Bitmap original, int newWidth, int newHeight)
    {
        Bitmap upscaled = new Bitmap(newWidth, newHeight, original.PixelFormat);

        // Lock bits for faster processing
        BitmapData originalData = original.LockBits(
            new Rectangle(0, 0, original.Width, original.Height),
            ImageLockMode.ReadOnly,
            original.PixelFormat);

        BitmapData upscaledData = upscaled.LockBits(
            new Rectangle(0, 0, newWidth, newHeight),
            ImageLockMode.WriteOnly,
            upscaled.PixelFormat);

        int originalBytesPerPixel = Image.GetPixelFormatSize(original.PixelFormat) / 8;
        int upscaledBytesPerPixel = Image.GetPixelFormatSize(upscaled.PixelFormat) / 8;

        byte[] originalPixels = new byte[originalData.Stride * original.Height];
        byte[] upscaledPixels = new byte[upscaledData.Stride * newHeight];

        Marshal.Copy(originalData.Scan0, originalPixels, 0, originalPixels.Length);

        float xRatio = (float)(original.Width - 1) / newWidth;
        float yRatio = (float)(original.Height - 1) / newHeight;

        for (int y = 0; y < newHeight; y++)
        {
            float originalY = y * yRatio;
            int yFloor = (int)originalY;
            float yFraction = originalY - yFloor;
            int yCeiling = yFloor + 1;
            if (yCeiling >= original.Height) yCeiling = yFloor;

            for (int x = 0; x < newWidth; x++)
            {
                float originalX = x * xRatio;
                int xFloor = (int)originalX;
                float xFraction = originalX - xFloor;
                int xCeiling = xFloor + 1;
                if (xCeiling >= original.Width) xCeiling = xFloor;

                // Get the four neighboring pixels
                int indexTL = (yFloor * originalData.Stride) + (xFloor * originalBytesPerPixel);
                int indexTR = (yFloor * originalData.Stride) + (xCeiling * originalBytesPerPixel);
                int indexBL = (yCeiling * originalData.Stride) + (xFloor * originalBytesPerPixel);
                int indexBR = (yCeiling * originalData.Stride) + (xCeiling * originalBytesPerPixel);

                // Bilinear interpolation for each color channel
                for (int i = 0; i < originalBytesPerPixel; i++)
                {
                    byte top = (byte)(originalPixels[indexTL + i] * (1 - xFraction) + originalPixels[indexTR + i] * xFraction);
                    byte bottom = (byte)(originalPixels[indexBL + i] * (1 - xFraction) + originalPixels[indexBR + i] * xFraction);
                    byte interpolated = (byte)(top * (1 - yFraction) + bottom * yFraction);

                    int upscaledIndex = (y * upscaledData.Stride) + (x * upscaledBytesPerPixel) + i;
                    upscaledPixels[upscaledIndex] = interpolated;
                }
            }
        }

        Marshal.Copy(upscaledPixels, 0, upscaledData.Scan0, upscaledPixels.Length);

        original.UnlockBits(originalData);
        upscaled.UnlockBits(upscaledData);

        return upscaled;
    }

    private static void SaveImageWithQuality(Bitmap image, string outputPath)
    {
        string extension = System.IO.Path.GetExtension(outputPath).ToLower();

        ImageFormat format = ImageFormat.Png; // Default to PNG for lossless
        EncoderParameters encoderParams = new EncoderParameters(1);

        if (extension == ".jpg" || extension == ".jpeg")
        {
            format = ImageFormat.Jpeg;
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L); // Highest quality
        }
        else if (extension == ".png")
        {
            format = ImageFormat.Png;
        }
        else if (extension == ".bmp")
        {
            format = ImageFormat.Bmp;
        }
        else if (extension == ".tiff")
        {
            format = ImageFormat.Tiff;
        }

        // Get the encoder info
        ImageCodecInfo codecInfo = GetEncoderInfo(format);

        if (codecInfo != null)
        {
            image.Save(outputPath, codecInfo, encoderParams);
        }
        else
        {
            // Fallback if we can't find the encoder
            image.Save(outputPath, format);
        }
    }

    private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }
}