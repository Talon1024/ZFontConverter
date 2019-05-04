using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq; // map = Select, filter = Where
namespace ZFontConverter.Doom
{
    public struct DoomWADLump
    {
        public string Name;
        // public byte[] data; // Just in case we want to read 
        public uint Offset;
        public uint Size;
    }
    public class DoomWAD
    {
        public Dictionary<string, DoomWADLump> lumps;
        public string Filename { get; private set; }
        private BinaryReader binaryReader;

        public DoomWAD(FileStream fs)
        {
            Filename = fs.Name;
            binaryReader = new BinaryReader(fs);
            lumps = new Dictionary<string, DoomWADLump>();
        }

        public bool IsFormat()
        {
            // Ensure it is really a WAD
            binaryReader.BaseStream.Position = 0;
            byte[] header = binaryReader.ReadBytes(4);
            string headStr = Encoding.ASCII.GetString(header);
            return headStr == "IWAD" || headStr == "PWAD";
        }

        public void Read()
        {
            // After IWAD/PWAD header
            binaryReader.BaseStream.Position = 4;
            uint LumpCount = binaryReader.ReadUInt32();
            uint LumpsOffset = binaryReader.ReadUInt32();

            binaryReader.BaseStream.Position = LumpsOffset;
            for (int i = 0; i < LumpCount; i++)
            {
                // Read lump info
                DoomWADLump lump = new DoomWADLump
                {
                    Offset = binaryReader.ReadUInt32(),
                    Size = binaryReader.ReadUInt32()
                };
                // Convert lump name to string
                string lumpName = Encoding.ASCII.GetString(
                    binaryReader.ReadBytes(8).Where((arg) => arg != 0).ToArray()
                    );
                lump.Name = lumpName;
                // Some WADs have two or more lumps with the same name.
                // Use the new lump if it replaces another
                if (lumps.ContainsKey(lumpName))
                {
                    lumps.Remove(lumpName);
                    lumps.Add(lumpName, lump);
                }
            }
        }

        public byte[] GetLumpDataFor(string lumpName)
        {
            if (!lumps.ContainsKey(lumpName))
            {
                // Specified lump is not in this WAD
                return null;
            }
            if (lumps.TryGetValue(lumpName, out DoomWADLump lump))
            {
                // Read lump data
                binaryReader.BaseStream.Position = lump.Offset;
                return binaryReader.ReadBytes((int)lump.Size);
            }
            return null;
        }

        public void Close()
        {
            // Close WAD file. It remains open while the fonts are being read from it.
            binaryReader.Close();
        }
    }
}
