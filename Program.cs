using System;
using System.Drawing;
using System.Device.Gpio;

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
        private static IReader _reader;

        public static void displayPage(Bitmap bmp)
        {
            try
            {
                _log.LogInformation("e-Reader change page");
                _log.LogInformation("init and Clear");
                _epd.init();
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
                .AddSingleton<IReader, Reader>()
                .BuildServiceProvider();

            _log = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            _epd = serviceProvider.GetService<IEPD>();
            _im = serviceProvider.GetService<IImageManipulation>();
            _reader = serviceProvider.GetService<IReader>();

            _log.LogDebug("Hello World!");
            GpioController controller = new GpioController(PinNumberingScheme.Logical);
            var next_button = 20; // 38
            var home_button = 21; // 40

            controller.OpenPin(home_button, PinMode.InputPullUp);
            controller.OpenPin(next_button, PinMode.InputPullUp);

            int pageNumber = 0;
            int bookMode = 0;
            bool hasMore = false;

            _reader.openBook("3musketeers.epub");

            while (controller.Read(home_button) == true)
            {
                if (controller.Read(next_button) == false)
                {
                    switch (bookMode)
                    {
                        case 0:
                            displayPage(_reader.getCoverImage());
                            bookMode++;
                            break;
                        case 1:
                            displayPage(_reader.getTitlePage());
                            bookMode++;
                            break;  
                        case 2:
                            var toc = _reader.getToc(pageNumber);
                            displayPage(toc.Item1);
                            hasMore = toc.Item2;
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
                            var page = _reader.getPageNumber(pageNumber);
                            displayPage(page.Item1);
                            hasMore = page.Item2;
                            if (hasMore) {
                                pageNumber++;
                            }
                            else
                            {
                                pageNumber = 0;
                                bookMode = 0;
                            }
                            break;
                    }
                }
            }
        }
    }
}
