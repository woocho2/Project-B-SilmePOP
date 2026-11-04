using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class ExportGentleSliderFill
{
    public static void Write(string sourcePath, string destinationPath)
    {
        using (Bitmap source = new Bitmap(sourcePath))
        using (Bitmap result = new Bitmap(1280, 256, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(result))
        using (ImageAttributes attributes = new ImageAttributes())
        {
            // Locate the continuous bar, excluding the generated outer padding
            // and isolated specks. These bounds are used only for export cropping.
            int top = -1, bottom = -1;
            for (int y = 0; y < source.Height; y++)
            {
                int count = 0;
                for (int x = 0; x < source.Width; x++)
                    if (source.GetPixel(x, y).A >= 128) count++;
                if (count < source.Width * 0.3) continue;
                if (top < 0) top = y;
                bottom = y;
            }
            if (top < 0) throw new InvalidOperationException("No slider bar to export.");
            int height = bottom - top + 1;
            int cap = Math.Min(height / 2, source.Width / 3);
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            // Keep round end caps at the same horizontal/vertical scale.
            // Only the straight center changes length to reach the 5:1 target.
            g.DrawImage(source, new Rectangle(0, 0, 128, 256),
                0, top, cap, height, GraphicsUnit.Pixel, attributes);
            g.DrawImage(source, new Rectangle(128, 0, 1024, 256),
                cap, top, source.Width - cap * 2, height, GraphicsUnit.Pixel, attributes);
            g.DrawImage(source, new Rectangle(1152, 0, 128, 256),
                source.Width - cap, top, cap, height, GraphicsUnit.Pixel, attributes);
            result.Save(destinationPath, ImageFormat.Png);
        }
    }
}
