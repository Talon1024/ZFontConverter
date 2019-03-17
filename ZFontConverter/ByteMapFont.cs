using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ZFontConverter
{
    struct ByteMapFontCharacter
    {
        public byte ASCIICharacter;
        public byte Width;
        public byte Height;
        public sbyte XOffset;
        public sbyte YOffset;
        public byte Shift;
        public byte[] Data;
    }

    public class ByteMapFont : FontFormat
    {
        public static readonly byte[] BmfHeader = { 0xE1, 0xE6, 0xD5, 0x1A };
        private BinaryReader binaryReader;
        private byte Version;
        //private sbyte HeightAboveBaseLine;
        //private sbyte HeightUnderBaseLine;
        //private sbyte InnerSize;
        private byte UsedColours;
        private byte LargestUsedColour;
        private Color[] Palette;
        private ushort CharacterCount;
        private Dictionary<byte, ByteMapFontCharacter> Characters;
        private long HeaderSize;

        public ByteMapFont(FileStream fs)
        {
            binaryReader = new BinaryReader(fs);
            Characters = new Dictionary<byte, ByteMapFontCharacter>(128);
        }

        public override FontCharacterImage? GetBitmapFor(byte character)
        {
            bool available = Characters.TryGetValue(character, out ByteMapFontCharacter fontCharacter);
            if (available && fontCharacter.Width > 0 && fontCharacter.Height > 0)
            {
                Bitmap bitmap = new Bitmap(fontCharacter.Width, fontCharacter.Height);
                for (int i = 0; i < fontCharacter.Data.Length; i++)
                {
                    int Column = i % fontCharacter.Width;
                    int Row = i / fontCharacter.Width;
                    bitmap.SetPixel(Column, Row, GetColor(fontCharacter.Data[i]));
                }
                return new FontCharacterImage { bitmap = bitmap, xOffset = fontCharacter.XOffset, yOffset = fontCharacter.YOffset };
            }
            return null;
        }

        public override bool IsFormat()
        {
            binaryReader.BaseStream.Position = 0;
            byte[] header = binaryReader.ReadBytes(4);
            int comparison = 0;
            for (int i = 0; i < header.Length; i++)
            {
                comparison += header[i] ^ BmfHeader[i];
            }
            return comparison == 0;
        }

        public override void Read()
        {
            if (!IsFormat()) return;
            ReadHeader();
            ReadAllCharacters();
            if (SpaceWidth == 0)
            {
                // Ported from GZDoom source code
                uint totalWidth = 0;
                foreach (ByteMapFontCharacter fontCharacter in Characters.Values)
                {
                    totalWidth += fontCharacter.Width;
                }
                SpaceWidth = (uint)(totalWidth * 2 / (Characters.Count * 3));
            }
        }

        private void ReadHeader()
        {
            Version = binaryReader.ReadByte(); // Position: 4
            FontHeight = binaryReader.ReadByte();
            //HeightAboveBaseLine = binaryReader.ReadSByte();
            //HeightUnderBaseLine = binaryReader.ReadSByte();
            binaryReader.BaseStream.Position += 2; // Skip useless info
            GlobalKerning = binaryReader.ReadSByte();
            //InnerSize = binaryReader.ReadSByte();
            binaryReader.BaseStream.Position += 1; // Skip "inner size"
            UsedColours = binaryReader.ReadByte();
            LargestUsedColour = binaryReader.ReadByte();
            binaryReader.BaseStream.Position += 4; // Skip reserved values
            byte PaletteSize = (byte)(binaryReader.ReadByte() + 1);
            Palette = new Color[PaletteSize];
            for (int i = 1; i < PaletteSize; i++)
            {
                // 6-bit RGB
                byte r = DecompressComponent(binaryReader.ReadByte());
                byte g = DecompressComponent(binaryReader.ReadByte());
                byte b = DecompressComponent(binaryReader.ReadByte());
                Palette[i] = Color.FromArgb(r, g, b);
            }
            byte InfoLength = binaryReader.ReadByte(); // Contrary to what ZDoom Wiki says, this is NOT the list of ASCII characters in the font, but rather a comment
            binaryReader.BaseStream.Position += InfoLength; // Skip comment
            CharacterCount = binaryReader.ReadUInt16();
            HeaderSize = binaryReader.BaseStream.Position;
        }

        private byte DecompressComponent(byte compressed)
        {
            return (byte)(compressed << 2 | compressed >> 4);
        }

        private void ReadAllCharacters()
        {
            for (int i = 0; i < CharacterCount; i++)
            {
                ByteMapFontCharacter curCharacter;
                curCharacter.ASCIICharacter = binaryReader.ReadByte();
                curCharacter.Width = binaryReader.ReadByte();
                if (curCharacter.ASCIICharacter == 32) // Space
                {
                    SpaceWidth = curCharacter.Width;
                }
                else if (curCharacter.ASCIICharacter == 78 && SpaceWidth == 0) // Capital N
                {
                    SpaceWidth = curCharacter.Width;
                }
                curCharacter.Height = binaryReader.ReadByte();
                curCharacter.XOffset = binaryReader.ReadSByte();
                curCharacter.YOffset = binaryReader.ReadSByte();
                curCharacter.Shift = binaryReader.ReadByte();
                int Size = curCharacter.Width * curCharacter.Height;
                curCharacter.Data = binaryReader.ReadBytes(Size);
                if (Characters.ContainsKey(curCharacter.ASCIICharacter))
                {
                    Characters.Remove(curCharacter.ASCIICharacter);
                }
                Characters.Add(curCharacter.ASCIICharacter, curCharacter);
            }
        }

        private Color GetColor(byte palIndex)
        {
            return Palette[Math.Min(palIndex, LargestUsedColour - 1)];
        }
    }
}
