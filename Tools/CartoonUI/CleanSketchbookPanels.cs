using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class CleanSketchbookPanels
{
    private static GraphicsPath Rounded(float x, float y, float w, float h, float r)
    {
        GraphicsPath path = new GraphicsPath();
        float d = r * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void Fill(Graphics g, int rgb, float x, float y, float w, float h, float radius)
    {
        using (GraphicsPath path = Rounded(x, y, w, h, radius))
        using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, rgb, rgb, rgb)))
            g.FillPath(brush, path);
    }

    public static void Write(string path, int width, int height, bool cover)
    {
        using (Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.ScaleTransform(width / 1600f, height / 900f);
            if (cover)
            {
                Fill(g, 80, 8, 5, 1584, 890, 44);
                Fill(g, 184, 13, 10, 1574, 880, 39);
                Fill(g, 243, 13, 10, 1574, 866, 39);
                using (SolidBrush shine = new SolidBrush(Color.White))
                {
                    g.FillEllipse(shine, 33, 25, 72, 18);
                    g.FillEllipse(shine, 113, 26, 14, 14);
                }
            }
            else
            {
                Fill(g, 146, 30, 27, 1540, 846, 37);
                Fill(g, 224, 33, 30, 1534, 838, 34);
                Fill(g, 188, 33, 30, 1534, 829, 34);
                Fill(g, 244, 33, 30, 1534, 825, 34);
                Fill(g, 201, 33, 30, 1534, 816, 34);
                Fill(g, 255, 33, 30, 1534, 812, 34);
            }
            bitmap.Save(path, ImageFormat.Png);
        }
    }
}
