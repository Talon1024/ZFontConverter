using System.Drawing;
using System.Text;
namespace ZFontConverter
{
    public abstract class FontFormat
    {
        public uint SpaceWidth { get; protected set; }
        public uint FontHeight { get; protected set; }
        public int GlobalKerning { get; protected set; }
        public bool Ready { get; protected set; }
        public abstract bool IsFormat();
        public abstract void Read();
        public abstract FontCharacterImage? GetBitmapFor(byte character);
        /*
         Notes about font info:
         The file must be named font.inf
         It can have any of these attributes, which map to respective ZDoom font properties:
         Kerning <int>: GlobalKerning
         Scale <float>[, <float>]: Scale.X, Scale.Y
         SpaceWidth: SpaceWidth
         FontHeight: FontHeight
         CellSize <int>, <int>: FixedWidth, FontHeight
         TranslationType <"Console"|"Standard">: TranslationType        
        */
        public virtual string GetFontInfo()
        {
            StringBuilder FontInfo = new StringBuilder($"SpaceWidth {SpaceWidth}\n");
            FontInfo.Append($"FontHeight {FontHeight}\n");
            if (GlobalKerning != 0)
            {
                FontInfo.Append($"Kerning {GlobalKerning}\n");
            }
            return FontInfo.ToString();
        }
    }
}
