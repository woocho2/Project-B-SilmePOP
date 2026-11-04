using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class ExportAngelReferenceBody
{
    // Export preparation: remove the generated outer transparent padding and
    // map the resulting body to the user's requested edge-filling 1024px square.
    public static void Write(string sourcePath, string destinationPath)
    {
        using (Bitmap source = new Bitmap(sourcePath))
        {
            int left = source.Width, top = source.Height, right = -1, bottom = -1;
            for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
            {
                if (source.GetPixel(x, y).A == 0) continue;
                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
            if (right < left) throw new InvalidOperationException("Empty source artwork.");
            Rectangle crop = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
            using (Bitmap output = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(output))
            using (ImageAttributes attributes = new ImageAttributes())
            {
                g.Clear(Color.Transparent);
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source, new Rectangle(0, 0, 1024, 1024),
                    crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attributes);
                output.Save(destinationPath, ImageFormat.Png);
            }
        }
    }
}
