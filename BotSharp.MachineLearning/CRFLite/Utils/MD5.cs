using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.MachineLearning.CRFLite.Utils
{
    public sealed class MD5
    {
        private const int BLOCK_SIZE_BYTES = 64;
        private const int HASH_SIZE_BYTES = 16;

        private uint _state0;
        private uint _state1;
        private uint _state2;
        private uint _state3;

        private uint _decodeBuf0;
        private uint _decodeBuf1;
        private uint _decodeBuf2;
        private uint _decodeBuf3;
        private uint _decodeBuf4;
        private uint _decodeBuf5;
        private uint _decodeBuf6;
        private uint _decodeBuf7;
        private uint _decodeBuf8;
        private uint _decodeBuf9;
        private uint _decodeBuf10;
        private uint _decodeBuf11;
        private uint _decodeBuf12;
        private uint _decodeBuf13;
        private uint _decodeBuf14;
        private uint _decodeBuf15;

        private ulong count;
        private byte[] _ProcessingBuffer;   // Used to start data when passed less than a block worth.
        private int _ProcessingBufferCount; // Counts how much data we have stored that still needs processed.
        private byte[] hash;
        private byte[] fooBuffer;

        public MD5()
        {
            fooBuffer = new byte[BLOCK_SIZE_BYTES * 4096];
            hash = new byte[16];
            _ProcessingBuffer = new byte[BLOCK_SIZE_BYTES];

            Initialize();
        }

        ~MD5()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_ProcessingBuffer != null)
            {
                Array.Clear(_ProcessingBuffer, 0, _ProcessingBuffer.Length);
                _ProcessingBuffer = null;
            }
        }

        private void HashCore(byte[] rgb, int start, int size)
        {
            int i;
            if (_ProcessingBufferCount != 0)
            {
                if (size < (BLOCK_SIZE_BYTES - _ProcessingBufferCount))
                {
                    System.Buffer.BlockCopy(rgb, start, _ProcessingBuffer, _ProcessingBufferCount, size);
                    _ProcessingBufferCount += size;
                    return;
                }
                else
                {
                    i = (BLOCK_SIZE_BYTES - _ProcessingBufferCount);
                    System.Buffer.BlockCopy(rgb, start, _ProcessingBuffer, _ProcessingBufferCount, i);
                    ProcessBlock(_ProcessingBuffer, 0);
                    _ProcessingBufferCount = 0;
                    start += i;
                    size -= i;
                }
            }

            for (i = 0; i < size - size % BLOCK_SIZE_BYTES; i += BLOCK_SIZE_BYTES)
            {
                ProcessBlock(rgb, start + i);
            }

            if (size % BLOCK_SIZE_BYTES != 0)
            {
                System.Buffer.BlockCopy(rgb, size - size % BLOCK_SIZE_BYTES + start, _ProcessingBuffer, 0, size % BLOCK_SIZE_BYTES);
                _ProcessingBufferCount = size % BLOCK_SIZE_BYTES;
            }
        }

        public long Compute64BitHash(byte[] buffer)
        {
            HashCore(buffer, 0, buffer.Length);

            ProcessFinalBlock(_ProcessingBuffer, 0, _ProcessingBufferCount);
            long longRst = (((long)_state1 << 32) | (long)_state0);
            this.Initialize();

            return longRst;
        }

        public byte[] ComputeHash(byte [] buffer)
        {
            HashCore(buffer, 0, buffer.Length);

            ProcessFinalBlock(_ProcessingBuffer, 0, _ProcessingBufferCount);

            hash[0] = (byte)(_state0);
            hash[1] = (byte)(_state0 >> 8);
            hash[2] = (byte)(_state0 >> 16);
            hash[3] = (byte)(_state0 >> 24);
            hash[4] = (byte)(_state1);
            hash[5] = (byte)(_state1 >> 8);
            hash[6] = (byte)(_state1 >> 16);
            hash[7] = (byte)(_state1 >> 24);
            hash[8] = (byte)(_state2);
            hash[9] = (byte)(_state2 >> 8);
            hash[10] = (byte)(_state2 >> 16);
            hash[11] = (byte)(_state2 >> 24);
            hash[12] = (byte)(_state3);
            hash[13] = (byte)(_state3 >> 8);
            hash[14] = (byte)(_state3 >> 16);
            hash[15] = (byte)(_state3 >> 24);

            return hash;
        }

        public void Initialize()
        {
            count = 0;
            _ProcessingBufferCount = 0;

            _state0 = 0x67452301;
            _state1 = 0xefcdab89;
            _state2 = 0x98badcfe;
            _state3 = 0x10325476;
        }

        private void ProcessBlock(byte[] inputBuffer, int inputOffset)
        {
            uint a, b, c, d;

            count += BLOCK_SIZE_BYTES;

            _decodeBuf0 = ((uint)(inputBuffer[inputOffset] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 1] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 2] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 3]) << 24);

            _decodeBuf1 = ((uint)(inputBuffer[inputOffset + 4] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 5] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 6] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 7]) << 24);

            _decodeBuf2 = ((uint)(inputBuffer[inputOffset + 8] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 9] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 10] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 11]) << 24);

            _decodeBuf3 = ((uint)(inputBuffer[inputOffset + 12] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 13] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 14] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 15]) << 24);

            _decodeBuf4 = ((uint)(inputBuffer[inputOffset + 16] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 17] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 18] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 19]) << 24);

            _decodeBuf5 = ((uint)(inputBuffer[inputOffset + 20] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 21] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 22] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 23]) << 24);

            _decodeBuf6 = ((uint)(inputBuffer[inputOffset + 24] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 25] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 26] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 27]) << 24);

            _decodeBuf7 = ((uint)(inputBuffer[inputOffset + 28] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 29] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 30] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 31]) << 24);

            _decodeBuf8 = ((uint)(inputBuffer[inputOffset + 32] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 33] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 34] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 35]) << 24);

            _decodeBuf9 = ((uint)(inputBuffer[inputOffset + 36] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 37] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 38] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 39]) << 24);

            _decodeBuf10 = ((uint)(inputBuffer[inputOffset + 40] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 41] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 42] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 43]) << 24);

            _decodeBuf11 = ((uint)(inputBuffer[inputOffset + 44] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 45] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 46] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 47]) << 24);

            _decodeBuf12 = ((uint)(inputBuffer[inputOffset + 48] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 49] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 50] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 51]) << 24);

            _decodeBuf13 = ((uint)(inputBuffer[inputOffset + 52] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 53] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 54] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 55]) << 24);

            _decodeBuf14 = ((uint)(inputBuffer[inputOffset + 56] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 57] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 58] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 59]) << 24);

            _decodeBuf15 = ((uint)(inputBuffer[inputOffset + 60] & 0xff)) |
              (((uint)(inputBuffer[inputOffset + 61] & 0xff)) << 8) |
              (((uint)(inputBuffer[inputOffset + 62] & 0xff)) << 16) |
              (((uint)inputBuffer[inputOffset + 63]) << 24);

            a = _state0;
            b = _state1;
            c = _state2;
            d = _state3;

            // ---- Round 1 --------

            // ---- Round 1 --------

            a += (((c ^ d) & b) ^ d) + (uint)0xd76aa478 + _decodeBuf0;
            a = (a << 7) | (a >> 25);
            a += b;

            d += (((b ^ c) & a) ^ c) + (uint)0xe8c7b756 + _decodeBuf1;
            d = (d << 12) | (d >> 20);
            d += a;

            c += (((a ^ b) & d) ^ b) + (uint)0x242070db + _decodeBuf2;
            c = (c << 17) | (c >> 15);
            c += d;

            b += (((d ^ a) & c) ^ a) + (uint)0xc1bdceee + _decodeBuf3;
            b = (b << 22) | (b >> 10);
            b += c;

            a += (((c ^ d) & b) ^ d) + (uint)0xf57c0faf + _decodeBuf4;
            a = (a << 7) | (a >> 25);
            a += b;

            d += (((b ^ c) & a) ^ c) + (uint)0x4787c62a + _decodeBuf5;
            d = (d << 12) | (d >> 20);
            d += a;

            c += (((a ^ b) & d) ^ b) + (uint)0xa8304613 + _decodeBuf6;
            c = (c << 17) | (c >> 15);
            c += d;

            b += (((d ^ a) & c) ^ a) + (uint)0xfd469501 + _decodeBuf7;
            b = (b << 22) | (b >> 10);
            b += c;

            a += (((c ^ d) & b) ^ d) + (uint)0x698098d8 + _decodeBuf8;
            a = (a << 7) | (a >> 25);
            a += b;

            d += (((b ^ c) & a) ^ c) + (uint)0x8b44f7af + _decodeBuf9;
            d = (d << 12) | (d >> 20);
            d += a;

            c += (((a ^ b) & d) ^ b) + (uint)0xffff5bb1 + _decodeBuf10;
            c = (c << 17) | (c >> 15);
            c += d;

            b += (((d ^ a) & c) ^ a) + (uint)0x895cd7be + _decodeBuf11;
            b = (b << 22) | (b >> 10);
            b += c;

            a += (((c ^ d) & b) ^ d) + (uint)0x6b901122 + _decodeBuf12;
            a = (a << 7) | (a >> 25);
            a += b;

            d += (((b ^ c) & a) ^ c) + (uint)0xfd987193 + _decodeBuf13;
            d = (d << 12) | (d >> 20);
            d += a;

            c += (((a ^ b) & d) ^ b) + (uint)0xa679438e + _decodeBuf14;
            c = (c << 17) | (c >> 15);
            c += d;

            b += (((d ^ a) & c) ^ a) + (uint)0x49b40821 + _decodeBuf15;
            b = (b << 22) | (b >> 10);
            b += c;


            // ---- Round 2 --------

            a += ((b & d) | (c & ~d)) + (uint)0xf61e2562 + _decodeBuf1;
            a = (a << 5) | (a >> 27);
            a += b;

            d += ((a & c) | (b & ~c)) + (uint)0xc040b340 + _decodeBuf6;
            d = (d << 9) | (d >> 23);
            d += a;

            c += ((d & b) | (a & ~b)) + (uint)0x265e5a51 + _decodeBuf11;
            c = (c << 14) | (c >> 18);
            c += d;

            b += ((c & a) | (d & ~a)) + (uint)0xe9b6c7aa + _decodeBuf0;
            b = (b << 20) | (b >> 12);
            b += c;

            a += ((b & d) | (c & ~d)) + (uint)0xd62f105d + _decodeBuf5;
            a = (a << 5) | (a >> 27);
            a += b;

            d += ((a & c) | (b & ~c)) + (uint)0x02441453 + _decodeBuf10;
            d = (d << 9) | (d >> 23);
            d += a;

            c += ((d & b) | (a & ~b)) + (uint)0xd8a1e681 + _decodeBuf15;
            c = (c << 14) | (c >> 18);
            c += d;

            b += ((c & a) | (d & ~a)) + (uint)0xe7d3fbc8 + _decodeBuf4;
            b = (b << 20) | (b >> 12);
            b += c;

            a += ((b & d) | (c & ~d)) + (uint)0x21e1cde6 + _decodeBuf9;
            a = (a << 5) | (a >> 27);
            a += b;

            d += ((a & c) | (b & ~c)) + (uint)0xc33707d6 + _decodeBuf14;
            d = (d << 9) | (d >> 23);
            d += a;

            c += ((d & b) | (a & ~b)) + (uint)0xf4d50d87 + _decodeBuf3;
            c = (c << 14) | (c >> 18);
            c += d;

            b += ((c & a) | (d & ~a)) + (uint)0x455a14ed + _decodeBuf8;
            b = (b << 20) | (b >> 12);
            b += c;

            a += ((b & d) | (c & ~d)) + (uint)0xa9e3e905 + _decodeBuf13;
            a = (a << 5) | (a >> 27);
            a += b;

            d += ((a & c) | (b & ~c)) + (uint)0xfcefa3f8 + _decodeBuf2;
            d = (d << 9) | (d >> 23);
            d += a;

            c += ((d & b) | (a & ~b)) + (uint)0x676f02d9 + _decodeBuf7;
            c = (c << 14) | (c >> 18);
            c += d;

            b += ((c & a) | (d & ~a)) + (uint)0x8d2a4c8a + _decodeBuf12;
            b = (b << 20) | (b >> 12);
            b += c;


            // ---- Round 3 --------

            a += (b ^ c ^ d) + (uint)0xfffa3942 + _decodeBuf5;
            a = (a << 4) | (a >> 28);
            a += b;

            d += (a ^ b ^ c) + (uint)0x8771f681 + _decodeBuf8;
            d = (d << 11) | (d >> 21);
            d += a;

            c += (d ^ a ^ b) + (uint)0x6d9d6122 + _decodeBuf11;
            c = (c << 16) | (c >> 16);
            c += d;

            b += (c ^ d ^ a) + (uint)0xfde5380c + _decodeBuf14;
            b = (b << 23) | (b >> 9);
            b += c;

            a += (b ^ c ^ d) + (uint)0xa4beea44 + _decodeBuf1;
            a = (a << 4) | (a >> 28);
            a += b;

            d += (a ^ b ^ c) + (uint)0x4bdecfa9 + _decodeBuf4;
            d = (d << 11) | (d >> 21);
            d += a;

            c += (d ^ a ^ b) + (uint)0xf6bb4b60 + _decodeBuf7;
            c = (c << 16) | (c >> 16);
            c += d;

            b += (c ^ d ^ a) + (uint)0xbebfbc70 + _decodeBuf10;
            b = (b << 23) | (b >> 9);
            b += c;

            a += (b ^ c ^ d) + (uint)0x289b7ec6 + _decodeBuf13;
            a = (a << 4) | (a >> 28);
            a += b;

            d += (a ^ b ^ c) + (uint)0xeaa127fa + _decodeBuf0;
            d = (d << 11) | (d >> 21);
            d += a;

            c += (d ^ a ^ b) + (uint)0xd4ef3085 + _decodeBuf3;
            c = (c << 16) | (c >> 16);
            c += d;

            b += (c ^ d ^ a) + (uint)0x04881d05 + _decodeBuf6;
            b = (b << 23) | (b >> 9);
            b += c;

            a += (b ^ c ^ d) + (uint)0xd9d4d039 + _decodeBuf9;
            a = (a << 4) | (a >> 28);
            a += b;

            d += (a ^ b ^ c) + (uint)0xe6db99e5 + _decodeBuf12;
            d = (d << 11) | (d >> 21);
            d += a;

            c += (d ^ a ^ b) + (uint)0x1fa27cf8 + _decodeBuf15;
            c = (c << 16) | (c >> 16);
            c += d;

            b += (c ^ d ^ a) + (uint)0xc4ac5665 + _decodeBuf2;
            b = (b << 23) | (b >> 9);
            b += c;


            // ---- Round 4 --------

            a += (((~d) | b) ^ c) + (uint)0xf4292244 + _decodeBuf0;
            a = (a << 6) | (a >> 26);
            a += b;

            d += (((~c) | a) ^ b) + (uint)0x432aff97 + _decodeBuf7;
            d = (d << 10) | (d >> 22);
            d += a;

            c += (((~b) | d) ^ a) + (uint)0xab9423a7 + _decodeBuf14;
            c = (c << 15) | (c >> 17);
            c += d;

            b += (((~a) | c) ^ d) + (uint)0xfc93a039 + _decodeBuf5;
            b = (b << 21) | (b >> 11);
            b += c;

            a += (((~d) | b) ^ c) + (uint)0x655b59c3 + _decodeBuf12;
            a = (a << 6) | (a >> 26);
            a += b;

            d += (((~c) | a) ^ b) + (uint)0x8f0ccc92 + _decodeBuf3;
            d = (d << 10) | (d >> 22);
            d += a;

            c += (((~b) | d) ^ a) + (uint)0xffeff47d + _decodeBuf10;
            c = (c << 15) | (c >> 17);
            c += d;

            b += (((~a) | c) ^ d) + (uint)0x85845dd1 + _decodeBuf1;
            b = (b << 21) | (b >> 11);
            b += c;

            a += (((~d) | b) ^ c) + (uint)0x6fa87e4f + _decodeBuf8;
            a = (a << 6) | (a >> 26);
            a += b;

            d += (((~c) | a) ^ b) + (uint)0xfe2ce6e0 + _decodeBuf15;
            d = (d << 10) | (d >> 22);
            d += a;

            c += (((~b) | d) ^ a) + (uint)0xa3014314 + _decodeBuf6;
            c = (c << 15) | (c >> 17);
            c += d;

            b += (((~a) | c) ^ d) + (uint)0x4e0811a1 + _decodeBuf13;
            b = (b << 21) | (b >> 11);
            b += c;

            a += (((~d) | b) ^ c) + (uint)0xf7537e82 + _decodeBuf4;
            a = (a << 6) | (a >> 26);
            a += b;

            d += (((~c) | a) ^ b) + (uint)0xbd3af235 + _decodeBuf11;
            d = (d << 10) | (d >> 22);
            d += a;

            c += (((~b) | d) ^ a) + (uint)0x2ad7d2bb + _decodeBuf2;
            c = (c << 15) | (c >> 17);
            c += d;

            b += (((~a) | c) ^ d) + (uint)0xeb86d391 + _decodeBuf9;
            b = (b << 21) | (b >> 11);
            b += c;

            _state0 += a;
            _state1 += b;
            _state2 += c;
            _state3 += d;
        }

        private void ProcessFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ulong total = count + (ulong)inputCount;
            int paddingSize = (int)(56 - total % BLOCK_SIZE_BYTES);

            if (paddingSize < 1)
                paddingSize += BLOCK_SIZE_BYTES;

            for (int i = 0; i < inputCount; i++)
            {
                fooBuffer[i] = inputBuffer[i + inputOffset];
            }

            fooBuffer[inputCount] = 0x80;
            for (int i = inputCount + 1; i < inputCount + paddingSize; i++)
            {
                fooBuffer[i] = 0x00;
            }

            // I deal in bytes. The algorithm deals in bits.
            ulong size = total << 3;
            AddLength(size, fooBuffer, inputCount + paddingSize);
            ProcessBlock(fooBuffer, 0);

            if (inputCount + paddingSize + 8 == 128)
            {
                ProcessBlock(fooBuffer, 64);
            }
        }

        internal void AddLength(ulong length, byte[] buffer, int position)
        {
            buffer[position++] = (byte)(length);
            buffer[position++] = (byte)(length >> 8);
            buffer[position++] = (byte)(length >> 16);
            buffer[position++] = (byte)(length >> 24);
            buffer[position++] = (byte)(length >> 32);
            buffer[position++] = (byte)(length >> 40);
            buffer[position++] = (byte)(length >> 48);
            buffer[position] = (byte)(length >> 56);
        }
    }
}
