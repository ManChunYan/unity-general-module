using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace General.Module
{
    public class DataPack
    {
        private readonly ArraySegment<byte> _buffer;
        private readonly List<byte> _data = new List<byte>();
        private int _position;

        public int OffSet => IsWriting ? _data.Count : _position;
        public int Count => IsWriting ? _data.Count : _buffer.Count;
        public int Remaining => Count - OffSet;
        public byte[] GetBuffer
        {
            get
            {
                if (IsWriting)
                    return _data.ToArray();

                if (_buffer.Count == 0)
                    return Array.Empty<byte>();

                if (_buffer.Offset == 0 && _buffer.Array.Length == _buffer.Count)
                    return _buffer.Array;

                var bytes = new byte[_buffer.Count];
                Buffer.BlockCopy(_buffer.Array, _buffer.Offset, bytes, 0, _buffer.Count);
                return bytes;
            }
        }

        private bool IsWriting => _buffer.Array == null;

        public DataPack()
        {
        }

        public DataPack(byte[] data)
        {
            _buffer = new ArraySegment<byte>(data ?? Array.Empty<byte>());
        }

        public int Write(bool value) { return Write(value ? (byte)1 : (byte)0); }
        public int Write(sbyte value) { return Write((byte)value); }
        public int Write(short value) { return Write((ushort)value); }
        public int Write(int value) { return Write((uint)value); }
        public int Write(long value) { return Write((ulong)value); }
        public int Write(double value) { return Write((ulong)BitConverter.DoubleToInt64Bits(value)); }

        public int Write(byte value)
        {
            EnsureWriting();
            _data.Add(value);
            return sizeof(byte);
        }

        public int Write(ushort value)
        {
            EnsureWriting();
            _data.Add((byte)value);
            _data.Add((byte)(value >> 8));
            return sizeof(ushort);
        }

        public int Write(uint value)
        {
            EnsureWriting();
            _data.Add((byte)value);
            _data.Add((byte)(value >> 8));
            _data.Add((byte)(value >> 16));
            _data.Add((byte)(value >> 24));
            return sizeof(uint);
        }

        public int Write(ulong value)
        {
            EnsureWriting();
            _data.Add((byte)value);
            _data.Add((byte)(value >> 8));
            _data.Add((byte)(value >> 16));
            _data.Add((byte)(value >> 24));
            _data.Add((byte)(value >> 32));
            _data.Add((byte)(value >> 40));
            _data.Add((byte)(value >> 48));
            _data.Add((byte)(value >> 56));
            return sizeof(ulong);
        }

        public int Write(byte[] value)
        {
            EnsureWriting();

            if (value == null || value.Length == 0)
                return 0;

            _data.AddRange(value);
            return value.Length;
        }

        public int Write(string value)
        {
            var bytes = string.IsNullOrEmpty(value) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(value);
            Write(bytes.Length);
            Write(bytes);
            return sizeof(int) + bytes.Length;
        }

        public bool ReadBool() { return ReadUInt8() != 0; }
        public sbyte ReadInt8() { return (sbyte)ReadUInt8(); }
        public short ReadInt16() { return (short)ReadUInt16(); }
        public int ReadInt32() { return (int)ReadUInt32(); }
        public long ReadInt64() { return (long)ReadUInt64(); }
        public double ReadDouble() { return BitConverter.Int64BitsToDouble(ReadInt64()); }

        public byte ReadUInt8()
        {
            EnsureReadable(sizeof(byte));
            return _buffer.Array[_buffer.Offset + _position++];
        }

        public ushort ReadUInt16()
        {
            EnsureReadable(sizeof(ushort));
            var index = _buffer.Offset + _position;
            var value = (ushort)(_buffer.Array[index] | (_buffer.Array[index + 1] << 8));
            _position += sizeof(ushort);
            return value;
        }

        public uint ReadUInt32()
        {
            EnsureReadable(sizeof(uint));
            var index = _buffer.Offset + _position;
            var value = (uint)(_buffer.Array[index]
                | (_buffer.Array[index + 1] << 8)
                | (_buffer.Array[index + 2] << 16)
                | (_buffer.Array[index + 3] << 24));
            _position += sizeof(uint);
            return value;
        }

        public ulong ReadUInt64()
        {
            EnsureReadable(sizeof(ulong));
            var index = _buffer.Offset + _position;
            var value = (ulong)_buffer.Array[index]
                | ((ulong)_buffer.Array[index + 1] << 8)
                | ((ulong)_buffer.Array[index + 2] << 16)
                | ((ulong)_buffer.Array[index + 3] << 24)
                | ((ulong)_buffer.Array[index + 4] << 32)
                | ((ulong)_buffer.Array[index + 5] << 40)
                | ((ulong)_buffer.Array[index + 6] << 48)
                | ((ulong)_buffer.Array[index + 7] << 56);
            _position += sizeof(ulong);
            return value;
        }

        public byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Read length cannot be negative.");

            EnsureReadable(length);

            var bytes = new byte[length];
            Buffer.BlockCopy(_buffer.Array, _buffer.Offset + _position, bytes, 0, length);
            _position += length;
            return bytes;
        }

        public string ReadString()
        {
            var length = ReadInt32();
            return length == 0 ? string.Empty : Encoding.UTF8.GetString(ReadBytes(length));
        }

        public void Skip(int length)
        {
            EnsureReadable(length);
            _position += length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWriting()
        {
            if (!IsWriting)
                throw new InvalidOperationException("This DataPack is read-only.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureReadable(int size)
        {
            if (IsWriting)
                throw new InvalidOperationException("This DataPack is write-only.");

            if (size < 0 || _position + size > _buffer.Count)
                throw new EndOfStreamException("DataPack does not contain enough data to read.");
        }
    }
}
