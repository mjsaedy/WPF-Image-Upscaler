using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class Program {
    [STAThread]
    static void Main(string[] args) {
        if (args.Length < 3) {
            Console.WriteLine("Usage: WpfConsoleUpscaler.exe <inputFile> <outputFile> <scale> [/over] [/sat:VALUE] [/sharp:VALUE]");
            Console.WriteLine("Example: WpfConsoleUpscaler.exe in.png out.png 2.0 /over /sat:120 /sharp:2");
            return;
        }

        string inputFile = args[0];
        string outputFile = args[1];
        if (!double.TryParse(args[2], out double scale) || scale <= 0) {
            Console.WriteLine("Invalid scale value.");
            return;
        }

        bool overwrite = args.Any(a => a.Equals("/over", StringComparison.OrdinalIgnoreCase));
        int saturation = 100; // default normal
        double sharpness = 0; // default none

        foreach (var arg in args.Skip(3)) {
            if (arg.StartsWith("/sat:", StringComparison.OrdinalIgnoreCase)) {
                if (int.TryParse(arg.Substring(5), out int satVal)) saturation = satVal;
            }
            else if (arg.StartsWith("/sharp:", StringComparison.OrdinalIgnoreCase)) {
                if (double.TryParse(arg.Substring(7), out double shVal)) sharpness = shVal;
            }
        }

        if (!File.Exists(inputFile)) {
            Console.WriteLine("Input file not found.");
            return;
        }
        if (File.Exists(outputFile) && !overwrite) {
            Console.WriteLine($"Output file '{outputFile}' already exists. Use /over to overwrite.");
            return;
        }

        try {
            UpscaleFile(inputFile, outputFile, scale, saturation, sharpness);
            Console.WriteLine("Done.");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void UpscaleFile(string inputPath, string outputPath, double scale, int saturation, double sharpness) {
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.UriSource = new Uri(Path.GetFullPath(inputPath));
        bi.EndInit();
        bi.Freeze();

        int targetW = (int)Math.Round(bi.PixelWidth * scale);
        int targetH = (int)Math.Round(bi.PixelHeight * scale);

        Console.WriteLine($"Upscaling {Path.GetFileName(inputPath)}: {bi.PixelWidth}x{bi.PixelHeight} -> {targetW}x{targetH}");

        BitmapSource result = RenderScaleWithWpf(bi, targetW, targetH, bi.DpiX, bi.DpiY);

        if (saturation != 100)
            result = AdjustSaturation(result, saturation);

        if (sharpness > 0)
            result = ApplySharpen(result, sharpness);

        string ext = Path.GetExtension(outputPath).ToLowerInvariant();
        BitmapEncoder encoder;
        if (ext == ".jpg" || ext == ".jpeg")
            encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        else if (ext == ".bmp")
            encoder = new BmpBitmapEncoder();
        else {
            ext = ".png";
            outputPath = Path.ChangeExtension(outputPath, ".png");
            encoder = new PngBitmapEncoder();
        }

        encoder.Frames.Add(BitmapFrame.Create(result));
        using (var fs = new FileStream(outputPath, FileMode.Create))
            encoder.Save(fs);
    }

    static BitmapSource RenderScaleWithWpf(BitmapSource src, int targetW, int targetH, double dpiX, double dpiY) {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) {
            var brush = new ImageBrush(src) { Stretch = Stretch.Uniform };
            dc.DrawRectangle(brush, null, new Rect(0, 0, targetW, targetH));
        }
        RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.HighQuality);

        var rtb = new RenderTargetBitmap(targetW, targetH, dpiX, dpiY, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }

    static BitmapSource AdjustSaturation(BitmapSource src, int saturation) {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        double satFactor = saturation / 100.0;
        for (int i = 0; i < pixels.Length; i += 4) {
            double b = pixels[i] / 255.0;
            double g = pixels[i + 1] / 255.0;
            double r = pixels[i + 2] / 255.0;

            double gray = 0.3 * r + 0.59 * g + 0.11 * b;
            r = gray + (r - gray) * satFactor;
            g = gray + (g - gray) * satFactor;
            b = gray + (b - gray) * satFactor;

            pixels[i] = (byte)Math.Min(255, Math.Max(0, b * 255));
            pixels[i + 1] = (byte)Math.Min(255, Math.Max(0, g * 255));
            pixels[i + 2] = (byte)Math.Min(255, Math.Max(0, r * 255));
        }

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }

    static BitmapSource ApplySharpen(BitmapSource src, double strength) {
        // Simple sharpening kernel (unsharp mask style)
        int size = 3;
        double[,] kernel = {
            { 0, -1, 0 },
            { -1, 5, -1 },
            { 0, -1, 0 }
        };

        // Scale kernel strength
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                kernel[y, x] *= strength;

        return Convolve(src, kernel);
    }

    static BitmapSource Convolve(BitmapSource src, double[,] kernel) {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        byte[] output = new byte[pixels.Length];
        src.CopyPixels(pixels, stride, 0);

        int w = src.PixelWidth;
        int h = src.PixelHeight;
        int kSize = kernel.GetLength(0);
        int kHalf = kSize / 2;

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                double r = 0, g = 0, b = 0;
                for (int ky = -kHalf; ky <= kHalf; ky++) {
                    for (int kx = -kHalf; kx <= kHalf; kx++) {
                        int px = Math.Min(w - 1, Math.Max(0, x + kx));
                        int py = Math.Min(h - 1, Math.Max(0, y + ky));
                        int idx = (py * stride) + px * 4;

                        double kval = kernel[ky + kHalf, kx + kHalf];
                        b += pixels[idx] * kval;
                        g += pixels[idx + 1] * kval;
                        r += pixels[idx + 2] * kval;
                    }
                }

                int outIdx = (y * stride) + x * 4;
                output[outIdx] = (byte)Math.Min(255, Math.Max(0, b));
                output[outIdx + 1] = (byte)Math.Min(255, Math.Max(0, g));
                output[outIdx + 2] = (byte)Math.Min(255, Math.Max(0, r));
                output[outIdx + 3] = pixels[outIdx + 3]; // alpha unchanged
            }
        }

        var wb = new WriteableBitmap(w, h, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, w, h), output, stride, 0);
        wb.Freeze();
        return wb;
    }
}
