using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace ZFontConverter.Doom
{
    public class WADFont : FontFormat
    {
        enum FontType
        {
            Template,
            ByChar,
        }

        private Dictionary<byte, DoomWADLump> CharacterLumps;
        public DoomWAD FontWad;
        public DoomWAD PaletteWad;

        public WADFont(DoomWAD FontWad, DoomWAD PaletteWad = null)
        {
            this.FontWad = FontWad;
            this.PaletteWad = PaletteWad;
        }

        public override FontCharacterImage? GetBitmapFor(byte character)
        {
            if (CharacterLumps.TryGetValue(character, out DoomWADLump charPic))
            {

            }
            return null;
        }

        public override bool IsFormat()
        {
            // Created by DoomWAD
            return true;
        }

        public override void Read()
        {
            throw new NotImplementedException();
        }

        private void GetFontDefs()
        {
            byte[] FontDefData = FontWad.GetLumpDataFor("FONTDEFS");
        }

        public override Color[] GetPalette()
        {

            byte[] playpal = FontWad.GetLumpDataFor("PLAYPAL");
            if (playpal == null)
            {
                // Try the palette WAD
                playpal = PaletteWad.GetLumpDataFor("PLAYPAL");
            }
            if (playpal != null)
            {
                // Palette is available
                Color[] palette = new Color[256];
                byte r, g, b;
                for (int palIndex = 0; palIndex < 256; palIndex++)
                {
                    // One colour at a time
                    r = playpal[palIndex * 3];
                    g = playpal[palIndex * 3 + 1];
                    b = playpal[palIndex * 3 + 2];
                    palette[palIndex] = Color.FromArgb(r, g, b);
                }
                return palette;
            }
            // Palette unavailable
            return null;
        }

        private bool LumpIsPicture(string lumpName)
        {
            byte[] lumpData = FontWad.GetLumpDataFor(lumpName);
            MemoryStream lumpStream = new MemoryStream(lumpData);
            bool NotImage = false;
            try
            {
                Bitmap bitmap = new Bitmap(lumpStream);
            }
            catch (ArgumentException ex)
            {
                // Assume it is a Doom picture, since .NET supports most image
                // formats ZDoom supports out of the box, except for Doom pictures
                Console.WriteLine($"{lumpName} is a Doom picture");
                NotImage = true;
            }
            return NotImage;
        }

        /*
        public override FontCharacterImage? GetPalettedBitmapFor(byte codePoint)
        {
            throw new NotImplementedException();
        }
        */
    }
}
