using System;
using System.Drawing;
namespace ZFontConverter
{
    public struct FontCharacterImage : IDisposable
    {
        public Bitmap Bitmap;
        public int XOffset;
        public int YOffset;
        public int? XShift;

        public FontCharacterImage(Bitmap bmp)
        {
            Bitmap = bmp;
            XOffset = 0;
            YOffset = 0;
            XShift = null;
        }

        public FontCharacterImage(Bitmap bmp, int xOffset, int yOffset, int? shift)
        {
            Bitmap = bmp;
            XOffset = xOffset;
            YOffset = yOffset;
            XShift = shift ?? 0;
        }

        public void Dispose()
        {
            Bitmap.Dispose();
        }
    }
}
