using System.Drawing;
namespace ZFontConverter
{
    public struct FontCharacterImage
    {
        public Bitmap bitmap;
        public int xOffset;
        public int yOffset;

        public FontCharacterImage(Bitmap bmp)
        {
            bitmap = bmp;
            xOffset = 0;
            yOffset = 0;
        }

        public FontCharacterImage(Bitmap bmp, int xOffset, int yOffset)
        {
            bitmap = bmp;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
        }
    }
}
