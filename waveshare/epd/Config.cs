using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;

using Microsoft.Extensions.Logging;

namespace net_reader.waveshare.epd
{
    class Config : IConfig
    {
        public const int RST_PIN = 17;
        public const int DC_PIN = 25;
        public const int CS_PIN = 8;
        public const int BUSY_PIN = 24;

        private SpiDevice _spi;
        private GpioController _gpio;

        private ILogger _log;

        public Config(ILoggerFactory logggerFactory)
        {
            _log = logggerFactory.CreateLogger<Config>();
            _gpio = new GpioController(PinNumberingScheme.Logical);
            _gpio.OpenPin(RST_PIN, PinMode.Input);
            _gpio.OpenPin(DC_PIN, PinMode.Input);
            _gpio.OpenPin(CS_PIN, PinMode.Input);
            _gpio.OpenPin(BUSY_PIN, PinMode.Output);
            var settings = new SpiConnectionSettings(0, 0);
            _spi = SpiDevice.Create(settings);
        }

        public void digital_write(int pin, PinValue value)
        {
            _gpio.Write(pin, value);
        }

        public PinValue digital_read(int pin)
        {
            return _gpio.Read(pin);
        }

        /// <summary>
        /// delay_ms puts the thread to sleep for a number of milliseconds
        /// </summary>
        /// <param name="delayTimeMS">Milliseconds to sleep for</param>
        public void delay_ms(int delayTimeMS)
        {
            Thread.Sleep(delayTimeMS);
        }

        public void spi_writebyte(byte data)
        {
            _spi.WriteByte(data);
        }

        public int module_init()
        {
            _gpio.SetPinMode(RST_PIN, PinMode.Output);
            _gpio.SetPinMode(DC_PIN, PinMode.Output);
            _gpio.SetPinMode(CS_PIN, PinMode.Output);
            _gpio.SetPinMode(BUSY_PIN, PinMode.Input);
            _spi.ConnectionSettings.ClockFrequency = 4_000_000;
            _spi.ConnectionSettings.Mode = SpiMode.Mode0;
            return 0;
        }

        public void module_exit()
        {
            _log.LogDebug("spi end");
            _spi.Dispose();

            _log.LogDebug("close 5V, Module enters 0 power consumption ...");
            _gpio.Write(RST_PIN, PinValue.Low);
            _gpio.Write(DC_PIN, PinValue.Low);
        }
    }
}
