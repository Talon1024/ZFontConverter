using System.Drawing;
namespace ZFontConverter
{
    public struct FontCharacterImage
    {
        public Bitmap bitmap;
        public int xOffset;
        public int yOffset;
        public int? xShift;

        public FontCharacterImage(Bitmap bmp)
        {
            bitmap = bmp;
            xOffset = 0;
            yOffset = 0;
            xShift = null;
        }

        public FontCharacterImage(Bitmap bmp, int xOffset, int yOffset, int? shift)
        {
            bitmap = bmp;
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            xShift = shift ?? 0;
        }
    }
}
