using System;
using System.IO;
using System.Linq;
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
            Console.WriteLine("Usage: WpfConsoleUpscaler.exe <inputPattern> <outputFolder> <scale>");
            Console.WriteLine("Example: WpfConsoleUpscaler.exe \"images\\*.png\" upscaled 2.0");
            return;
        }

        string inputPattern = args[0];
        string outputFolder = args[1];
        if (!double.TryParse(args[2], out double scale) || scale <= 0)
        {
            Console.WriteLine("Invalid scale value.");
            return;
        }

        string dir = Path.GetDirectoryName(inputPattern);
        if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
        string pattern = Path.GetFileName(inputPattern);

        if (!Directory.Exists(dir))
        {
            Console.WriteLine("Input folder not found.");
            return;
        }
        Directory.CreateDirectory(outputFolder);

        var files = Directory.GetFiles(dir, pattern);
        if (files.Length == 0)
        {
            Console.WriteLine("No matching files found.");
            return;
        }

        Console.WriteLine($"Found {files.Length} file(s). Output folder: {Path.GetFullPath(outputFolder)}");

        foreach (var file in files)
        {
            try
            {
                string outName = Path.Combine(outputFolder, Path.GetFileName(file));
                UpscaleFile(file, outName, scale);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }

        Console.WriteLine("All done.");
    }

    static void UpscaleFile(string inputPath, string outputPath, double scale)
    {
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.UriSource = new Uri(Path.GetFullPath(inputPath));
        bi.EndInit();
        bi.Freeze();

        int targetW = (int)Math.Round(bi.PixelWidth * scale);
        int targetH = (int)Math.Round(bi.PixelHeight * scale);

        Console.WriteLine($"Upscaling {Path.GetFileName(inputPath)}: {bi.PixelWidth}x{bi.PixelHeight} ? {targetW}x{targetH}");

        var result = RenderScaleWithWpf(bi, targetW, targetH);

        string ext = Path.GetExtension(outputPath).ToLowerInvariant();
        BitmapEncoder encoder;
        if (ext == ".jpg" || ext == ".jpeg")
            encoder = new JpegBitmapEncoder { QualityLevel = 95 };
        else if (ext == ".bmp")
            encoder = new BmpBitmapEncoder();
        else
        {
            ext = ".png";
            outputPath = Path.ChangeExtension(outputPath, ".png");
            encoder = new PngBitmapEncoder();
        }

        encoder.Frames.Add(BitmapFrame.Create(result));
        using (var fs = new FileStream(outputPath, FileMode.Create))
            encoder.Save(fs);
    }

    static BitmapSource RenderScaleWithWpf(BitmapSource src, int targetW, int targetH)
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
