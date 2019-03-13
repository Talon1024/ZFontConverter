using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace ZFontConverter {
    class MainClass
    {
        private static Encoding codePage;
        public static void Main(string[] args)
        {
            bool setCodePage = false;
            codePage = Encoding.GetEncoding("iso-8859-1");
            if (args.Length > 0)
            {

                foreach (string arg in args)
                {
                    if (arg == "--codepage")
                    {
                        setCodePage = true;
                        continue;
                    }
                    else if (setCodePage)
                    {
                        codePage = Encoding.GetEncoding(arg);
                        continue;
                    }
                    FileStream fontFileStream;
                    try
                    {
                        fontFileStream = File.Open(arg, FileMode.Open, FileAccess.Read);
                        FontFormat[] fontFormats = {
                            new FON2Font(fontFileStream),
                            new ByteMapFont(fontFileStream)
                        };
                        foreach (var format in fontFormats)
                        {
                            if (format.IsFormat())
                            {
                                ProcessFont(format, arg);
                                fontFileStream.Close();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"{arg} cannot be converted: {ex}");
                    }
                }
            }
            else
            {
                // Help
                Console.Write(
                    "ZFontConverter 0.1 by Kevin Caccamo\n" +
                    "Converts FON2 and BMF fonts to GZDoom Unicode fonts\n" +
                    "\n" +
                    "Usage:\n" +
                    "ZFontConverter.exe [--codepage <encoding>] font...\n" +
                    "\n" +
                    "Default codepage is iso-8859-1\n" +
                    "See https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding?view=netframework-4.7.2 for a list of supported encodings and their names.\n"
                    );
            }
        }

        private static void ProcessFont(FontFormat font, string fontFName)
        {
            font.Read();
            byte validChars = 0;
            string fontName = Path.GetFileNameWithoutExtension(fontFName);
            string FontCharDir = $"fonts{Path.DirectorySeparatorChar}{fontName}";
            for (byte bc = 0; bc < 255; bc++)
            {
                byte[] isoChars = { bc };
                String ucString = codePage.GetString(isoChars);
                ushort codePoint = (ushort)ucString[0];
                Bitmap CharBmp = font.GetBitmapFor(bc);
                if (CharBmp != null)
                {
                    Directory.CreateDirectory(FontCharDir);
                    var handle = CharBmp.GetHbitmap();
                    Image img = Image.FromHbitmap(handle);
                    string fname = String.Format("{1}{2}{0:X4}.png", codePoint, FontCharDir, Path.DirectorySeparatorChar);
                    img.Save(fname, System.Drawing.Imaging.ImageFormat.Png);
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
