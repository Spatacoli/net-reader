using System;
using System.Device.Gpio;
using System.Drawing;

namespace net_reader.waveshare.epd
{
    public interface IEPD
    {
        void reset();
        void sendCommand(byte command);
        void sendData(byte data);
        void ReadBusyHigh();
        void ReadBusyLow();
        int init();
        byte[] getBuffer(Bitmap image);
        void display(byte[] buffer);
        void Clear();
        void sleep();
        void DevExit();
    }
}
