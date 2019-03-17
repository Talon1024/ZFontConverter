using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ZFontConverter
{
    // Used for inserting grAB chunk
    public struct PNGChunk
    {
        public uint Length;
        public string Type;
        public uint CRC;
        public byte[] Data;

        public PNGChunk(uint Length, string Type, uint CRC, byte[] data)
        {
            this.Length = Length;
            this.Type = Type;
            this.CRC = CRC;
            Data = data;
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
                    crcValue = ((crcValue & 1) > 0) ? crcConst ^ (crcValue >> 1) : crcValue >> 1;
                }
                CRCTable[i] = crcValue;
            }
        }

        public static uint CalculateCRC(byte[] data)
        {
            uint CRC = initialCRC;
            for (int i = 0; i < data.Length; i++)
            {
                CRC = CRCTable[(byte)(CRC ^ data[i] & 0xff)] ^ (CRC >> 8);
            }
            return CRC;
        }
        public static uint CalculateCRC(string type, byte[] data)
        {
            uint CRC = initialCRC;
            byte[] typeAscii = Encoding.ASCII.GetBytes(type);
            byte[] concatd = new byte[data.Length + typeAscii.Length];
            Array.Copy(typeAscii, 0, concatd, 0, typeAscii.Length);
            Array.Copy(data, 0, concatd, typeAscii.Length, data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                CRC = CRCTable[(byte)(CRC ^ concatd[i] & 0xff)] ^ (CRC >> 8);
            }
            return CRC;
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

        public PNGFile()
        {
            chunks = new LinkedList<PNGChunk>();
        }

        public PNGFile(FileStream file)
        {
            // fileStream = file;
            chunks = new LinkedList<PNGChunk>();
            binaryReader = new BinaryReader(file);
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

        public void Open(FileStream file)
        {
            binaryReader = new BinaryReader(file);
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
            return true;
        }

        public void InsertGrabChunk(int xOffset, int yOffset)
        {
            string grabType = "grAB";
            byte[] grabTypeAscii = Encoding.ASCII.GetBytes(grabType);
            byte[] grabData = new byte[8];
            byte[] grabX = BitConverter.GetBytes(xOffset);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(grabX);
            }
            Array.Copy(grabX, 0, grabData, 0, 4);
            byte[] grabY = BitConverter.GetBytes(yOffset);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(grabY);
            }
            Array.Copy(grabY, 0, grabData, 4, 4);
            uint crc = CRCCalculator.CalculateCRC(grabType, grabData);
            PNGChunk grabChunk = new PNGChunk((uint)grabData.Length, grabType, crc, grabData);
            if (grabChunkNode == null)
            {
                grabChunkNode = chunks.AddAfter(ihdrChunkNode, grabChunk);
            }
            else
            {
                grabChunkNode.Value = grabChunk;
            }
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
                writeStream.Write(PNGHead, 0, PNGHead.Length);
                foreach (var chunk in chunks)
                {
                    WriteChunk(writeStream, chunk);
                }
                writeStream.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cannot write {fname} for some reason: {ex}");
                return false;
            }
            return true;
        }
    }
}
