using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

class Program
{
    [STAThread] // Required for WPF imaging
    static void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: WpfConsoleUpscaler.exe <input> <output> <scale>");
            return;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        if (!double.TryParse(args[2], out double scale) || scale <= 0)
        {
            Console.WriteLine("Invalid scale value.");
            return;
        }

        if (!File.Exists(inputPath))
        {
            Console.WriteLine("Input file not found.");
            return;
        }

        try
        {
            // Load image into memory
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.UriSource = new Uri(Path.GetFullPath(inputPath));
            bi.EndInit();
            bi.Freeze();

            int targetW = (int)Math.Round(bi.PixelWidth * scale);
            int targetH = (int)Math.Round(bi.PixelHeight * scale);

            Console.WriteLine($"Upscaling {bi.PixelWidth}x{bi.PixelHeight} ? {targetW}x{targetH} ...");

            var result = RenderScaleWithWpf(bi, targetW, targetH);

            // Choose encoder based on extension
            string ext = Path.GetExtension(outputPath).ToLowerInvariant();
            BitmapEncoder encoder;
            if (ext == ".jpg" || ext == ".jpeg")
                encoder = new JpegBitmapEncoder { QualityLevel = 95 };
            else if (ext == ".bmp")
                encoder = new BmpBitmapEncoder();
            else
                encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(result));
            using (var fs = new FileStream(outputPath, FileMode.Create))
                encoder.Save(fs);

            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    private static BitmapSource RenderScaleWithWpf(BitmapSource src, int targetW, int targetH)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var brush = new ImageBrush(src) { Stretch = Stretch.Fill };
            dc.DrawRectangle(brush, null, new Rect(0, 0, targetW, targetH));
        }

        RenderOptions.SetBitmapScalingMode(dv, BitmapScalingMode.Fant);

        var rtb = new RenderTargetBitmap(targetW, targetH, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        rtb.Freeze();
        return rtb;
    }
}
