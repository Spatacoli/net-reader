using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace net_reader
{
    public interface IImageManipulation
    {
        Bitmap GetBitmap(Size maxSize);
        Bitmap AddTextToBmp(Bitmap bmp, string text, SolidBrush brush, PointF location, int fontSize = 36);
        Size FigureOutCoverSize(Size size, Size maxSize);
        Bitmap AddCoverImage(Bitmap bmp, Image coverImage, PointF location, Size maxSize);
    }
}
