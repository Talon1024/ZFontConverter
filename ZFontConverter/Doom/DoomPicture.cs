using System.IO;
using System.Drawing;
using System.Collections.Generic;
namespace ZFontConverter.Doom
{
    public struct DoomPicturePost
    {
        public ushort YOffset; // Absolute
        public byte[] Pixels;
    }
    public struct DoomPictureColumn
    {
        public ushort XOffset;
        public List<DoomPicturePost> Posts;
    }
    public class DoomPicture
    {
        private ushort Width;
        private ushort Height;
        private ushort XOffset;
        private ushort YOffset;
        private uint[] ColumnOffsets;
        private BinaryReader binaryReader;
        private bool ready;
        public Color[] Palette;
        private DoomPictureColumn[] columns;

        public DoomPicture(byte[] pictureData)
        {
            binaryReader = new BinaryReader(new MemoryStream(pictureData, false));
            Palette = new Color[256];
            ready = false;
        }

        public void Read()
        {
            Width = binaryReader.ReadUInt16();
            Height = binaryReader.ReadUInt16();
            XOffset = binaryReader.ReadUInt16();
            YOffset = binaryReader.ReadUInt16();
            ColumnOffsets = new uint[Width]; // Offsets within the file
            columns = new DoomPictureColumn[Width];
            for (int i = 0; i < Width; i++)
            {
                ColumnOffsets[i] = binaryReader.ReadUInt32();
            }
            for (int col = 0; col < Width; col++)
            {
                binaryReader.BaseStream.Position = ColumnOffsets[col];
                columns[col].XOffset = (ushort)col;
                byte Topdelta = binaryReader.ReadByte();
                byte PrevTopdelta = Topdelta;
                while (Topdelta < 255) // 255 = end of column
                {
                    int postOffset = Topdelta;
                    if (Topdelta < PrevTopdelta)
                    {
                        // Account for DeePSea tall patches
                        postOffset = PrevTopdelta + Topdelta;
                    }
                    DoomPicturePost post = new DoomPicturePost
                    {
                        YOffset = (ushort)postOffset
                    };
                    int PostHeight = binaryReader.ReadByte();
                    binaryReader.BaseStream.Position += 1; // dummy byte
                    post.Pixels = binaryReader.ReadBytes(PostHeight);
                    columns[col].Posts.Add(post);
                    PrevTopdelta = Topdelta;
                    Topdelta = binaryReader.ReadByte();
                }
            }
            ready = true;
        }

        public Bitmap GetBitmap()
        {
            if (!ready) return null;
            Bitmap pictureBitmap = new Bitmap(Width, Height);
            foreach (DoomPictureColumn col in columns)
            {
                foreach (DoomPicturePost post in col.Posts)
                {
                    for (int pixIndex = 0; pixIndex < post.Pixels.Length; pixIndex++)
                    {
                        int pixY = post.YOffset + pixIndex;
                        int palIndex = post.Pixels[pixIndex];
                        pictureBitmap.SetPixel(col.XOffset, pixY, Palette[palIndex]);
                    }
                }
            }
            return pictureBitmap;
        }
    }
}
