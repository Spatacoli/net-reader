using System;
using System.Device.Gpio;
using System.Drawing;

using Microsoft.Extensions.Logging;

namespace net_reader.waveshare.epd
{
    class EPD : IEPD
    {
        public const int EPD_WIDTH = 600;
        public const int EPD_HEIGHT = 448;

        public const int BLACK = 0x000000;
        public const int WHITE = 0xffffff;
        public const int GREEN = 0x00ff00;
        public const int BLUE = 0x0000ff;
        public const int RED = 0xff0000;
        public const int YELLOW = 0xffff00;
        public const int ORANGE = 0xff8000;

        private int _reset_pin = Config.RST_PIN;
        private int _dc_pin = Config.DC_PIN;
        private int _busy_pin = Config.BUSY_PIN;
        private int _cs_pin = Config.CS_PIN;

        private IConfig epdconfig;

        private ILogger _log;

        public EPD(ILoggerFactory loggerFactory, IConfig config)
        {
            _log = loggerFactory.CreateLogger<EPD>();
            epdconfig = config;
        }

        public void reset()
        {
            epdconfig.digital_write(_reset_pin, PinValue.High);
            epdconfig.delay_ms(600);
            epdconfig.digital_write(_reset_pin, PinValue.Low);
            epdconfig.delay_ms(2);
            epdconfig.digital_write(_reset_pin, PinValue.High);
            epdconfig.delay_ms(200);
        }

        public void sendCommand(byte command)
        {
            epdconfig.digital_write(_dc_pin, PinValue.Low);
            epdconfig.digital_write(_cs_pin, PinValue.Low);
            epdconfig.spi_writebyte(command);
            epdconfig.digital_write(_cs_pin, PinValue.High);
        }

        public void sendData(byte data)
        {
            epdconfig.digital_write(_dc_pin, PinValue.High);
            epdconfig.digital_write(_cs_pin, PinValue.Low);
            epdconfig.spi_writebyte(data);
            epdconfig.digital_write(_cs_pin, PinValue.High);
        }

        public void ReadBusyHigh()
        {
            _log.LogDebug("e-Paper busy");
            while (epdconfig.digital_read(_busy_pin) == PinValue.Low)
            {
                epdconfig.delay_ms(100);
            }
            _log.LogDebug("e-Paper busy release");
        }

        public void ReadBusyLow()
        {
            _log.LogDebug("e-Paper busy");
            while (epdconfig.digital_read(_busy_pin) == PinValue.High)
            {
                epdconfig.delay_ms(100);
            }
            _log.LogDebug("e-Paper busy release");
        }

        public int init()
        {
            if (epdconfig.module_init() != 0)
            {
                return -1;
            }

            _log.LogDebug("reset");

            this.reset();

            this.ReadBusyHigh();
            this.sendCommand(0x00);
            this.sendData(0xEF);
            this.sendData(0x08);
            this.sendCommand(0x01);
            this.sendData(0x37);
            this.sendData(0x00);
            this.sendData(0x23);
            this.sendData(0x23);
            this.sendCommand(0x03);
            this.sendData(0x00);
            this.sendCommand(0x06);
            this.sendData(0xC7);
            this.sendData(0xC7);
            this.sendData(0x1D);
            this.sendCommand(0x30);
            this.sendData(0x3C);
            this.sendCommand(0x40);
            this.sendData(0x00);
            this.sendCommand(0x50);
            this.sendData(0x37);
            this.sendCommand(0x60);
            this.sendData(0x22);
            this.sendCommand(0x61);
            this.sendData(0x02);
            this.sendData(0x58);
            this.sendData(0x01);
            this.sendData(0xC0);
            this.sendCommand(0xE3);
            this.sendData(0xAA);

            epdconfig.delay_ms(100);
            this.sendCommand(0x50);
            this.sendData(0x37);

            return 0;
        }

