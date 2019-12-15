using System;
using System.Text;
using System.Drawing;
using System.IO;

namespace ZFontConverter
{
    public class FON2Font : FontFormat
    {
        public static readonly byte[] Fon2header = Encoding.ASCII.GetBytes("FON2");
        private Color[] Palette;
        private byte FirstASCIIChar;
        private byte LastASCIIChar;
        private bool Monospace;
        private int NumFontChars;
        // private FileStream fileStream;
        private ushort[] CharWidths;
        private BinaryReader binaryReader;
        private long HeaderByteSize;
        private byte[][] AllCharData;

        public FON2Font(FileStream fs)
        {
            // fileStream = fs;
            binaryReader = new BinaryReader(fs);
        }

        public override bool IsFormat()
        {
            binaryReader.BaseStream.Position = 0;
            byte[] header = binaryReader.ReadBytes(4);
            return IsFon2Header(header);
        }

        private bool IsFon2Header(byte[] header)
        {
            int comparison = 0;
            for (int i = 0; i < header.Length; i++)
            {
                comparison += header[i] ^ Fon2header[i];
            }
            return comparison == 0;
        }

        private void ReadHeader()
        {
            if (!IsFormat()) { return; }
            FontHeight = binaryReader.ReadUInt16();
            FirstASCIIChar = binaryReader.ReadByte();
            LastASCIIChar = binaryReader.ReadByte();
            NumFontChars = LastASCIIChar - FirstASCIIChar + 1;
            Monospace = binaryReader.ReadBoolean();
            CharWidths = new ushort[NumFontChars];
            binaryReader.ReadByte(); // Shading type
            int PaletteSize = binaryReader.ReadByte() + 1;
            Palette = new Color[PaletteSize];
            int Flags = binaryReader.ReadByte();
            if ((Flags & 1) == 1)
            {
                GlobalKerning = binaryReader.ReadInt16();
            }
            else
            {
                GlobalKerning = 0;
            }
            if (!Monospace)
            {
                for (int i = 0; i < NumFontChars; i++)
                {
                    CharWidths[i] = binaryReader.ReadUInt16();
                }
            }
            else
            {
                ushort MonoWidth = binaryReader.ReadUInt16();
                for (int i = 0; i < NumFontChars; i++)
                {
                    CharWidths[i] = MonoWidth;
                }
            }
            if (FirstASCIIChar == 32) // Space
            {
                SpaceWidth = CharWidths[0];
            }
            else if (FirstASCIIChar < 32 && LastASCIIChar >= 32)
            {
                int charWidthIndex = 32 - FirstASCIIChar;
                SpaceWidth = CharWidths[charWidthIndex];
            }
            else if (FirstASCIIChar <= 78 && LastASCIIChar >= 78) // Capital N
            {
                // Copied from GZDoom source code
                SpaceWidth = (uint)((CharWidths['N' - FirstASCIIChar] + 1) / 2);
            }
            else
            {
                // Ported from GZDoom source code
                uint totalWidth = 0;
                foreach (byte width in CharWidths)
                {
                    totalWidth += width;
                }
                SpaceWidth = (uint)(totalWidth * 2 / (NumFontChars * 3));
            }
            HeaderByteSize = binaryReader.BaseStream.Position;
        }

        private byte[][] ReadAllCharData()
        {
            binaryReader.BaseStream.Position = HeaderByteSize + Palette.Length * 3;
            byte[][] vs = new byte[NumFontChars][];
            for (int i = 0; i < NumFontChars; i++)
            {
                ushort Width = CharWidths[i];
                int Size = (int)(Width * FontHeight);
                vs[i] = new byte[Size];
                int curPos = 0;
                while (curPos < Size)
                {
                    sbyte code = binaryReader.ReadSByte();
                    if (code > 0)
                    {
                        byte runLength = (byte)(code + 1);
                        for (int j = 0; j < runLength; j++)
                        {
                            vs[i][curPos + j] = binaryReader.ReadByte();
                        }
                        curPos += runLength;
                    }
                    else if (code != -128)
                    {
                        byte theByte = binaryReader.ReadByte();
                        byte runLength = (byte)(-code + 1);
                        byte[] bytes = DecodeRLE((byte)runLength, theByte);
                        Array.Copy(bytes, 0, vs[i], curPos, bytes.Length);
                        curPos += bytes.Length;
                    }
                }
            }
            AllCharData = vs;
            return vs;
        }

        private Color[] ReadPalette()
        {
            binaryReader.BaseStream.Position = HeaderByteSize;
            for (int i = 0; i < Palette.Length; i++)
            {
                // Colours are stored as RGB triples
                byte r = binaryReader.ReadByte();
                byte g = binaryReader.ReadByte();
                byte b = binaryReader.ReadByte();
                Palette[i] = Color.FromArgb(r, g, b);
            }
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

        private byte[] DecodeRLE(byte runLength, byte theByte)
        {
            byte[] decoded = new byte[runLength];
            // Array.Fill(decoded, theByte);
            for (int i = 0; i < runLength; i++)
            {
                decoded[i] = theByte;
            }
            return decoded;
        }

        public override void Read()
        {
            if (!IsFormat()) return;
            ReadHeader();
            ReadPalette();
            ReadAllCharData();
            Ready = true;
            binaryReader.Close();
        }

        public override FontCharacterImage? GetBitmapFor(byte character)
        {
            int charIndex = character - FirstASCIIChar;
            //Console.WriteLine($"Attempting to get bitmap for {character} ({(char)character})");
            //Console.WriteLine($"FirstASCIIChar {FirstASCIIChar} LastASCIIChar {LastASCIIChar} charIndex {charIndex}");
            if (charIndex < AllCharData.Length && charIndex >= 0 && CharWidths[charIndex] > 0)
            {
                byte[] charData = AllCharData[charIndex]; // Palette references
                ushort Width = CharWidths[charIndex];
                Bitmap bitmap = new Bitmap((int)Width, (int)FontHeight);
                for (int i = 0; i < charData.Length; i++)
                {
                    byte palIndex = charData[i];
                    Color colour = GetColor(palIndex);
                    int x = i % Width;
                    int y = i / Width;
                    bitmap.SetPixel(x, y, colour);
                }
                return new FontCharacterImage(bitmap);
            }
            return null;
        }

        public override string GetFontInfo()
        {
            StringBuilder FontInfo = new StringBuilder($"SpaceWidth {SpaceWidth}\n");
            if (Monospace)
            {
                FontInfo.Append($"CellSize {CharWidths[0]}, {FontHeight}\n");
            }
            else
            {
                FontInfo.Append($"FontHeight {FontHeight}\n");
            }
            if (GlobalKerning != 0)
            {
                FontInfo.Append($"Kerning {GlobalKerning}\n");
            }
            return FontInfo.ToString();
        }
    }
}
