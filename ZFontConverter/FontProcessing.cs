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
        public static void ConvertFont(string fontFName)
        {
            FileStream fontFileStream = File.Open(fontFName, FileMode.Open, FileAccess.Read);
            FontFormat font = FormatOf(fontFileStream);
            font.Read();
            ConvertFont(fontFName, font);
        }
        // Font data is already available
        public static void ConvertFont(string fontFName, FontFormat font)
        {
            byte validChars = 0;
            string fontName = Path.GetFileNameWithoutExtension(fontFName);
            string FontCharDir = $"fonts{Path.DirectorySeparatorChar}{fontName}";
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
                    var handle = bitmap.GetHbitmap();
                    Image img = Image.FromHbitmap(handle);
                    string fname = String.Format("{1}{2}{0:X4}.png", codePoint, FontCharDir, Path.DirectorySeparatorChar);
                    img.Save(fname, System.Drawing.Imaging.ImageFormat.Png);
                    if (CharBmp.Value.xOffset != 0 || CharBmp.Value.yOffset != 0)
                    {
                        FileStream pngFile = File.Open(fname, FileMode.Open, FileAccess.ReadWrite);
                        // Re-write PNG file with grAB chunk inserted
                        PNGFile png = new PNGFile(pngFile);
                        png.Read();
                        png.InsertGrabChunk(CharBmp.Value.xOffset, CharBmp.Value.yOffset);
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
