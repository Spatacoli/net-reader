using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace net_reader
{
    public interface IImageManipulation
    {
        Bitmap TextToBmp(string text, SolidBrush brush, PointF location, int fontSize = 36);
        Bitmap AddTextToBmp(Bitmap bmp, string text, SolidBrush brush, PointF location, int fontSize = 36);
        Tuple<int, int> FigureOutCoverSize(int width, int height);
        Bitmap AddCoverImage(Bitmap bmp, Image coverImage, PointF location);
    }
}