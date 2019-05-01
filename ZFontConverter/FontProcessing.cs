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
                new ByteMapFont(fontFileStream)
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
            ConvertFont(fontFName, font, outDir);
        }

        // Font data is already available
        public static void DrawFontOn(FontFormat font, Graphics graphics, Rectangle rect)
        {
            if (!font.Ready) return;
            int curXPos = 0;
            int curYPos = 0;
            for (byte bc = 0; bc < 255; bc++)
            {
                FontCharacterImage? CharImg = font.GetBitmapFor(bc);
                if (CharImg.HasValue)
                {
                    Bitmap bitmap = CharImg.Value.bitmap;
                    int nextXPos = curXPos + bitmap.Width + font.GlobalKerning;
                    if (nextXPos > rect.Width)
                    {
                        curYPos += (int)font.FontHeight;
                        curXPos = 0;
                        nextXPos = 0;
                    }
                    graphics.DrawImageUnscaled(bitmap, curXPos, curYPos + CharImg.Value.yOffset);
                    curXPos = nextXPos;
                }
            }
        }

        public static void ConvertFont(string fontFName, FontFormat font, string outDir = "")
        {
            if (!font.Ready) return;
            if (outDir == "")
            {
                outDir = Path.GetDirectoryName(fontFName);
            }

            byte validChars = 0;
            string fontName = Path.GetFileNameWithoutExtension(fontFName);
            string FontCharDir = $"{outDir}{Path.DirectorySeparatorChar}fonts{Path.DirectorySeparatorChar}{fontName}";
            for (byte bc = 0; bc < 255; bc++)
            {
                byte[] isoChars = { bc };
                string ucString = codePage.GetString(isoChars);
                ushort codePoint = ucString[0];
                FontCharacterImage? CharBmp = font.GetBitmapFor(bc);
                if (CharBmp != null)
                {
                    var bitmap = CharBmp.Value.bitmap;
                    Directory.CreateDirectory(FontCharDir);
                    string fname = String.Format("{1}{2}{0:X4}.png", codePoint, FontCharDir, Path.DirectorySeparatorChar);
                    bitmap.Save(fname, System.Drawing.Imaging.ImageFormat.Png);
                    if (CharBmp.Value.xOffset != 0 || CharBmp.Value.yOffset != 0)
                    {
                        FileStream pngFile = File.Open(fname, FileMode.Open, FileAccess.ReadWrite);
                        // Re-write PNG file with grAB chunk inserted
                        PNGFile png = new PNGFile(pngFile);
                        png.Read();
                        png.InsertGrabChunk(CharBmp.Value.xOffset, -CharBmp.Value.yOffset);
                        png.Write(fname);
                    }
                    validChars += 1;
                }
            }
            if (validChars > 0)
            {
                FileStream infoFileStream = File.Open($"{FontCharDir}{Path.DirectorySeparatorChar}font.inf", FileMode.Create, FileAccess.ReadWrite);
                StreamWriter sw = new StreamWriter(infoFileStream);
                sw.Write(font.GetFontInfo());
                sw.Flush();
                infoFileStream.Close();
            }
        }
    }
}
