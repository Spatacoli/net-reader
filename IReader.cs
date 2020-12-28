using System;
using System.Drawing;

namespace net_reader
{
    public interface IReader
    {
        void openBook(string path);
        Bitmap getTitlePage();
        Bitmap getCoverImage();
        Tuple<Bitmap, bool> getToc(int pageNumber = 0);
        Tuple<Bitmap, bool> getPageNumber(int pageNumber);
    }
}
