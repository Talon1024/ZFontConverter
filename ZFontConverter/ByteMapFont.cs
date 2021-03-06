﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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

        public override FontCharacterImage? GetBitmapFor(byte codePoint)
        {
            bool available = Characters.TryGetValue(codePoint, out ByteMapFontCharacter character);
            if (available && character.Width > 0 && character.Height > 0)
            {
                Bitmap bitmap = new Bitmap(character.Width, character.Height);
                for (int i = 0; i < character.Data.Length; i++)
                {
                    int Column = i % character.Width;
                    int Row = i / character.Width;
                    bitmap.SetPixel(Column, Row, GetColor(character.Data[i]));
                }
                return new FontCharacterImage {
                    bitmap = bitmap,
                    xOffset = character.XOffset,
                    yOffset = character.YOffset,
                    xShift = character.Shift
                };
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
            binaryReader.Close();
            Ready = true;
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
            Palette[0] = Color.Transparent;
            for (int i = 1; i < PaletteSize; i++)
            {
                // 6-bit RGB
                byte r = VgaToRgb(binaryReader.ReadByte());
                byte g = VgaToRgb(binaryReader.ReadByte());
                byte b = VgaToRgb(binaryReader.ReadByte());
                Palette[i] = Color.FromArgb(r, g, b);
            }
            byte InfoLength = binaryReader.ReadByte(); // BMF files can have a comment. Read the length of the comment and skip it.
            binaryReader.BaseStream.Position += InfoLength;
            CharacterCount = binaryReader.ReadUInt16();
            HeaderSize = binaryReader.BaseStream.Position;
        }

        private byte VgaToRgb(byte compressed)
        {
            return (byte)(compressed * 255 / 63);
        }

        private void ReadAllCharacters()
        {
            for (int i = 0; i < CharacterCount; i++)
            {
                ByteMapFontCharacter curCharacter;
                curCharacter.ASCIICharacter = binaryReader.ReadByte();
                curCharacter.Width = binaryReader.ReadByte();
                curCharacter.Height = binaryReader.ReadByte();
                curCharacter.XOffset = binaryReader.ReadSByte();
                curCharacter.YOffset = binaryReader.ReadSByte();
                curCharacter.Shift = binaryReader.ReadByte();
                int Size = curCharacter.Width * curCharacter.Height;
                curCharacter.Data = binaryReader.ReadBytes(Size);

                if (curCharacter.ASCIICharacter == 32) // Space
                {
                    SpaceWidth = curCharacter.Shift;
                }
                else if (curCharacter.ASCIICharacter == 78 && SpaceWidth == 0) // Capital N
                {
                    SpaceWidth = curCharacter.Shift;
                }

                if (Characters.ContainsKey(curCharacter.ASCIICharacter))
                {
                    Characters.Remove(curCharacter.ASCIICharacter);
                }
                Characters.Add(curCharacter.ASCIICharacter, curCharacter);
            }
        }

        private Color GetColor(byte palIndex)
        {
            int index;
            // LargestUsedColour may not be set for all BMF fonts
            if (LargestUsedColour == 0)
            {
                index = palIndex;
            }
            else
            {
                index = Math.Min(palIndex, LargestUsedColour - 1);
            }
            // Ensure index stays within array bounds
            if(index > Palette.Length)
            {
                index = 0;
            }
            return Palette[index];
        }

        public override FontCharacterImage? GetPalettedBitmapFor(byte codePoint)
        {
            bool available = Characters.TryGetValue(codePoint, out ByteMapFontCharacter character);
            if (available && character.Width > 0 && character.Height > 0)
            {
                Bitmap bitmap = new Bitmap(character.Width, character.Height, PixelFormat.Format8bppIndexed);
                ColorPalette palette = bitmap.Palette;
                Palette.CopyTo(palette.Entries, 0);
                bitmap.Palette = palette;
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, character.Width, character.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                // Doing it this way ensures the data is copied correctly, regardless of how wide each character is.
                for (int row = 0; row < character.Height; row++)
                {
                    // The "stride" for a row may not be the same as the character width
                    IntPtr rowPtr = data.Scan0 + row * data.Stride;
                    // Since this code is copying row by row, calculate offset for each row
                    int rowOffset = row * character.Width;
                    Marshal.Copy(character.Data, rowOffset, rowPtr, character.Width);
                }
                bitmap.UnlockBits(data);
                return new FontCharacterImage
                {
                    bitmap = bitmap,
                    xOffset = character.XOffset,
                    yOffset = character.YOffset,
                    xShift = character.Shift
                };
            }
            return null;
        }

        public override Color[] GetPalette()
        {
            return Palette;
        }
    }
}
