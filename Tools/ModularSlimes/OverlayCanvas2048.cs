using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Offline sprite layout exporter. Never writes a body PNG, Unity asset, or meta.
public sealed class OverlayCanvas2048
{
    private const int CanvasSize = 2048;
    private readonly int bodyWidth;
    private readonly int bodyHeight;
    private readonly int bodyLeft;
    private readonly int bodyTop;
    private readonly float[] forehead;

    private sealed class Part
    {
        public readonly List<int> Pixels = new List<int>();
        public Rectangle Bounds;
    }

    public OverlayCanvas2048(string bodyPath)
    {
        using (Bitmap body = new Bitmap(bodyPath))
        {
            bodyWidth = body.Width;
            bodyHeight = body.Height;
            bodyLeft = (CanvasSize - bodyWidth) / 2;
            bodyTop = (CanvasSize - bodyHeight) / 2;
            int[] pixels = ReadPixels(body);
            float[] rawTop = new float[bodyWidth];
            for (int x = 0; x < bodyWidth; x++)
            {
                rawTop[x] = -1;
                for (int y = 0; y < bodyHeight; y++)
                {
                    uint pixel = (uint)pixels[y * bodyWidth + x];
                    int alpha = (int)(pixel >> 24);
                    int red = (int)((pixel >> 16) & 255);
                    int green = (int)((pixel >> 8) & 255);
                    int blue = (int)(pixel & 255);
                    // Trace the actual dark body contour, ignoring faint outer alpha.
                    if (alpha >= 160 && red < 110 && green < 110 && blue < 110)
                    {
                        rawTop[x] = y;
                        break;
                    }
                }
            }
            forehead = new float[bodyWidth];
            for (int x = 0; x < bodyWidth; x++)
            {
                float sum = 0;
                int count = 0;
                for (int sample = Math.Max(0, x - 5); sample <= Math.Min(bodyWidth - 1, x + 5); sample++)
                {
                    if (rawTop[sample] < 0) continue;
                    sum += rawTop[sample];
                    count++;
                }
                forehead[x] = count > 0 ? sum / count : bodyHeight / 2f;
            }
        }
    }

