using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
            Filename = Path.GetFileName(fs.Name);
            binaryReader = new BinaryReader(fs);
        }

        public override bool IsFormat()
        {
            binaryReader.BaseStream.Position = 0;
            byte[] header = binaryReader.ReadBytes(4);
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
                    if (code >= 0)
                    {
                        byte runLength = (byte)(code + 1);
                        // byte pixel = binaryReader.ReadByte();
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
                        for (int j = 0; j < runLength; j++)
                        {
                            vs[i][curPos + j] = theByte;
                        }
                        curPos += runLength;
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
                Palette[i] = Color.FromArgb(i == 0 ? 0 : 255, r, g, b);
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

        public override void Read()
        {
            if (!IsFormat()) return;
            ReadHeader();
            ReadPalette();
            ReadAllCharData();
            Ready = NumFontChars > 0;
            binaryReader.Close();
        }

        public override FontCharacterImage? GetBitmapFor(byte codePoint)
        {
            int charIndex = codePoint - FirstASCIIChar;
            if (charIndex < NumFontChars && charIndex >= 0 && CharWidths[charIndex] > 0)
            {
                ushort Width = CharWidths[charIndex];
                byte[] charData = AllCharData[charIndex]; // Palette references
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
            StringBuilder infoText = new StringBuilder(base.GetFontInfo());
            infoText.Append(Monospace ? GetMonospaceFontInfo() : GetVariableWidthFontInfo());
            return infoText.ToString();
        }

        public FontCharacterImage? GetPalettedBitmapFor(byte codePoint)
        {
            int charIndex = codePoint - FirstASCIIChar;
            if (charIndex < AllCharData.Length && charIndex >= 0 && CharWidths[charIndex] > 0)
            {
                ushort Width = CharWidths[charIndex];
                byte[] charData = AllCharData[charIndex]; // Palette references
                Bitmap bitmap = new Bitmap((int)Width, (int)FontHeight, PixelFormat.Format8bppIndexed);
                ColorPalette palette = bitmap.Palette;
                Palette.CopyTo(palette.Entries, 0);
                bitmap.Palette = palette;
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, Width, (int)FontHeight), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                for(int row = 0; row < FontHeight; row++)
                {
                    // The "stride" for a row may not be the same as the character width
                    IntPtr rowPtr = data.Scan0 + row * data.Stride;
                    // Since this code is copying row by row, calculate offset for each row
                    int rowOffset = row * Width;
                    Marshal.Copy(charData, rowOffset, rowPtr, Width);
                }
                bitmap.UnlockBits(data);
                return new FontCharacterImage(bitmap);
            }
            return null;
        }

        public override Color[] GetPalette()
        {
            return Palette;
        }

        private int CountBlankRows(byte[] charImage, int charWidth, bool above = true)
        {
            int start = above ? 0 : (int)FontHeight - 1;
            int direction = above ? 1 : -1;
            Func<int, bool> checkAbove = (int imageRow) => imageRow < FontHeight;
            Func<int, bool> checkBelow = (int imageRow) => imageRow >= 0;
            Func<int, bool> check = above ? checkAbove : checkBelow;
            int blankRows = 0;
            for (int imageRow = start; check(imageRow); imageRow += direction)
            {
                int pos = imageRow * charWidth;
                // If all bytes in this row are 0, add 1 to blankRowsAbove
                byte last = 0;
                for (int imageCol = 0; imageCol < charWidth; imageCol++)
                {
                    last = charImage[pos + imageCol];
                    if (last != 0)
                    {
                        return blankRows;
                    }
                }
                blankRows += 1;
            }
            return blankRows;
        }

        public override void Export(string fontCharDir, ApplyOffsetsCallback ApplyOffsets)
        {
            PixelFormat pixelFormat = PixelFormat.Format8bppIndexed;
            if (Monospace)
            {
                // Calculate sheet rows, columns, width, and height
                int charRows = (int)Math.Floor(Math.Sqrt(NumFontChars));
                int charCols = (int)Math.Ceiling((double)(NumFontChars / charRows));
                int charHeight = (int)FontHeight;
                int sheetWidth = charCols * CharWidths[0]; // Widths are the same for all characters
                int sheetHeight = charRows * (int)FontHeight;
                Bitmap fontSheet = new Bitmap(sheetWidth, sheetHeight, pixelFormat);
                Palette.CopyTo(fontSheet.Palette.Entries, 0);
                // Copy character graphics to the bitmap, row by row
                int curChar = -1;
                foreach (byte[] charImage in AllCharData)
                {
                    curChar += 1;
                    if (charImage == null)
                    {
                        continue;
                    }
                    int sheetCol = curChar % charCols;
                    int sheetRow = curChar / charCols;
                    Rectangle charRect = new Rectangle(CharWidths[0] * sheetCol, charHeight * sheetRow, CharWidths[0], charHeight);
                    BitmapData charPixels = fontSheet.LockBits(charRect, ImageLockMode.WriteOnly, pixelFormat);
                    for (int imageRow = 0; imageRow < FontHeight; imageRow++)
                    {
                        IntPtr sheetData = charPixels.Scan0 + imageRow * charPixels.Stride;
                        Marshal.Copy(charImage, imageRow * CharWidths[0], sheetData, CharWidths[0]);
                    }
                    fontSheet.UnlockBits(charPixels);
                }
                string sheetFileName = string.Format("{1}{0:X4}.png", FirstASCIIChar, fontCharDir);
                fontSheet.Save(sheetFileName);
                ApplyOffsets(sheetFileName, Palette, 0, 0);
            }
            else
            {
                int curChar = -1;
                foreach (byte[] charImage in AllCharData)
                {
                    curChar += 1;
                    if (charImage == null)
                    {
                        continue;
                    }
                    int charNumber = FirstASCIIChar + curChar;
                    int charWidth = CharWidths[curChar];
                    // Attempt to crop out blank rows above and below the image
                    int blankRowsAbove = CountBlankRows(charImage, charWidth);
                    if (blankRowsAbove == FontHeight)
                    {
                        // The image is completely blank!
                        continue;
                    }
                    int blankRowsBelow = CountBlankRows(charImage, charWidth, false);
                    int charHeight = (int)FontHeight - blankRowsAbove - blankRowsBelow;
                    int yOffset = blankRowsAbove != 0 ? -blankRowsAbove : 0;
                    Bitmap fontChar = new Bitmap(charWidth, charHeight, pixelFormat);
                    Palette.CopyTo(fontChar.Palette.Entries, 0);
                    Rectangle charRect = new Rectangle(0, 0, charWidth, charHeight);
                    BitmapData charPixels = fontChar.LockBits(charRect, ImageLockMode.WriteOnly, pixelFormat);
                    for (int charRow = 0, imageRow = blankRowsAbove; charRow < charHeight; charRow++, imageRow++)
                    {
                        IntPtr rowData = charPixels.Scan0 + charRow * charPixels.Stride;
                        Marshal.Copy(charImage, imageRow * charWidth, rowData, charWidth);
                    }
                    fontChar.UnlockBits(charPixels);
                    string charFileName = string.Format("{1}{0:X4}.png", charNumber, fontCharDir);
                    fontChar.Save(charFileName);
                    ApplyOffsets(charFileName, Palette, 0, yOffset);
                }
            }
            base.Export(fontCharDir, ApplyOffsets);
        }
    }
}
