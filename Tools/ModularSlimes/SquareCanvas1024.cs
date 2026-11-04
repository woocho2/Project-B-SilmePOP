using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class ModularSquareCanvas
{
    public static void Export(string sourcePath, string destinationPath)
    {
        const int size = 1024;
        using (Bitmap source = new Bitmap(sourcePath))
        using (Bitmap output = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(output))
        using (ImageAttributes attributes = new ImageAttributes())
        {
            double scale = Math.Min(size / (double)source.Width, size / (double)source.Height);
            int width = Math.Min(size, (int)Math.Round(source.Width * scale));
            int height = Math.Min(size, (int)Math.Round(source.Height * scale));
            Rectangle target = new Rectangle((size - width) / 2, (size - height) / 2, width, height);

            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(source, target, 0, 0, source.Width, source.Height,
                GraphicsUnit.Pixel, attributes);
            output.Save(destinationPath, ImageFormat.Png);
        }
    }
}
