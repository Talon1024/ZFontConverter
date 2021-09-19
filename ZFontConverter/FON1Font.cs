using System;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ZFontConverter
{
    public class FON1Font : FontFormat
    {
        public static readonly byte[] Fon1header = Encoding.ASCII.GetBytes("FON1");
        private BinaryReader binaryReader;
        private Color[] Palette;
        private byte[][] Pixels;
        // Copied from GZDoom/Raze source code:
        // https://github.com/coelckers/Raze/blob/de816fa90a9240d891ad87b447bcae7a37ab0317/source/common/utility/utf8.cpp#L150
        // To be used later
        /*
        private ushort[] win1252map = {
            0x20AC,
            0x81  ,
            0x201A,
            0x0192,
            0x201E,
            0x2026,
            0x2020,
            0x2021,
            0x02C6,
            0x2030,
            0x0160,
            0x2039,
            0x0152,
            0x8d  ,
            0x017D,
            0x8f  ,
            0x90  ,
            0x2018,
            0x2019,
            0x201C,
            0x201D,
            0x2022,
            0x2013,
            0x2014,
            0x02DC,
            0x2122,
            0x0161,
            0x203A,
            0x0153,
            0x9d  ,
            0x017E,
            0x0178,
        };
        */

        public FON1Font(FileStream fs)
        {
            binaryReader = new BinaryReader(fs);
        }

        public override FontCharacterImage? GetBitmapFor(byte codePoint)
        {
            Bitmap bitmap = ConvertByteArray(Pixels[codePoint]);
            return new FontCharacterImage(bitmap);
        }

        public override Color[] GetPalette()
        {
            return Palette;
        }

        private Color GetColor(byte palIndex)
        {
            if (palIndex == 0 || palIndex >= Palette.Length) // Index 0 is transparent
            {
                return Color.Transparent;
            }
            else
            {
                return Palette[palIndex];
            }
        }

        public override FontCharacterImage? GetPalettedBitmapFor(byte codePoint)
        {
            Bitmap bitmap = ConvertByteArray(Pixels[codePoint], true);
            return new FontCharacterImage(bitmap);
        }

        private Bitmap ConvertByteArray(byte[] pixels, bool paletted = false)
        {
            Bitmap bitmap = new Bitmap((int)SpaceWidth, (int)FontHeight, paletted ? PixelFormat.Format8bppIndexed : PixelFormat.Format32bppArgb);
            if (paletted)
            {
                ColorPalette palette = bitmap.Palette;
                Palette.CopyTo(palette.Entries, 0);
                bitmap.Palette = palette;
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, (int)SpaceWidth, (int)FontHeight), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                for (int row = 0; row < FontHeight; row++)
                {
                    IntPtr rowPtr = data.Scan0 + row * data.Stride;
                    int rowOffset = row * (int)SpaceWidth;
                    Marshal.Copy(pixels, rowOffset, rowPtr, (int)SpaceWidth);
                }
            }
            else
            {
                uint Size = SpaceWidth * FontHeight;
                for (int pixel = 0; pixel < Size; pixel++)
                {
                    byte palIndex = pixels[pixel];
                    Color colour = GetColor(palIndex);
                    int x = pixel % (int)SpaceWidth;
                    int y = pixel / (int)SpaceWidth;
                    bitmap.SetPixel(x, y, colour);
                }
            }
            return bitmap;
        }

        public override bool IsFormat()
        {
            binaryReader.BaseStream.Position = 0;
            byte[] header = binaryReader.ReadBytes(4);
            return IsFon1Header(header);
        }

        private bool IsFon1Header(byte[] header)
        {
            int comparison = 0;
            for (int i = 0; i < header.Length; i++)
            {
                comparison += header[i] ^ Fon1header[i];
            }
            return comparison == 0;
        }

        public override void Read()
        {
            if (!IsFormat()) return;
            ReadHeader();
            InitPalette();
            ReadAllChars();
            Ready = true;
            binaryReader.Close();
        }

        private void ReadHeader()
        {
            // FON1 is monospaced
            SpaceWidth = binaryReader.ReadUInt16();
            FontHeight = binaryReader.ReadUInt16();
            GlobalKerning = 0;
        }

        private void InitPalette()
        {
            // FON1 fonts use a grayscale palette
            Palette = new Color[256];
            for (int c = 0; c < 256; c++)
            {
                Palette[c] = Color.FromArgb(c, c, c);
            }
        }

        private void ReadAllChars()
        {
            uint Size = SpaceWidth * FontHeight;
            Pixels = new byte[256][];
            // uint is required here to prevent overflows
            for (uint codePoint = 0; codePoint <= 255; codePoint++)
            {
                Pixels[codePoint] = new byte[Size];
                uint curPos = 0;
                while(curPos < Size)
                {
                    sbyte code;
                    try
                    {
                        code = binaryReader.ReadSByte();
                    }
                    catch (IOException except)
                    {
                        Console.WriteLine("No more data available.");
                        Console.WriteLine(except.StackTrace);
                        return;
                    }
                    // Based on studying GZDoom code
                    if (code >= 0)
                    {
                        // Read a given number of pixels
                        byte runLength = (byte)(code + 1);
                        for (byte run = 0; run < runLength; run++)
                        {
                            byte pixel = binaryReader.ReadByte();
                            Pixels[codePoint][curPos + run] = pixel;
                        }
                        curPos += runLength;
                    }
                    else if (code != -128)
                    {
                        // Read one colour, and repeat it for the run length
                        byte runLength = (byte)(-code + 1);
                        byte pixel = binaryReader.ReadByte();
                        for (byte run = 0; run < runLength; run++)
                        {
                            Pixels[codePoint][curPos + run] = pixel;
                        }
                        curPos += runLength;
                    }
                }
            }
        }

        public override string GetFontInfo()
        {
            return $"CellSize {SpaceWidth}, {FontHeight}\nTranslationType Console";
        }
    }
}
