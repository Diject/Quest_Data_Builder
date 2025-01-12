﻿using System;
using System.IO;
using System.Text;

namespace Quest_Data_Builder.Core
{
    public class BetterMemoryReader : BetterReader
    {
        private static byte[] _buffer2Bytes = new byte[2];
        private static byte[] _buffer4Bytes = new byte[4];
        private static byte[] _buffer8Bytes = new byte[8];
        private MemoryStream _reader;

        public override long Position
        {
            get { return _reader.Position; }
            set { _reader.Position = value; }
        }

        public override long Length
        {
            get { return _reader.Length; }
        }

        public BetterMemoryReader(byte[] array)
        {
            _reader = new MemoryStream(array);
        }

        public BetterMemoryReader(Stream stream)
        {
            _reader = new MemoryStream();
            stream.CopyTo(_reader);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _reader.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _reader.Read(buffer, offset, count);
        }

        public override char ReadChar()
        {
            return Convert.ToChar(_reader.ReadByte());
        }

        public override byte ReadByte()
        {
            return (byte)_reader.ReadByte();
        }

        public override byte[] ReadBytes(int count)
        {
            var data = new byte[count];
            _reader.Read(data, 0, count);
            return data;
        }

        public override bool ReadBool32()
        {
            return ReadUInt32() != 0;
        }

        public override ushort ReadUInt16()
        {
            _reader.Read(_buffer2Bytes, 0, 2);

            return (ushort)((_buffer2Bytes[1] << 8) | _buffer2Bytes[0]);
        }

        public override uint ReadUInt32()
        {
            _reader.Read(_buffer4Bytes, 0, 4);

            return ((uint)_buffer4Bytes[3] << 24) | ((uint)_buffer4Bytes[2] << 16) | ((uint)_buffer4Bytes[1] << 8) | _buffer4Bytes[0];
        }

        public override ulong ReadUInt64()
        {
            _reader.Read(_buffer8Bytes, 0, 8);

            return ((ulong)_buffer8Bytes[7] << 56) | ((ulong)_buffer8Bytes[6] << 48) | ((ulong)_buffer8Bytes[5] << 40) | ((ulong)_buffer8Bytes[4] << 32) | ((ulong)_buffer8Bytes[3] << 24) | ((ulong)_buffer8Bytes[2] << 16) | ((ulong)_buffer8Bytes[1] << 8) | _buffer8Bytes[0];
        }

        public override short ReadInt16()
        {
            _reader.Read(_buffer2Bytes, 0, 2);

            return BitConverter.ToInt16(_buffer2Bytes, 0);
        }

        public override int ReadInt32()
        {
            _reader.Read(_buffer4Bytes, 0, 4);

            return BitConverter.ToInt32(_buffer4Bytes, 0);
        }

        public override long ReadInt64()
        {
            _reader.Read(_buffer8Bytes, 0, 8);

            return BitConverter.ToInt32(_buffer8Bytes, 0);
        }

        public override float ReadSingle()
        {
            _reader.Read(_buffer4Bytes, 0, 4);

            return BitConverter.ToSingle(_buffer4Bytes, 0);
        }

        public override double ReadDouble()
        {
            _reader.Read(_buffer8Bytes, 0, 8);

            return BitConverter.ToDouble(_buffer8Bytes, 0);
        }

        public override string ReadString(int length)
        {
            var temp = new byte[length];
            _reader.Read(temp, 0, length);
            return Encoding.ASCII.GetString(temp);
        }

        public override string ReadNullTerminatedString(int length)
        {
            return ReadString(length).TrimEnd('\0');
        }

        public override string ReadNullTerminatedString()
        {
            int bt;
            bool endFound = false;
            string str = "";
            do
            {
                bt = _reader.ReadByte();
                if (bt == '\0')
                    endFound = true;
                if (!endFound)
                    str += Convert.ToChar(bt);
            } while (!(endFound && bt != '\0'));

            _reader.Position--;

            return str.TrimEnd('\0');
        }

        public override sbyte ReadSByte()
        {
            return (sbyte)_reader.ReadByte();
        }

        protected override void Dispose(bool disposed)
        {
            if (!disposed)
                _reader.Dispose();
        }
    }
}
