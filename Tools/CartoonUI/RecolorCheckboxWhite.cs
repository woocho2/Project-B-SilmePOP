using System;
using System.Drawing;
using System.Drawing.Imaging;

public static class RecolorCheckboxWhite
{
    public static void Write(string sourcePath, string destinationPath)
    {
        using (Bitmap source = new Bitmap(sourcePath))
        using (Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                // Only replace the warm cream/peach fill. Preserve the original
                // dark-indigo frame, lavender rim, highlights and every alpha.
                if (pixel.R >= 180 && pixel.G >= 100 &&
                    pixel.R >= pixel.G && pixel.G >= pixel.B && pixel.R - pixel.B > 12)
                {
                    int neutral = Math.Max(236, Math.Min(255,
                        236 + (pixel.G - 180) * 19 / 45));
                    pixel = Color.FromArgb(pixel.A, neutral, neutral, neutral);
                }
                result.SetPixel(x, y, pixel);
            }
            result.Save(destinationPath, ImageFormat.Png);
        }
    }
}
