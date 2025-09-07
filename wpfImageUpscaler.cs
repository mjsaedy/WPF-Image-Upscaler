using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks; //Parallel.For()
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class Program {
    [STAThread]
    static void Main(string[] args) {
        if (args.Length < 2 || args.Any(a => a.Equals("/?", StringComparison.OrdinalIgnoreCase) || a.Equals("/help", StringComparison.OrdinalIgnoreCase))) {
            ShowHelp();
            return;
        }

        string inputFile = args[0];
        string outputFile = args[1];

        bool overwrite = false;
        double scale = 2.0;   // default x2
        int saturation = 100; // default normal
        string mirror = null; // "h" or "v"
        int brightness = 0;   // default none
        int contrast = 0;     // default none
        int jpegQuality = 85; // default quality
        double sharpness = 0; // default none

        foreach (var arg in args.Skip(2)) {
            if (arg.Equals("/over", StringComparison.OrdinalIgnoreCase)) {
                overwrite = true;
            }
            else if (arg.StartsWith("/scale:", StringComparison.OrdinalIgnoreCase)) {
                if (double.TryParse(arg.Substring(7), out double sc) && sc > 0) scale = sc;
            }
            else if (arg.StartsWith("/sat:", StringComparison.OrdinalIgnoreCase)) {
                if (int.TryParse(arg.Substring(5), out int satVal)) saturation = satVal;
            }
            else if (arg.StartsWith("/sharp:", StringComparison.OrdinalIgnoreCase)) {
                if (double.TryParse(arg.Substring(7), out double shVal)) sharpness = shVal;
            }
            else if (arg.StartsWith("/con:", StringComparison.OrdinalIgnoreCase)) {
                if (int.TryParse(arg.Substring(5), out int conVal)) contrast = conVal;
            }
            else if (arg.StartsWith("/bright:", StringComparison.OrdinalIgnoreCase)) {
                if (int.TryParse(arg.Substring(8), out int brightVal)) brightness = brightVal;
            }
            else if (arg.StartsWith("/mirror", StringComparison.OrdinalIgnoreCase)) {
                if (arg.Contains(":")) {
                    var val = arg.Split(':')[1].ToLower();
                    if (val == "v")
                        mirror = "v";
                    else
                        mirror = "h";
                }
                else {
                    mirror = "h"; // default
                }
            }
            else if (arg.StartsWith("/quality:", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("/q:", StringComparison.OrdinalIgnoreCase)) {
                var val = arg.Contains(":") ? arg.Split(':')[1] : null;
                if (int.TryParse(val, out int qVal) && qVal >= 1 && qVal <= 100)
                    jpegQuality = qVal;
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
            Console.WriteLine();
            Console.WriteLine($" Source file: \"{inputFile}\"");
            Console.WriteLine($" Target file: \"{outputFile}\"");
            Console.WriteLine($" Scale [x2]:  x{scale}");
            Console.WriteLine($" Sharpness [0]:     {sharpness}");
            Console.WriteLine($" Saturation [100]:  {saturation}");
            Console.WriteLine($" Brightness [0]:    {brightness}");
            Console.WriteLine($" Contrast [0]:      {contrast}");
            Console.WriteLine($" Mirror [none]:     {mirror}");
            Console.WriteLine($" JPEG Quality [85]: {jpegQuality}");
            Console.WriteLine($" Overwrite mode [false]: {overwrite}");
            // only first two arguments are required
            UpscaleFile(inputFile,
                        outputFile,
                        scale: scale,
                        sharpness: sharpness,
                        saturation: saturation,
                        brightness: brightness,
                        contrast: contrast,
                        mirror: mirror,
                        jpegQuality: jpegQuality);
            Console.WriteLine("Done.");
        }
        catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ShowHelp() {
        Console.WriteLine("wimgx.exe <inputFile> <outputFile> [operations] [options]");
        Console.WriteLine();
        Console.WriteLine("Operations:");
        Console.WriteLine("  /bright:N            Brightness adjustment (0 = unchanged, -100 = fully dark, +100 = fully bright)");
        Console.WriteLine("  /con:N               Contrast adjustment (0 = unchanged, -100 = flat gray image, +100 = very high contrast)");
        Console.WriteLine("  /mirror[:h|v]        Mirror the image (default = horizontal if no h/v given)");
        Console.WriteLine("  /sat:N               Saturation adjustment (0 = grayscale, 100 = normal, >100 = oversaturated)");
        Console.WriteLine("  /scale:N             Scaling factor (must be > 0, default = 2.0)");
        Console.WriteLine("  /sharp:N             Sharpening strength (0 = off, suggested range 1–3)");
        Console.WriteLine("  /quality:N or /q:N   JPEG quality (1–100, default = 85)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  /over                Allow overwriting existing files");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  wimgx.exe in.png out.png /scale:3 /sat:120 /sharp:2 /mirror /over");
        Console.WriteLine("  wimgx.exe in.jpg out.jpg /q:85 /sat:0   (grayscale, JPEG quality 85)");
    }

    static void UpscaleFile(string inputPath,
                            string outputPath,
                            double scale = 2,
                            double sharpness = 0,
                            int saturation = 100,
                            int brightness = 0,
                            int contrast = 0,
                            string mirror = "h",
                            int jpegQuality = 85)
    {
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.UriSource = new Uri(Path.GetFullPath(inputPath));
        bi.EndInit();
        bi.Freeze();

        int targetW = (int)Math.Round(bi.PixelWidth * scale);
        int targetH = (int)Math.Round(bi.PixelHeight * scale);

        Console.WriteLine($"\nUpscaling {bi.PixelWidth}x{bi.PixelHeight} --> {targetW}x{targetH}");

        BitmapSource result = RenderScaleWithWpf(bi, targetW, targetH, bi.DpiX, bi.DpiY);

        if (mirror != null)
            result = ApplyMirror(result, mirror);

        /*
        if (saturation != 100)
            //result = AdjustSaturation(result, saturation);
            //result = AdjustSaturationParallel(result, saturation);
            //result = AdjustSaturationUnsafe(result, saturation);
            result = AdjustSaturationParallelUnsafe(result, saturation);
        */

        if (saturation != 100 || brightness != 0 || contrast != 0)
            result = AdjustSaturationBrightnessContrast(result, saturation, brightness, contrast);

        if (sharpness > 0)
            result = ApplySharpen(result, sharpness);

        string ext = Path.GetExtension(outputPath).ToLowerInvariant();
        BitmapEncoder encoder;
        if (ext == ".jpg" || ext == ".jpeg")
            encoder = new JpegBitmapEncoder { QualityLevel = jpegQuality };
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

    static BitmapSource ApplyMirror(BitmapSource src, string mode) {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) {
            var scaleX = (mode == "h") ? -1 : 1;
            var scaleY = (mode == "v") ? -1 : 1;
            var transform = new ScaleTransform(scaleX, scaleY, src.PixelWidth / 2.0, src.PixelHeight / 2.0);
            dc.PushTransform(transform);
            dc.DrawImage(src, new Rect(0, 0, src.PixelWidth, src.PixelHeight));
            dc.Pop();
        }
        var rtb = new RenderTargetBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }


    //original code,  works ok
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

    // Parallel execution for speed
    static BitmapSource AdjustSaturationParallel(BitmapSource src, int saturation)
    {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        // Clamp saturation to >= 0
        double satFactor = Math.Max(0, saturation) / 100.0;

        // Rec.709 luma coefficients
        const double cr = 0.2126;
        const double cg = 0.7152;
        const double cb = 0.0722;

        Parallel.For(0, src.PixelHeight, y =>
        {
            int offset = y * stride;
            for (int x = 0; x < src.PixelWidth; x++, offset += 4)
            {
                double b = pixels[offset] / 255.0;
                double g = pixels[offset + 1] / 255.0;
                double r = pixels[offset + 2] / 255.0;
                byte a = pixels[offset + 3];

                if (a > 0)
                {
                    double alpha = a / 255.0;

                    // Unpremultiply
                    r /= alpha;
                    g /= alpha;
                    b /= alpha;

                    // Convert to grayscale using Rec.709
                    double gray = cr * r + cg * g + cb * b;

                    // Apply saturation
                    r = gray + (r - gray) * satFactor;
                    g = gray + (g - gray) * satFactor;
                    b = gray + (b - gray) * satFactor;

                    // Repremultiply
                    r *= alpha;
                    g *= alpha;
                    b *= alpha;
                }

                // Clamp and store back
                pixels[offset]     = (byte)Math.Min(255, Math.Max(0, b * 255));
                pixels[offset + 1] = (byte)Math.Min(255, Math.Max(0, g * 255));
                pixels[offset + 2] = (byte)Math.Min(255, Math.Max(0, r * 255));
                // alpha unchanged
            }
        });

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }

    // Unsafe code for more speed
    static BitmapSource AdjustSaturationUnsafe(BitmapSource src, int saturation)
    {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        double satFactor = Math.Max(0, saturation) / 100.0;

        // Rec.709 luma coefficients
        const double cr = 0.2126;
        const double cg = 0.7152;
        const double cb = 0.0722;

        unsafe
        {
            fixed (byte* basePtr = pixels)
            {
                byte* row = basePtr;

                for (int y = 0; y < src.PixelHeight; y++)
                {
                    byte* pixel = row;

                    for (int x = 0; x < src.PixelWidth; x++, pixel += 4)
                    {
                        double b = pixel[0] / 255.0;
                        double g = pixel[1] / 255.0;
                        double r = pixel[2] / 255.0;
                        byte a = pixel[3];

                        if (a > 0)
                        {
                            double alpha = a / 255.0;

                            // Unpremultiply
                            r /= alpha;
                            g /= alpha;
                            b /= alpha;

                            // Convert to grayscale (Rec.709)
                            double gray = cr * r + cg * g + cb * b;

                            // Apply saturation
                            r = gray + (r - gray) * satFactor;
                            g = gray + (g - gray) * satFactor;
                            b = gray + (b - gray) * satFactor;

                            // Premultiply
                            r *= alpha;
                            g *= alpha;
                            b *= alpha;
                        }

                        // Clamp and store back
                        pixel[0] = (byte)Math.Min(255, Math.Max(0, b * 255));
                        pixel[1] = (byte)Math.Min(255, Math.Max(0, g * 255));
                        pixel[2] = (byte)Math.Min(255, Math.Max(0, r * 255));
                        // alpha unchanged
                    }

                    row += stride;
                }
            }
        }

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }


    // Unsafe code + Parallel.For() for max speed
    static BitmapSource AdjustSaturationParallelUnsafe(BitmapSource src, int saturation)
    {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        double satFactor = Math.Max(0, saturation) / 100.0;

        // Rec.709 luma coefficients
        const double cr = 0.2126;
        const double cg = 0.7152;
        const double cb = 0.0722;

        unsafe
        {
            fixed (byte* basePtr = pixels)
            {
                byte* p = basePtr; // make a local copy so lambda doesn't capture 'fixed'

                Parallel.For(0, src.PixelHeight, y =>
                {
                    byte* row = p + y * stride;

                    for (int x = 0; x < src.PixelWidth; x++, row += 4)
                    {
                        double b = row[0] / 255.0;
                        double g = row[1] / 255.0;
                        double r = row[2] / 255.0;
                        byte a = row[3];

                        if (a > 0)
                        {
                            double alpha = a / 255.0;

                            // Unpremultiply
                            r /= alpha;
                            g /= alpha;
                            b /= alpha;

                            // Grayscale (Rec.709)
                            double gray = cr * r + cg * g + cb * b;

                            // Apply saturation
                            r = gray + (r - gray) * satFactor;
                            g = gray + (g - gray) * satFactor;
                            b = gray + (b - gray) * satFactor;

                            // Repremultiply
                            r *= alpha;
                            g *= alpha;
                            b *= alpha;
                        }

                        // Clamp + store
                        row[0] = (byte)Math.Min(255, Math.Max(0, b * 255));
                        row[1] = (byte)Math.Min(255, Math.Max(0, g * 255));
                        row[2] = (byte)Math.Min(255, Math.Max(0, r * 255));
                        // alpha unchanged
                    }
                });
            }
        }

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }

    // New: Brightness and Contrast
    static BitmapSource AdjustBrightnessContrast(BitmapSource src, int brightness, int contrast)
    {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        // Brightness: -100..100 → factor of -1.0..+1.0
        double brightFactor = brightness / 100.0;

        // Contrast: -100..100 → factor of 0..2.0
        double contrastFactor = (100.0 + contrast) / 100.0;
        contrastFactor *= contrastFactor; // better curve

        unsafe
        {
            fixed (byte* basePtr = pixels)
            {
                byte* row = basePtr;

                for (int y = 0; y < src.PixelHeight; y++)
                {
                    byte* pixel = row;

                    for (int x = 0; x < src.PixelWidth; x++, pixel += 4)
                    {
                        double b = pixel[0] / 255.0;
                        double g = pixel[1] / 255.0;
                        double r = pixel[2] / 255.0;
                        byte a = pixel[3];

                        if (a > 0)
                        {
                            double alpha = a / 255.0;

                            // Unpremultiply
                            r /= alpha;
                            g /= alpha;
                            b /= alpha;

                            // Apply brightness and contrast
                            r = (((r - 0.5) * contrastFactor) + 0.5) + brightFactor;
                            g = (((g - 0.5) * contrastFactor) + 0.5) + brightFactor;
                            b = (((b - 0.5) * contrastFactor) + 0.5) + brightFactor;

                            // Premultiply
                            r *= alpha;
                            g *= alpha;
                            b *= alpha;
                        }

                        // Clamp
                        pixel[0] = (byte)Math.Min(255, Math.Max(0, b * 255));
                        pixel[1] = (byte)Math.Min(255, Math.Max(0, g * 255));
                        pixel[2] = (byte)Math.Min(255, Math.Max(0, r * 255));
                        // alpha unchanged
                    }

                    row += stride;
                }
            }
        }

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }

    /* Combined Saturation, Brightness, and Contrast to avoid looping more than once
       Saturation: 0 → grayscale, 100 → unchanged, 200 → double saturation
       Brightness: 0 → unchanged, -100 → fully dark, +100 → fully bright
       Contrast:   0 → unchanged, -100 → flat gray, +100 → strong contrast
    */
    static BitmapSource AdjustSaturationBrightnessContrast(BitmapSource src,
                                                           int saturation = 100,
                                                           int brightness = 0,
                                                           int contrast = 0)
    {
        var format = PixelFormats.Pbgra32;
        int stride = src.PixelWidth * (format.BitsPerPixel / 8);
        byte[] pixels = new byte[stride * src.PixelHeight];
        src.CopyPixels(pixels, stride, 0);

        // Parameters
        double satFactor = Math.Max(0, saturation) / 100.0; // 0 = grayscale, 100 = normal, >100 = boost
        double brightFactor = brightness / 100.0;           // -100..100 → -1..+1
        double contrastFactor = (100.0 + contrast) / 100.0; // -100..100 → 0..2
        contrastFactor *= contrastFactor;                   // nonlinear scaling for better results

        // Rec.709 luma coefficients
        const double cr = 0.2126;
        const double cg = 0.7152;
        const double cb = 0.0722;

        unsafe
        {
            fixed (byte* basePtr = pixels)
            {
                byte* row = basePtr;

                for (int y = 0; y < src.PixelHeight; y++)
                {
                    byte* pixel = row;

                    for (int x = 0; x < src.PixelWidth; x++, pixel += 4)
                    {
                        double b = pixel[0] / 255.0;
                        double g = pixel[1] / 255.0;
                        double r = pixel[2] / 255.0;
                        byte a = pixel[3];

                        if (a > 0)
                        {
                            double alpha = a / 255.0;

                            // Unpremultiply
                            r /= alpha;
                            g /= alpha;
                            b /= alpha;

                            // --- Saturation ---
                            double gray = cr * r + cg * g + cb * b;
                            r = gray + (r - gray) * satFactor;
                            g = gray + (g - gray) * satFactor;
                            b = gray + (b - gray) * satFactor;

                            // --- Contrast & Brightness ---
                            r = (((r - 0.5) * contrastFactor) + 0.5) + brightFactor;
                            g = (((g - 0.5) * contrastFactor) + 0.5) + brightFactor;
                            b = (((b - 0.5) * contrastFactor) + 0.5) + brightFactor;

                            // Premultiply
                            r *= alpha;
                            g *= alpha;
                            b *= alpha;
                        }

                        // Clamp and store
                        pixel[0] = (byte)Math.Min(255, Math.Max(0, b * 255));
                        pixel[1] = (byte)Math.Min(255, Math.Max(0, g * 255));
                        pixel[2] = (byte)Math.Min(255, Math.Max(0, r * 255));
                        // alpha unchanged
                    }

                    row += stride;
                }
            }
        }

        var wb = new WriteableBitmap(src.PixelWidth, src.PixelHeight, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, src.PixelWidth, src.PixelHeight), pixels, stride, 0);
        wb.Freeze();
        return wb;
    }



    static BitmapSource ApplySharpen(BitmapSource src, double strength) {
        int size = 3;
        double[,] kernel = {
            { 0, -1, 0 },
            { -1, 5, -1 },
            { 0, -1, 0 }
        };

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
                output[outIdx + 3] = pixels[outIdx + 3];
            }
        }

        var wb = new WriteableBitmap(w, h, src.DpiX, src.DpiY, format, null);
        wb.WritePixels(new Int32Rect(0, 0, w, h), output, stride, 0);
        wb.Freeze();
        return wb;
    }
}
