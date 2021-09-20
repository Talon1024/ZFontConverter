using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;

namespace ZFontConverter
{
    // Used for inserting grAB chunk
    public struct PNGChunk
    {
        public uint Length;
        public string Type;
        public uint CRC;
        public byte[] Data;

        // Initialize from known data
        public PNGChunk(uint Length, string Type, uint CRC, byte[] data)
        {
            this.Length = Length;
            this.Type = Type;
            this.CRC = CRC;
            Data = data;
        }

        // Initialize from scratch
        public PNGChunk(string Type, byte[] data)
        {
            Length = (uint)data.Length;
            this.Type = Type;
            Data = data;
            CRC = CRCCalculator.CalculateCRC(Type, data);
        }

        // Initialize empty chunk
        public PNGChunk(string Type)
        {
            Length = 0;
            this.Type = Type;
            Data = new byte[0];
            CRC = CRCCalculator.CalculateCRC(Type, Data);
        }
    }
    public static class CRCCalculator
    {
        private static readonly uint initialCRC = 0xffffffff;
        private static readonly uint[] CRCTable;

        static CRCCalculator()
        {
            const uint crcConst = 0xedb88320;
            CRCTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint crcValue = (uint)i;
                for (int k = 0; k < 8; k++)
                {
                    crcValue = ((crcValue & 1) != 0) ? crcConst ^ (crcValue >> 1) : crcValue >> 1;
                }
                CRCTable[i] = crcValue;
            }
        }

