using System;
using System.IO;
using System.Text;
using System.Drawing;

namespace ZFontConverter
{
    public static class FontProcessing
    {
        public static Encoding codePage;
        static FontProcessing()
        {
            codePage = Encoding.GetEncoding("iso-8859-1");
        }
        private static FontFormat FormatOf(FileStream fontFileStream)
        {
            FontFormat[] fontFormats = {
                new FON2Font(fontFileStream),
                new FON1Font(fontFileStream),
                new ByteMapFont(fontFileStream),
            };
            foreach (var format in fontFormats)
            {
                if (format.IsFormat())
                {
                    return format;
                }
            }
            throw new Exception($"Format of font file {fontFileStream.Name} not supported.");
        }

        // Font data needs to be obtained
        public static FontFormat ReadFont(string fontFName)
        {
            try
            {
                FileStream fontFileStream = File.Open(fontFName, FileMode.Open, FileAccess.Read);
                FontFormat font = FormatOf(fontFileStream);
                font.Read();
                return font;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Cannot convert {fontFName} for some reason: {e}");
            }
            return null;
        }

        public static void ConvertFont(string fontFName, string outDir = "")
        {
            FontFormat font = ReadFont(fontFName);
            if (font == null) return;
            ExportFont(fontFName, font, outDir);
        }

        // Font data is already available
        public static void DrawFontOn(FontFormat font, Graphics graphics, Rectangle rect)
        {
            if (!font.Ready) return;
            int curXPos = 0;
            int curYPos = 0;
            foreach (var charImage in font.Images)
            {
                int moveX = charImage.XShift ?? charImage.Bitmap.Width;
                int nextXPos = curXPos + moveX + font.GlobalKerning;
                if (nextXPos > rect.Width)
                {
                    curYPos += (int)font.FontHeight;
                    curXPos = 0;
                    nextXPos = moveX + font.GlobalKerning;
                }
                graphics.DrawImageUnscaled(charImage.Bitmap, curXPos, curYPos + charImage.YOffset);
                curXPos = nextXPos;
            }
        }

        public static void ApplyOffsets(string charFName, Color[] Palette, int xOffset, int yOffset)
        {
            PNGFile png = new PNGFile();
            using(FileStream pngFileStream = File.Open(charFName, FileMode.Open, FileAccess.ReadWrite))
            {
                png.Open(pngFileStream); // Read
            }
            png.ReplacePalette(Palette, 0);
            // Set offsets if necessary
            if (xOffset != 0 || yOffset != 0)
            {
                png.InsertGrabChunk(xOffset, yOffset);
            }
            else
            {
                png.RemoveGrabChunk();
            }
            using (FileStream pngFileStream = File.Open(charFName, FileMode.Create, FileAccess.Write))
            {
                png.Write(pngFileStream); // Write
            }
        }

        public static void ExportFont(string fontFName, FontFormat font, string outDir = "")
        {
            if (!font.Ready) return;
            if (outDir == "")
            {
                outDir = Path.GetDirectoryName(fontFName);
            }
            string fontName = Path.GetFileNameWithoutExtension(fontFName);
            string fontCharDir = string.Format("{0}{2}fonts{2}{1}{2}", outDir, fontName, Path.DirectorySeparatorChar);
            Directory.CreateDirectory(fontCharDir);
            font.Export(fontCharDir, ApplyOffsets);
        }
    }
}
