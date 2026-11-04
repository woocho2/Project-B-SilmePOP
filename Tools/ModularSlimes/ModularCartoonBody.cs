using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class ModularCartoonBody
{
    private static GraphicsPath Body()
    {
        GraphicsPath p = new GraphicsPath();
        // Rounded shoulders and full sides; the contour reaches all canvas edges.
        p.AddBezier(512, 12, 802, 12, 1012, 216, 1012, 580);
        p.AddBezier(1012, 580, 1012, 810, 978, 932, 878, 976);
        p.AddBezier(878, 976, 790, 1012, 654, 1012, 512, 1012);
        p.AddBezier(512, 1012, 370, 1012, 234, 1012, 146, 976);
        p.AddBezier(146, 976, 46, 932, 12, 810, 12, 580);
        p.AddBezier(12, 580, 12, 216, 222, 12, 512, 12);
        p.CloseFigure();
        return p;
    }

    public static void WriteBody(string path)
    {
        using (Bitmap bitmap = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bitmap))
        using (GraphicsPath body = Body())
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            using (SolidBrush white = new SolidBrush(Color.FromArgb(244, 244, 244)))
                g.FillPath(white, body);

            GraphicsState clipped = g.Save();
            g.SetClip(body);
            using (GraphicsPath shade = new GraphicsPath())
            using (SolidBrush gray = new SolidBrush(Color.FromArgb(216, 216, 216)))
            {
                shade.AddBezier(0, 760, 130, 865, 300, 900, 512, 900);
                shade.AddBezier(512, 900, 724, 900, 894, 865, 1024, 760);
                shade.AddLine(1024, 760, 1024, 1100);
                shade.AddLine(1024, 1100, 0, 1100);
                shade.CloseFigure();
                g.FillPath(gray, shade);
            }

            using (SolidBrush shine = new SolidBrush(Color.White))
            {
                GraphicsState rotation = g.Save();
                g.TranslateTransform(255, 245);
                g.RotateTransform(-40);
                g.FillEllipse(shine, -90, -29, 180, 58);
                g.Restore(rotation);
                g.FillEllipse(shine, 381, 97, 38, 38);
            }
            g.Restore(clipped);

            using (Pen contour = new Pen(Color.FromArgb(36, 36, 36), 24))
            {
                contour.LineJoin = LineJoin.Round;
                g.DrawPath(contour, body);
            }
            bitmap.Save(path, ImageFormat.Png);
        }
    }

    private static Rectangle FacePlacement(int canvasWidth, int canvasHeight, int faceWidth, int faceHeight)
    {
        int width = (int)Math.Round(canvasWidth * 0.36);
        int height = (int)Math.Round(width * faceHeight / (double)faceWidth);
        return new Rectangle((canvasWidth - width) / 2,
            (int)Math.Round(canvasHeight * 0.49), width, height);
    }

    // Keep the body-sized transparent canvas so this layer overlays at scale 1.
    public static void WriteFaceLayer(string tightFacePath, string destinationPath)
    {
        using (Bitmap face = new Bitmap(tightFacePath))
        using (Bitmap layer = new Bitmap(1024, 1024, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(layer))
        using (ImageAttributes attributes = new ImageAttributes())
        {
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(face, FacePlacement(1024, 1024, face.Width, face.Height),
                0, 0, face.Width, face.Height, GraphicsUnit.Pixel, attributes);
            layer.Save(destinationPath, ImageFormat.Png);
        }
    }

    public static void ComposeFaceLayer(string bodyPath, string layerPath, string destinationPath)
    {
        using (Bitmap body = new Bitmap(bodyPath))
        using (Bitmap layer = new Bitmap(layerPath))
        using (Bitmap result = new Bitmap(body.Width, body.Height, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(result))
        {
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImageUnscaled(body, 0, 0);
            g.CompositingMode = CompositingMode.SourceOver;
            g.DrawImageUnscaled(layer, 0, 0);
            result.Save(destinationPath, ImageFormat.Png);
        }
    }

    public static void ComposeFace(string bodyPath, string facePath, string destinationPath)
    {
        using (Bitmap body = new Bitmap(bodyPath))
        using (Bitmap face = new Bitmap(facePath))
        using (Bitmap result = new Bitmap(body.Width, body.Height, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(result))
        using (ImageAttributes attributes = new ImageAttributes())
        {
            g.Clear(Color.Transparent);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImageUnscaled(body, 0, 0);
            g.CompositingMode = CompositingMode.SourceOver;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            Rectangle destination = FacePlacement(body.Width, body.Height, face.Width, face.Height);
            g.DrawImage(face, destination, 0, 0, face.Width, face.Height,
                GraphicsUnit.Pixel, attributes);
            result.Save(destinationPath, ImageFormat.Png);
        }
    }
}
