﻿using System;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace Quest_Data_Builder.Core
{
    public class BetterBinaryReader : BetterReader
    {
        private static byte[] _buffer2Bytes = new byte[2];
        private static byte[] _buffer4Bytes = new byte[4];
        private static byte[] _buffer8Bytes = new byte[8];
        private BinaryReader _reader;
        public static Encoding Encoding = Encoding.ASCII;

        public override long Position
        {
            get { return _reader.BaseStream.Position; }
            set { _reader.BaseStream.Position = value; }
        }

        public override long Length
        {
            get { return _reader.BaseStream.Length; }
        }

        public BetterBinaryReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _reader.BaseStream.Seek(offset, origin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _reader.Read(buffer, offset, count);
        }

        public override char ReadChar()
        {
            return _reader.ReadChar();
        }

        public override byte ReadByte()
        {
            return _reader.ReadByte();
        }

        public override byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
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
            var bytes = _reader.ReadBytes(length);
            return Encoding.GetString(bytes);
        }

        public override string ReadNullTerminatedString(int length)
        {
            return ReadString(length).TrimEnd('\0');
        }

        public override string ReadNullTerminatedString()
        {
            string str = "";
            for (;;)
            {
                byte bt = _reader.ReadByte();
                if (bt == '\0')
                    break;

                var arr = new char[3];
                if (Encoding.TryGetChars(new byte[] { bt }, arr, out var count) && count > 0)
                {
                    str += arr[0];
                }
            }

            return str.TrimEnd('\0');
        }

        public override sbyte ReadSByte()
        {
            return _reader.ReadSByte();
        }

        protected override void Dispose(bool disposed)
        {
            if (!disposed)
                _reader.Dispose();
        }
    }
}