        public static uint CalculateCRC(byte[] buffer)
        {
            uint CRC = initialCRC;
            foreach (byte theByte in buffer)
            {
                CRC = CRCTable[(byte)(CRC ^ theByte)] ^ (CRC >> 8);
            }
            CRC ^= initialCRC;
            return CRC;
        }
        public static uint CalculateCRC(string type, byte[] data)
        {
            byte[] typeAscii = Encoding.ASCII.GetBytes(type);
            byte[] concatd = new byte[data.Length + typeAscii.Length];
            Array.Copy(typeAscii, 0, concatd, 0, typeAscii.Length);
            Array.Copy(data, 0, concatd, typeAscii.Length, data.Length);
            return CalculateCRC(concatd);
        }
    }
    public class PNGFile
    {
        private LinkedList<PNGChunk> chunks;
        private readonly byte[] PNGHead = { 137, 80, 78, 71, 13, 10, 26, 10 };
        // private FileStream fileStream;
        private BinaryReader binaryReader;
        // private BinaryWriter binaryWriter;
        private LinkedListNode<PNGChunk> ihdrChunkNode;
        private LinkedListNode<PNGChunk> grabChunkNode;

        // New PNG file from scratch
        public PNGFile()
        {
            chunks = new LinkedList<PNGChunk>();
        }

        // Read existing PNG file
        public PNGFile(FileStream file)
        {
            // fileStream = file;
            chunks = new LinkedList<PNGChunk>();
            binaryReader = new BinaryReader(file);
            Read();
        }

        private uint ReadBigEndian()
        {
            byte[] bytes = binaryReader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt32(bytes, 0);
        }

        private void WriteBigEndian(Stream stream, uint number)
        {
            byte[] numBigEndian = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numBigEndian);
            }
            stream.Write(numBigEndian, 0, 4);
        }

        private void WriteBigEndian(Stream stream, int number)
        {
            byte[] numBigEndian = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(numBigEndian);
            }
            stream.Write(numBigEndian, 0, 4);
        }

        public void Open(FileStream file)
        {
            binaryReader = new BinaryReader(file);
            Read();
        }

        public bool Read()
        {
            byte[] header = binaryReader.ReadBytes(8);
            int comparison = 0;
            for (int i = 0; i < PNGHead.Length; i++)
            {
                comparison += PNGHead[i] ^ header[i];
            }
            if (comparison > 0)
            {
                return false;
            }
            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length) {
                uint length = ReadBigEndian();
                string type = Encoding.ASCII.GetString(binaryReader.ReadBytes(4));
                byte[] data;
                if (length > 0)
                {
                    data = binaryReader.ReadBytes((int)length);
                }
                else
                { 
                    data = new byte[] { };
                }
                uint crc = ReadBigEndian();
                PNGChunk chunk = new PNGChunk(length, type, crc, data);
                chunks.AddLast(chunk);
                if (type == "IHDR")
                {
                    ihdrChunkNode = chunks.Last;
                }
            }
            binaryReader.Close();
            return true;
        }

        public void InsertHeader(uint width, uint height, byte depth = 8, byte colourType = 3, byte compression = 0, byte filter = 0, byte interlace = 0)
        {
            string ihdrType = "IHDR";
            byte[] data = new byte[13];
            MemoryStream stream = new MemoryStream(data);
            WriteBigEndian(stream, width);
            WriteBigEndian(stream, height);
            stream.WriteByte(depth);
            stream.WriteByte(colourType);
            stream.WriteByte(compression);
            stream.WriteByte(filter);
            stream.WriteByte(interlace);
            stream.Close();
            PNGChunk ihdrChunk = new PNGChunk(ihdrType, data);
            ihdrChunkNode = chunks.AddFirst(ihdrChunk);
        }

        public void InsertGrabChunk(int xOffset, int yOffset)
        {
            byte[] grabData = new byte[8];
            MemoryStream stream = new MemoryStream(grabData);
            WriteBigEndian(stream, xOffset);
            WriteBigEndian(stream, yOffset);
            stream.Close();
            PNGChunk grabChunk = new PNGChunk("grAb", grabData);
            if (grabChunkNode == null)
            {
                grabChunkNode = chunks.AddAfter(ihdrChunkNode, grabChunk);
            }
            else
            {
                grabChunkNode.Value = grabChunk;
            }
        }

        public void InsertPalette(Color[] Palette, byte transparentColour = 0, LinkedListNode<PNGChunk> after = null)
        {
            byte[] plteData = new byte[Palette.Length * 3];
            MemoryStream palStream = new MemoryStream(plteData);
            MemoryStream transStream = new MemoryStream(256);
            foreach (Color colour in Palette)
            {
                palStream.WriteByte(colour.R);
                palStream.WriteByte(colour.G);
                palStream.WriteByte(colour.B);
            }
            palStream.Close();
            for (int i = 0; i < Palette.Length; i++)
            {
                if (i == transparentColour)
                {
                    transStream.WriteByte(0); // Transparent
                    break;
                }
                transStream.WriteByte(255); // Opaque
            }
            byte[] trnsData = transStream.ToArray();
            transStream.Close();
            PNGChunk plteChunk = new PNGChunk("PLTE", plteData);
            PNGChunk trnsChunk = new PNGChunk("tRNS", trnsData);
            if (after != null)
            {
                chunks.AddAfter(after, trnsChunk); // PLTE will go first
                chunks.AddAfter(after, plteChunk);
            }
            else
            {
                chunks.AddLast(plteChunk);
                chunks.AddLast(trnsChunk);
            }
        }

        public void ReplacePalette(Color[] Palette, byte transparentColour = 0)
        {
            LinkedListNode<PNGChunk> prevChunk = chunks.First; // Insert new chunks after this one
            foreach (var chunk in chunks)
            {
                if (chunk.Type == "PLTE")
                {
                    LinkedListNode<PNGChunk> palChunk = chunks.Find(chunk);
                    prevChunk = palChunk.Previous;
                    chunks.Remove(palChunk);
                    break;
                }
            }
            foreach (var chunk in chunks)
            {
                if (chunk.Type == "tRNS")
                {
                    LinkedListNode<PNGChunk> transChunk = chunks.Find(chunk);
                    chunks.Remove(transChunk);
                    break;
                }
            }
            InsertPalette(Palette, transparentColour, prevChunk);
        }

        public void InsertTransparency(byte transparentColour = 0)
        {
            LinkedListNode<PNGChunk> palChunk;
            foreach (var chunk in chunks)
            {
                if(chunk.Type == "PLTE")
                {
                    palChunk = chunks.Find(chunk);
                    uint palLength = chunk.Length / 3;
                    byte[] transData = new byte[palLength];
                    for(int i = 0; i < palLength; i++)
                    {
                        if(i == transparentColour)
                        {
                            transData[i] = 0;
                        }
                        else
                        {
                            transData[i] = 255;
                        }
                    }
                    PNGChunk transChunk = new PNGChunk("tRNS", transData);
                    chunks.AddAfter(palChunk, transChunk);
                    break;
                }
            }
        }

        public void InsertData(uint width, uint height, byte[] data)
        {
            int bufferSize = (int)((width + 1) * height);
            MemoryStream idatRawStream = new MemoryStream(bufferSize);
            for(uint row = 0; row < height; row++)
            {
                // Filter type byte
                idatRawStream.WriteByte(0);
                // Assuming data is row-major indices
                idatRawStream.Write(data, (int)(row * width), (int)width);
            }
            MemoryStream idatStream = new MemoryStream(4);
            using (DeflateStream compressStream = new DeflateStream(idatStream, CompressionLevel.NoCompression))
            {
                byte[] rawData = idatRawStream.GetBuffer();
                compressStream.Write(rawData, 0, rawData.Length);
            }
            byte[] idatData = idatStream.GetBuffer();
            Console.WriteLine($"Compressed {bufferSize} bytes to {idatData.Length} bytes.");
            PNGChunk idatChunk = new PNGChunk("IDAT", idatData);
            chunks.AddLast(idatChunk);
        }

        public void InsertEnd()
        {
            PNGChunk endChunk = new PNGChunk("IEND");
            chunks.AddLast(endChunk);
        }

        private void WriteChunk(Stream stream, PNGChunk chunk)
        {
            WriteBigEndian(stream, chunk.Length);
            byte[] typeBytes = Encoding.ASCII.GetBytes(chunk.Type);
            stream.Write(typeBytes, 0, 4);
            if (chunk.Length > 0)
            {
                stream.Write(chunk.Data, 0, (int)chunk.Length);
            }
            WriteBigEndian(stream, chunk.CRC);
        }

        public bool Write(string fname)
        {
            try
            {
                FileStream writeStream = File.Open(fname, FileMode.OpenOrCreate, FileAccess.Write);
                Write(writeStream);
                writeStream.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cannot write {fname} for some reason: {ex}");
                return false;
            }
            return true;
        }

        public void Write(Stream stream)
        {
            stream.Write(PNGHead, 0, PNGHead.Length);
            foreach(var chunk in chunks)
            {
                WriteChunk(stream, chunk);
            }
        }
    }
}
