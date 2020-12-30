using System;
using System.Drawing;
using System.IO;
using System.Text;

using Microsoft.Extensions.Logging;

using HtmlAgilityPack;
using VersOne.Epub;

using net_reader.waveshare.epd;

namespace net_reader
{
    public class Reader : IReader
    {
        private ILogger _log;
        private EpubBook book;
        private IImageManipulation _im;
        private IEPD _epd;
        private string bookContent;
        private int linesPerPage = 49;
        private int charsPerLine = 56;

        public Reader(ILoggerFactory loggerFactory, IImageManipulation imageManipulation, IEPD ePD)
        {
            _log = loggerFactory.CreateLogger<Reader>();
            _im = imageManipulation;
            _epd = ePD;
        }

        public void openBook(string path)
        {
            book = EpubReader.ReadBook(path);
            bookContent = readBook();
        }

        public Bitmap getTitlePage()
        {
            _log.LogDebug("displayTitlePage");

            PointF location = new PointF(10f, 10f);
            Bitmap bmp = _im.GetBitmap(_epd.getSizeReversed());
            bmp = _im.AddTextToBmp(bmp, book.Title, ImageManipulation.BLACK, location, 24);
            location = new PointF(10f, 50f);
            bmp = _im.AddTextToBmp(bmp, book.Author, ImageManipulation.BLACK, location, 12);
            return bmp;
        }

        public Bitmap getCoverImage()
        {
            _log.LogDebug("displayCoverImage");

            PointF location = new PointF(10f, 10f);
            Bitmap bmp = _im.GetBitmap(_epd.getSizeReversed());

            byte[] coverImageContent = book.CoverImage;
            if (coverImageContent != null)
            {
                _log.LogInformation("We have a cover image");
                using (MemoryStream coverImageStream = new MemoryStream(coverImageContent))
                {
                    Image coverImage = Image.FromStream(coverImageStream);
                    _log.LogInformation($"Image size is {coverImage.Width}x{coverImage.Height}");
                    location = new PointF(0f, 0f);
                    bmp = _im.AddCoverImage(bmp, coverImage, location, _epd.getSizeReversed());
                }
            }

            return bmp;
        }

        public Tuple<Bitmap, bool> getToc(int pageNumber = 0)
        {
            _log.LogDebug($"displayToc page {pageNumber}");
            PointF location = new PointF(0f, 0f);
            Bitmap bmp = _im.GetBitmap(_epd.getSizeReversed());
            bool hasMore = false;

            _log.LogDebug($"navigation is {book.Navigation.Count} long");

            var navItems = book.Navigation.ToArray();
            int max = Math.Min(navItems.Length, (linesPerPage * (pageNumber + 1)));
            for (int i = linesPerPage * pageNumber; i < max; i++)
            {
                var navItem = navItems[i];
                bmp = _im.AddTextToBmp(bmp, navItem.Title, ImageManipulation.BLACK, location, 10);
                location.Y = location.Y + 12f;
            }
            if (navItems.Length > (linesPerPage * (pageNumber + 1))) {
                hasMore = true;
            }
            return new Tuple<Bitmap, bool>(bmp, hasMore);
        }

        public Tuple<Bitmap, bool> getPageNumber(int pageNumber)
        {
            _log.LogDebug($"displayPageNumber page {pageNumber}");

            PointF location = new PointF(0f, 0f);
            Bitmap bmp = _im.GetBitmap(_epd.getSizeReversed());

            bool hasMore = false;

            int skipChars = pageNumber * charsPerLine * linesPerPage;

            string[] lines = bookContent.Split("\n");

            for (int i = 0; i < linesPerPage; i++)
            {
                int lineNumber = i + (pageNumber * linesPerPage);
                if (lineNumber >= lines.Length)
                {
                    hasMore = false;
                    break;
                }

                string lineToDisplay = lines[lineNumber];

                if(lineToDisplay.Length > charsPerLine)
                {
                    int startChar = 0;
                    int endChar = charsPerLine;
                    while(i < linesPerPage && startChar < endChar)
                    {
                        int length = Math.Min(charsPerLine, (lineToDisplay.Length - startChar));
                        string displayPart = lineToDisplay.Substring(startChar, length);
                        bmp = _im.AddTextToBmp(bmp, displayPart, ImageManipulation.BLACK, location, 10);
                        startChar = startChar + length;
                        endChar = Math.Min(startChar + charsPerLine, lineToDisplay.Length);
                        location.Y = location.Y + 12f;
                        i++;
                        _log.LogDebug($"startChar = {startChar} | endChar = {endChar} | i = {i}");
                    }

                    if (i >= linesPerPage)
                    {
                        _log.LogWarning("This string has more than can fit on the page");
                    }
                }
                else 
                {
                    bmp = _im.AddTextToBmp(bmp, lineToDisplay, ImageManipulation.BLACK, location, 10);
                    location.Y = location.Y + 12f;
                }
            }

            if (((pageNumber + 1) * linesPerPage) < lines.Length)
            {
                hasMore = true;
            }

            return new Tuple<Bitmap, bool>(bmp, hasMore);
        }

        private string readBook()
        {
            StringBuilder sb = new StringBuilder();
            foreach (EpubTextContentFile textContentFile in book.ReadingOrder)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(textContentFile.Content);
                foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
                {
                    sb.AppendLine(node.InnerText.Trim());
                }
            }
            return sb.ToString();
        }
    }
}