    private static int[] ReadPixels(Bitmap image)
    {
        int[] pixels = new int[image.Width * image.Height];
        BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < image.Height; y++)
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * image.Width, image.Width);
        }
        finally { image.UnlockBits(data); }
        return pixels;
    }

    private static Bitmap Extract(Bitmap source, Part part, int[] pixels)
    {
        Bitmap result = new Bitmap(part.Bounds.Width, part.Bounds.Height, PixelFormat.Format32bppArgb);
        int[] destination = new int[result.Width * result.Height];
        foreach (int index in part.Pixels)
        {
            int x = index % source.Width - part.Bounds.X;
            int y = index / source.Width - part.Bounds.Y;
            destination[y * result.Width + x] = pixels[index];
        }
        BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < result.Height; y++)
                Marshal.Copy(destination, y * result.Width, IntPtr.Add(data.Scan0, y * data.Stride), result.Width);
        }
        finally { result.UnlockBits(data); }
        return result;
    }

    private static List<Bitmap> SeparateParts(string path, int requiredParts)
    {
        using (Bitmap source = new Bitmap(path))
        {
            int width = source.Width, height = source.Height;
            int[] pixels = ReadPixels(source);
            bool[] visited = new bool[pixels.Length];
            int[] queue = new int[pixels.Length];
            List<Part> parts = new List<Part>();
            for (int seed = 0; seed < pixels.Length; seed++)
            {
                if (visited[seed] || (uint)pixels[seed] >> 24 < 16) continue;
                Part part = new Part();
                int left = width, right = 0, top = height, bottom = 0;
                int head = 0, tail = 0;
                queue[tail++] = seed;
                visited[seed] = true;
                while (head < tail)
                {
                    int index = queue[head++];
                    int x = index % width, y = index / width;
                    part.Pixels.Add(index);
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int next = ny * width + nx;
                        if (visited[next] || (uint)pixels[next] >> 24 < 16) continue;
                        visited[next] = true;
                        queue[tail++] = next;
                    }
                }
                part.Bounds = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
                if (part.Pixels.Count >= 64) parts.Add(part);
            }
            parts.Sort((a, b) => b.Pixels.Count.CompareTo(a.Pixels.Count));
            if (parts.Count < requiredParts)
                throw new InvalidOperationException("Missing separate accessory parts in " + path);
            parts = parts.GetRange(0, requiredParts);
            parts.Sort((a, b) => a.Bounds.X.CompareTo(b.Bounds.X));
            List<Bitmap> result = new List<Bitmap>();
            foreach (Part part in parts) result.Add(Extract(source, part, pixels));
            return result;
        }
    }

    private static Bitmap NewLayer()
    {
        return new Bitmap(CanvasSize, CanvasSize, PixelFormat.Format32bppArgb);
    }

    private static Graphics LayerGraphics(Bitmap image)
    {
        Graphics g = Graphics.FromImage(image);
        g.Clear(Color.Transparent);
        g.CompositingMode = CompositingMode.SourceOver;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        return g;
    }

    private static void DrawPart(Graphics g, Bitmap part, RectangleF destination)
    {
        using (ImageAttributes attributes = new ImageAttributes())
        {
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            PointF[] corners = { new PointF(destination.Left, destination.Top),
                new PointF(destination.Right, destination.Top), new PointF(destination.Left, destination.Bottom) };
            g.DrawImage(part, corners, new RectangleF(0, 0, part.Width, part.Height),
                GraphicsUnit.Pixel, attributes);
        }
    }

    private float HeadAt(float bodyX)
    {
        float x = Math.Max(0, Math.Min(bodyWidth - 1, bodyX));
        int x0 = (int)x, x1 = Math.Min(bodyWidth - 1, x0 + 1);
        return bodyTop + forehead[x0] * (1 - (x - x0)) + forehead[x1] * (x - x0);
    }

    public void WriteFace(string sourcePath, string destinationPath)
    {
        using (Bitmap source = new Bitmap(sourcePath))
        using (Bitmap result = NewLayer())
        using (Graphics g = LayerGraphics(result))
        {
            // Preserve the already aligned 1024px face layer pixel for pixel.
            g.DrawImageUnscaled(source, bodyLeft, bodyTop);
            result.Save(destinationPath, ImageFormat.Png);
        }
    }

    public void WriteHeadpiece(string sourcePath, string destinationPath,
        int targetWidth, float contactLeft, float contactRight, int overlap,
        bool smoothContact = false)
    {
        List<Bitmap> parts = SeparateParts(sourcePath, 1);
        using (Bitmap art = parts[0])
        using (Bitmap scaled = new Bitmap(targetWidth,
            (int)Math.Round(targetWidth * art.Height / (double)art.Width), PixelFormat.Format32bppArgb))
        using (Bitmap result = NewLayer())
        {
            using (Graphics g = LayerGraphics(scaled))
                DrawPart(g, art, new RectangleF(0, 0, scaled.Width, scaled.Height));
            int[] pixels = ReadPixels(scaled);
            int[] bottom = new int[scaled.Width];
            for (int x = 0; x < scaled.Width; x++)
            {
                bottom[x] = scaled.Height - 1;
                for (int y = scaled.Height - 1; y >= 0; y--)
                    if ((uint)pixels[y * scaled.Width + x] >> 24 >= 128)
                    { bottom[x] = y; break; }
            }
            int left = (CanvasSize - scaled.Width) / 2;
            int first = (int)Math.Round(contactLeft * (scaled.Width - 1));
            int last = (int)Math.Round(contactRight * (scaled.Width - 1));
            int center = scaled.Width / 2;
            int sampleOffset = (int)Math.Round(scaled.Width * 0.20);
            float centerBottom = bottom[center];
            float leftBottom = bottom[center - sampleOffset];
            float rightBottom = bottom[center + sampleOffset];
            float quadratic = (leftBottom + rightBottom) / 2f - centerBottom;
            float linear = (rightBottom - leftBottom) / 2f;
            using (Graphics g = LayerGraphics(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.None;
                for (int x = 0; x < scaled.Width; x++)
                {
                    // Seat the existing lower band directly on the traced head.
                    // Outside the band, continue the endpoint shift for the bells/tips.
                    int anchor = Math.Max(first, Math.Min(last, x));
                    // The jester's curled tips have a different bottom edge from
                    // the head opening. A smooth arch avoids jumping from the
                    // opening to a tip/bell and tearing the entire fabric column.
                    float contactBottom = bottom[anchor];
                    if (smoothContact)
                    {
                        float t = (anchor - center) / (float)sampleOffset;
                        contactBottom = centerBottom + quadratic * t * t + linear * t;
                    }
                    float top = HeadAt(left + anchor - bodyLeft) + overlap - contactBottom;
                    g.DrawImage(scaled, new Rectangle(left + x, (int)Math.Round(top), 1, scaled.Height),
                        new Rectangle(x, 0, 1, scaled.Height), GraphicsUnit.Pixel);
                }
            }
            result.Save(destinationPath, ImageFormat.Png);
        }
    }

    public void WriteAngel(string sourcePath, string destinationPath)
    {
        List<Bitmap> parts = SeparateParts(sourcePath, 3);
        try
        {
            Bitmap leftWing = parts[0], halo = parts[1], rightWing = parts[2];
            using (Bitmap result = NewLayer())
            using (Graphics g = LayerGraphics(result))
            {
                float haloWidth = 560;
                float haloHeight = haloWidth * halo.Height / halo.Width;
                float haloBottom = HeadAt(bodyWidth / 2f) + 98;
                DrawPart(g, halo, new RectangleF((CanvasSize - haloWidth) / 2,
                    haloBottom - haloHeight, haloWidth, haloHeight));

                float wingWidth = 330;
                float rootY = HeadAt(bodyWidth / 2f) + bodyHeight * 0.40f;
                // At this height each wing root overlaps the body's shoulder.
                float leftRoot = bodyLeft + bodyWidth * 0.105f;
                float rightRoot = bodyLeft + bodyWidth * 0.895f;
                float leftHeight = wingWidth * leftWing.Height / leftWing.Width;
                float rightHeight = wingWidth * rightWing.Height / rightWing.Width;
                DrawPart(g, leftWing, new RectangleF(leftRoot - wingWidth,
                    rootY - leftHeight * 0.53f, wingWidth, leftHeight));
                DrawPart(g, rightWing, new RectangleF(rightRoot,
                    rootY - rightHeight * 0.53f, wingWidth, rightHeight));
                result.Save(destinationPath, ImageFormat.Png);
            }
        }
        finally { foreach (Bitmap part in parts) part.Dispose(); }
    }

    public void WriteHorns(string sourcePath, string destinationPath)
    {
        List<Bitmap> parts = SeparateParts(sourcePath, 2);
        try
        {
            using (Bitmap result = NewLayer())
            using (Graphics g = LayerGraphics(result))
            {
                for (int i = 0; i < parts.Count; i++)
                {
                    Bitmap horn = parts[i];
                    float rootX = bodyWidth * (i == 0 ? 0.28f : 0.72f);
                    float rootY = HeadAt(rootX) + 110;
                    float width = 220;
                    float height = width * horn.Height / horn.Width;
                    GraphicsState state = g.Save();
                    g.TranslateTransform(bodyLeft + rootX, rootY);
                    g.RotateTransform(i == 0 ? -28 : 28);
                    DrawPart(g, horn, new RectangleF(-width / 2, -height, width, height));
                    g.Restore(state);
                }
                result.Save(destinationPath, ImageFormat.Png);
            }
        }
        finally { foreach (Bitmap part in parts) part.Dispose(); }
    }

    public static void Export(string bodyPath, string sourceDirectory, string destinationDirectory)
    {
        OverlayCanvas2048 layout = new OverlayCanvas2048(bodyPath);
        layout.WriteFace(Path.Combine(sourceDirectory, "Slime_Face_Normal.png"),
            Path.Combine(destinationDirectory, "Slime_Face_Normal.png"));
        layout.WriteHeadpiece(Path.Combine(sourceDirectory, "Slime_Costume_Crown.png"),
            Path.Combine(destinationDirectory, "Slime_Costume_Crown.png"), 760, 0.12f, 0.88f, 14);
        layout.WriteHeadpiece(Path.Combine(sourceDirectory, "Slime_Costume_Joker.png"),
            Path.Combine(destinationDirectory, "Slime_Costume_Joker.png"), 1100, 0.225f, 0.775f, 16, true);
        layout.WriteAngel(Path.Combine(sourceDirectory, "Slime_Costume_Angel.png"),
            Path.Combine(destinationDirectory, "Slime_Costume_Angel.png"));
        layout.WriteHorns(Path.Combine(sourceDirectory, "Slime_Costume_Horns.png"),
            Path.Combine(destinationDirectory, "Slime_Costume_Horns.png"));
    }
}