        public byte[] getBuffer(Bitmap image)
        {
            byte[] buf = new byte[EPD_WIDTH * EPD_HEIGHT / 2];
            int imwidth = image.Width;
            int imheight = image.Height;
            _log.LogDebug($"imwidth = {imwidth} imheight = {imheight}");
            if (imheight == EPD_WIDTH && imwidth == EPD_HEIGHT)
            {
                _log.LogDebug("Need to rotate image");
                image = ImageManipulation.RotateImage(image);
                imwidth = image.Width;
                imheight = image.Height;
            }

            if (imwidth == EPD_WIDTH && imheight == EPD_HEIGHT)
            {
                _log.LogDebug("Width and Height match!");
                for (int y = 0; y < imheight; y++)
                {
                    for (int x = 0; x < imwidth; x++)
                    {
                        int add = (int)((x + y * EPD_WIDTH) / 2);
                        int color = 0;
                        Color pixelColor = image.GetPixel(x, y);
                        if (pixelColor == Color.FromArgb(0x00, 0x00, 0x00))
                        {
                            color = 0x00;
                        }
                        else if (pixelColor == Color.FromArgb(0xFF, 0xFF, 0xFF))
                        {
                            color = 0x01;
                        }
                        else if (pixelColor == Color.FromArgb(0x00, 0xFF, 0x00))
                        {
                            color = 0x02;
                        }
                        else if (pixelColor == Color.FromArgb(0x00, 0x00, 0xFF))
                        {
                            color = 0x03;
                        }
                        else if (pixelColor == Color.FromArgb(0xFF, 0x00, 0x00))
                        {
                            color = 0x04;
                        }
                        else if (pixelColor == Color.FromArgb(0xFF, 0xFF, 0x00))
                        {
                            color = 0x05;
                        }
                        else if (pixelColor == Color.FromArgb(0xFF, 0x80, 0x00))
                        {
                            color = 0x06;
                        }
                        int data_t = buf[add] & (~(0xF0 >> ((x % 2) * 4)));
                        buf[add] = Convert.ToByte(data_t | ((color << 4) >> ((x % 2) * 4)));
                    }
                }
            }
            else
            {
                _log.LogDebug($"Image size is wrong. Must be {EPD_WIDTH}x{EPD_HEIGHT} but instead is {imwidth}x{imheight}");
            }
            return buf;
        }

        public void display(byte[] buffer)
        {
            this.sendCommand(0x61);
            this.sendData(0x02);
            this.sendData(0x58);
            this.sendData(0x01);
            this.sendData(0xC0);
            this.sendCommand(0x10);

            for (int i = 0; i < EPD_HEIGHT; i++)
            {
                for (int j = 0; j < EPD_WIDTH/2; j++)
                {
                    this.sendData(buffer[j+((int) EPD_WIDTH/2)*i]);
                }
            }

            this.sendCommand(0x04);
            this.ReadBusyHigh();
            this.sendCommand(0x12);
            this.ReadBusyHigh();
            this.sendCommand(0x02);
            this.ReadBusyLow();
            epdconfig.delay_ms(500);
        }

        public void Clear()
        {
            this.sendCommand(0x61);     // Set Resolution Setting
            this.sendData(0x02);
            this.sendData(0x58);
            this.sendData(0x01);
            this.sendData(0xC0);
            this.sendCommand(0x10);
            for (int i = 0; i < EPD_HEIGHT; i++)
            {
                for (int j = 0; j < EPD_WIDTH / 2; j++)
                {
                    this.sendData(0x11);
                }
            }
            this.sendCommand(0x04);
            this.ReadBusyHigh();
            this.sendCommand(0x12);
            this.ReadBusyHigh();
            this.sendCommand(0x02);
            this.ReadBusyLow();
            epdconfig.delay_ms(500);
        }

        public void sleep()
        {
            epdconfig.delay_ms(500);
            this.sendCommand(0x07);     // DEEP_SLEEP
            this.sendData(0xA5);
            epdconfig.digital_write(_reset_pin, PinValue.Low);
        }

        public void DevExit()
        {
            epdconfig.module_exit();
        }

        public Size getSize()
        {
            return new Size(EPD_WIDTH, EPD_HEIGHT);
        }
    }
}
