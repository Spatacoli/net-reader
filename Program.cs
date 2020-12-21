using System;
using System.Drawing;
using System.Device.Gpio;
using System.IO;
using System.Text;
using System.Threading;

using VersOne.Epub;
using HtmlAgilityPack;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using net_reader.waveshare.epd;

namespace net_reader
{
    class Program
    {
        private static ILogger _log;
        private static IEPD _epd;
        private static IImageManipulation _im;

        public static void displayPage(Bitmap bmp)
        {
            try
            {
                _log.LogInformation("e-Reader change page");
                _log.LogInformation("init");
                _epd.init();
                _log.LogInformation("Clear");
                _epd.Clear();
                _log.LogInformation("Display bmp image");
                _epd.display(_epd.getBuffer(bmp));
                _log.LogInformation("sleep");
                _epd.sleep();
                _epd.DevExit();
            }
            catch (Exception e)
            {
                _log.LogError("Error in displayPage: " + e.Message);
            }
        }

        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddSingleton<IImageManipulation, ImageManipulation>()
                .AddSingleton<IConfig, Config>()
                .AddSingleton<IEPD, EPD>()
                .BuildServiceProvider();

            _log = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            _epd = serviceProvider.GetService<IEPD>();
            _im = serviceProvider.GetService<IImageManipulation>();

            _log.LogDebug("Hello World!");
            GpioController controller = new GpioController(PinNumberingScheme.Logical);
            var next_button = 20; // 38
            var home_button = 21; // 40

            controller.OpenPin(home_button, PinMode.InputPullUp);
            controller.OpenPin(next_button, PinMode.InputPullUp);

            int pageNumber = 0;
            int bookMode = 2;
            while (controller.Read(home_button) == true)
            {
                if (controller.Read(next_button) == false)
                {
                    switch (bookMode)
                    {
                        case 0:
                            displayCoverImage();
                            bookMode++;
                            break;
                        case 1:
                            displayTitlePage();
                            bookMode++;
                            break;  
                        case 2:
                            bool hasMore = displayTOC(pageNumber);
                            if (hasMore)
                            {
                                pageNumber++;
                            }
                            else 
                            {
                                pageNumber = 0;
                                bookMode++;
                            }
                            break;                      
                        default:
                            // displayPageNumber(pageNumber - 2);
                            // pageNumber++;
                            pageNumber = 0;
                            break;
                    }
                }
            }
        }

        public static void displayTitlePage()
        {
            EpubBook epubBook = EpubReader.ReadBook("epub/3musketeers.epub");

            PointF location = new PointF(10f, 10f);
            Bitmap bmp = _im.TextToBmp(epubBook.Title, ImageManipulation.BLACK, location, _epd.getSize(), 24);
            location = new PointF(10f, 50f);
            bmp = _im.AddTextToBmp(bmp, epubBook.Author, ImageManipulation.BLUE, location, 12);

            displayPage(bmp);
        }

        public static void displayCoverImage()
        {
            EpubBook epubBook = EpubReader.ReadBook("epub/3musketeers.epub");

            PointF location = new PointF(10f, 10f);
            Bitmap bmp = _im.TextToBmp(string.Empty, ImageManipulation.BLACK, location, _epd.getSize(), 24);

            byte[] coverImageContent = epubBook.CoverImage;
            if (coverImageContent != null)
            {
                _log.LogInformation("We have a cover image");
                using (MemoryStream coverImageStream = new MemoryStream(coverImageContent))
                {
                    Image coverImage = Image.FromStream(coverImageStream);
                    _log.LogInformation($"Image size is {coverImage.Width}x{coverImage.Height}");
                    location = new PointF(0f, 0f);
                    bmp = _im.AddCoverImage(bmp, coverImage, location, _epd.getSize());
                }
            }

            displayPage(bmp);
        }

        public static bool displayTOC(int pageNumber = 0)
        {
            PointF location = new PointF(0f, 0f);
            Bitmap bmp = _im.TextToBmp(string.Empty, ImageManipulation.BLACK, location,_epd.getSize(), 24);
            bool hasMore = false;
            using (EpubBookRef epubBook = EpubReader.OpenBook("epub/3musketeers.epub"))
            {
                var navItems = epubBook.GetNavigation().ToArray();
                int max = Math.Min(navItems.Length, (60 * (pageNumber + 1)));
                for (int i = 60 * pageNumber; i < max; i++)
                {
                    var navItem = navItems[i];
                    bmp = _im.AddTextToBmp(bmp, navItem.Title, ImageManipulation.BLACK, location, 8);
                    location.Y = location.Y + 10f;
                }
                if (navItems.Length > (60 * (pageNumber + 1))) {
                    hasMore = true;
                }
            }
            displayPage(bmp);
            return hasMore;
        }

        public static bool displayPageNumber(int pageNumber)
        {
            throw new NotImplementedException();
        }
    }
}
