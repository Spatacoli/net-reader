using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using Microsoft.Extensions.Logging;

namespace net_reader 
{
    public class ImageManipulation : IImageManipulation
    {
        public static SolidBrush WHITE = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        public static SolidBrush GREEN = new SolidBrush(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
        public static SolidBrush BLUE = new SolidBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF));
        public static SolidBrush RED = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
        public static SolidBrush YELLOW = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00));
        public static SolidBrush ORANGE = new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0x80, 0x00));
        public static SolidBrush BLACK = new SolidBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));

        private ILogger _log;

        public ImageManipulation(ILoggerFactory logggerFactory)
        {
            _log = logggerFactory.CreateLogger<ImageManipulation>();
        }

        public Bitmap TextToBmp(string text, SolidBrush brush, PointF location, Size maxSize, int fontSize = 36)
        {
            _log.LogDebug("Creating new bitmap");
            Bitmap bmp = new Bitmap(maxSize.Width, maxSize.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(WHITE, new Rectangle(0, 0, maxSize.Width, maxSize.Height));
            }
            return AddTextToBmp(bmp, text, brush, location, fontSize);
        }

        public static Bitmap RotateImage(Bitmap bmp)
        {
            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            using (MemoryStream ms = new MemoryStream(bmp.Width * bmp.Height))
            {
                bmp.Save(ms, ImageFormat.Bmp);
                bmp = new Bitmap(ms);
            }
            
            return bmp;
        }

        public Bitmap AddTextToBmp(Bitmap bmp, string text, SolidBrush brush, PointF location, int fontSize = 36)
        {
            _log.LogDebug("Adding Text to bitmap");
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (Font dejavu = new Font("DejaVu Sans Mono", fontSize))
                {
                    g.DrawString(text, dejavu, brush, location);
                }
            }

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipNone);
            using (MemoryStream ms = new MemoryStream(bmp.Width * bmp.Height))
            {
                bmp.Save(ms, ImageFormat.Bmp);
                bmp = new Bitmap(ms);
            }
            
            return bmp;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, 0, 0, width, height);
            }
            return bitmap;
        }

        public Size FigureOutCoverSize(Size size, Size maxSize)
        {
            var ratio = 1.0;
            var newHeight = size.Height;
            var newWidth = size.Width;
            if (size.Width > size.Height)
            {
                ratio = (float)size.Width/maxSize.Width;
                newWidth = maxSize.Width;
                newHeight = (int)(size.Height / ratio);
            }
            else 
            {
                ratio = (float)size.Height/maxSize.Height;
                newWidth = (int)(size.Width / ratio);
                newHeight = maxSize.Height;
            }
            _log.LogDebug($"ratio is {ratio}");
            return new Size(newWidth, newHeight);
        }

        public Bitmap AddCoverImage(Bitmap bmp, Image coverImage, PointF location, Size maxSize)
        {
            var newSize = FigureOutCoverSize(coverImage.Size, maxSize);
            Bitmap smallCover = ImageManipulation.ResizeImage(coverImage, newSize.Width, newSize.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(smallCover, location);
            }
            return bmp;
        }
    }
}