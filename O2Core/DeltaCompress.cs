using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    /// <summary>
    /// Performs compression by calculating the difference between two sets of data and then
    /// performing run length encoding.
    /// </summary>
    /// <example>
    /// <code>
    /// byte[] t1 = new byte[] { 0, 1, 3, 4, 5, 1, 4, };
    /// byte[] t2 = new byte[] { 0, 1, 3, 6, 5, 1, 4, };
    ///
    /// byte[] result = DeltaCompress.Compress(t1, t2);
    ///
    /// byte[] o = DeltaCompress.Decompress(t1, result);
    /// </code>
    /// </example>
    public static class DeltaCompress
    {
        private const byte UNCOMPRESSED_BLOCK = 0;
        private const byte DELTA_COMPRESSED_BLOCK = 1;

        private static byte[] CalculateDeltas(byte[] initialData, byte[] newData, int length, int initialDataOffset, int newDataOffset)
        {
            byte[] delta = new byte[length];
            for (int i = 0; i < newData.Length; i++)
            {
                delta[i] = (byte)(initialData[i + initialDataOffset] - newData[i + newDataOffset]);
            }

            return delta;
        }

        private static void WriteBlock(MemoryStream ms, byte[] data, byte flags, int offset, int length)
        {
            ms.WriteByte(flags);

            if (flags == DELTA_COMPRESSED_BLOCK)
            {
                ms.WriteByte((byte)(data.Length & 0xFF));
                ms.WriteByte((byte)((data.Length >> 8) & 0xFF));

                // Then run length encode the data.
                byte value = data[0];
                short count = 1;
                for (int i = 1; i < data.Length; i++)
                {
                    if (data[i] == value)
                    {
                        count++;
                    }
                    else
                    {
                        ms.WriteByte(value);
                        ms.WriteByte((byte)(count & 0xFF));
                        ms.WriteByte((byte)((count >> 8) & 0xFF));

                        value = data[i];
                        count = 1;
                    }
                }

                ms.WriteByte(value);
                ms.WriteByte((byte)(count & 0xFF));
                ms.WriteByte((byte)((count >> 8) & 0xFF));
            }
            else if (flags == UNCOMPRESSED_BLOCK)
            {
                ms.WriteByte((byte)(length & 0xFF));
                ms.WriteByte((byte)((length >> 8) & 0xFF));
                ms.Write(data, offset, length);
            }
        }

        public static byte[] Compress(byte[] initialData, byte[] newData)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                if (initialData.Length > newData.Length)
                {
                    //int offset = FindSubArray(initialData, newData);
                    //if (offset > -1)
                    //{
                    //    WriteData(ms, initialData, newData, offset);
                    //}
                    //else
                    {
                        WriteBlock(ms, newData, UNCOMPRESSED_BLOCK, 0, newData.Length);
                    }
                }
                else if (newData.Length > initialData.Length)
                {
                    int offset = FindSubArray(newData, initialData);
                    if (offset > -1)
                    {
                        WriteData(ms, newData, initialData, offset);
                    }
                    else
                    {
                        WriteBlock(ms, newData, UNCOMPRESSED_BLOCK, 0, newData.Length);
                    }
                }
                else
                {
                    byte[] delta;
                    delta = CalculateDeltas(initialData, newData, initialData.Length, 0, 0);
                    WriteBlock(ms, delta, DELTA_COMPRESSED_BLOCK, 0, initialData.Length);
                }

                return ms.ToArray();
            }
        }

        private static void WriteData(MemoryStream ms, byte[] longer, byte[] shorter, int offset)
        {
            int length = shorter.Length;
            byte[] delta = CalculateDeltas(longer, shorter, length, offset, 0);

            if (offset > 0)
            {
                int uncompressedLength = offset;
                WriteBlock(ms, longer, UNCOMPRESSED_BLOCK, 0, uncompressedLength);
            }

            WriteBlock(ms, delta, DELTA_COMPRESSED_BLOCK, offset, length);

            if (offset + length < longer.Length)
            {
                int uncompressedLength = offset + length;
                WriteBlock(ms, longer, UNCOMPRESSED_BLOCK, uncompressedLength, longer.Length - uncompressedLength);
            }
        }

        private static void ReadBlock(MemoryStream ms, List<byte> newData, byte[] initialData, byte[] delta, ref int pos)
        {
            int flags = delta[pos++];
            if (flags == UNCOMPRESSED_BLOCK)
            {
                int countLo = delta[pos++];
                int countHi = delta[pos++];

                int count = countLo | (countHi << 8);

                for (int i = 0; i < count; i++)
                {
                    newData.Add(delta[pos++]);
                }
            }
            else if (flags == DELTA_COMPRESSED_BLOCK)
            {
                int dataLength;
                {
                    int countLo = delta[pos++];
                    int countHi = delta[pos++];

                    dataLength = countLo | (countHi << 8);
                }

                // Decode the run length encoding.
                int offset = newData.Count;
                int end = newData.Count + dataLength;
                while (newData.Count < end)
                {
                    int value = delta[pos++];
                    int countLo = delta[pos++];
                    int countHi = delta[pos++];

                    int count = countLo | (countHi << 8);

                    for (int j = 0; j < count; j++)
                    {
                        newData.Add((byte)value);
                    }
                }

                // Decode the deltas.
                for (int i = 0; i < initialData.Length; i++)
                {
                    newData[i + offset] = (byte)(initialData[i] - newData[i + offset]);
                }
            }
        }

        public static byte[] Decompress(byte[] initialData, byte[] delta)
        {
            List<byte> newData = new List<byte>();
            using (MemoryStream ms = new MemoryStream())
            {
                int pos = 0;
                while (pos < delta.Length)
                {
                    ReadBlock(ms, newData, initialData, delta, ref pos);
                }
            }

            return newData.ToArray();
        }

        /// <summary>
        /// Searches for the shorter array in the longer array.
        /// </summary>
        /// <param name="longer">The longer array.</param>
        /// <param name="shorter">The shorter array.</param>
        /// <exception cref="ArgumentException">Throws if the parameters are invalid.</exception>
        /// <returns>Returns the index of where the sub array is located, or -1 if the sub array could not be found.</returns>
        private static int FindSubArray(byte[] longer, byte[] shorter)
        {
            if (longer.Length <= shorter.Length)
            {
                throw new ArgumentException($"Invalid arguments '{nameof(longer)}' should be a longer array than '{nameof(shorter)}'.");
            }

            for (int i = 0; i < longer.Length; i++)
            {
                bool success = true;
                for (int j = 0; j < shorter.Length; j++)
                {
                    if (longer[i + j] != shorter[j])
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
