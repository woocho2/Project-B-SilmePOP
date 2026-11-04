using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class TitleAtlasLayout
{
    // Asset packing only: copy existing glyph pixels into eight centered square cells.
    public static void Pack(string sourcePath, string destinationPath)
    {
        using (Bitmap source = new Bitmap(sourcePath))
        {
            int width = source.Width;
            int height = source.Height;
            BitmapData data = source.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int stride = data.Stride;
            byte[] pixels = new byte[stride * height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            source.UnlockBits(data);

            bool[] visited = new bool[width * height];
            Rectangle[] glyphBounds = new Rectangle[8];
            Queue<int> queue = new Queue<int>();
            for (int start = 0; start < visited.Length; start++)
            {
                int sx = start % width, sy = start / width;
                if (visited[start] || pixels[sy * stride + sx * 4 + 3] <= 32) continue;

                int minX = sx, maxX = sx, minY = sy, maxY = sy, count = 0;
                double sumX = 0, sumY = 0;
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue(), x = p % width, y = p / width;
                    count++;
                    sumX += x; sumY += y;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);

                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            int next = ny * width + nx;
                            if (visited[next] || pixels[ny * stride + nx * 4 + 3] <= 32) continue;
                            visited[next] = true;
                            queue.Enqueue(next);
                        }
                }

                // Exclude detached single-pixel alpha debris from positioning.
                if (count < 128) continue;
                int column = Math.Min(3, (int)((sumX / count) * 4 / width));
                int row = Math.Min(1, (int)((sumY / count) * 2 / height));
                int index = row * 4 + column;
                Rectangle component = Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
                glyphBounds[index] = glyphBounds[index].IsEmpty
                    ? component : Rectangle.Union(glyphBounds[index], component);
            }

            const int cell = 512;
            using (Bitmap atlas = new Bitmap(cell * 4, cell * 2, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(atlas))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                for (int i = 0; i < glyphBounds.Length; i++)
                {
                    if (glyphBounds[i].IsEmpty)
                        throw new InvalidOperationException("No glyph artwork in tile " + i);

                    Rectangle bounds = glyphBounds[i];
                    bounds.Inflate(2, 2);
                    bounds.Intersect(new Rectangle(0, 0, width, height));
                    if (bounds.Width > cell || bounds.Height > cell)
                        throw new InvalidOperationException("Glyph artwork exceeds its target tile: " + i);

                    int x = (i % 4) * cell + (cell - bounds.Width) / 2;
                    int y = (i / 4) * cell + (cell - bounds.Height) / 2;
                    using (Bitmap glyph = source.Clone(bounds, PixelFormat.Format32bppArgb))
                        graphics.DrawImageUnscaled(glyph, x, y);
                }
                atlas.Save(destinationPath, ImageFormat.Png);
            }
        }
    }
}
