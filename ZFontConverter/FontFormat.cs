using System.Drawing;
using System.Text;
using System.IO;

namespace ZFontConverter
{
    public abstract class FontFormat
    {
        protected string Filename;
        public delegate void ApplyOffsetsCallback(string charFName, Color[] Palette, int xOffset, int yOffset);
        public uint SpaceWidth { get; protected set; }
        public uint FontHeight { get; protected set; }
        public int GlobalKerning { get; protected set; }
        public bool Ready { get; protected set; }
        public abstract bool IsFormat();
        public abstract void Read();
        public abstract FontCharacterImage? GetBitmapFor(byte codePoint); // Preview
        public abstract Color[] GetPalette(); // Font Palette
        public virtual void Export(string fontCharDir, ApplyOffsetsCallback ApplyOffsets)
        {
            string infoFileName = string.Format("{0}font.inf", fontCharDir);
            using(FileStream infoFileStream = File.Open(infoFileName, FileMode.Create, FileAccess.ReadWrite))
            {
                using(StreamWriter sw = new StreamWriter(infoFileStream))
                {
                    sw.Write(GetFontInfo());
                }
            }
        }
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
            return $"// {Filename}\n";
        }
        protected virtual string GetVariableWidthFontInfo()
        {
            StringBuilder FontInfo = new StringBuilder($"SpaceWidth {SpaceWidth}\n");
            FontInfo.Append($"FontHeight {FontHeight}\n");
            if (GlobalKerning != 0)
            {
                FontInfo.Append($"Kerning {GlobalKerning}\n");
            }
            return FontInfo.ToString();
        }
        protected virtual string GetMonospaceFontInfo()
        {
            return $"CellSize {SpaceWidth}, {FontHeight}\n";
        }
    }
}
