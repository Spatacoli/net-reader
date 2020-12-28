using System.Device.Gpio;
using System.Device.Spi;
using System.Threading;

namespace net_reader.waveshare.epd
{
    public interface IConfig
    {
        void digital_write(int pin, PinValue value);
        PinValue digital_read(int pin);
        void delay_ms(int delayTimeMS);
        void spi_writebyte(byte data);
        int module_init();
        void module_exit();
    }
}
