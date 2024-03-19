using System;
using System.Collections.Generic;
using System.Linq;
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
        public static byte[] Compress(byte[] initialData, byte[] newData)
        {
            if (initialData.Length != newData.Length)
            {
                throw new InvalidOperationException("For now intial data and new data must be the same length");
            }

            // First calculate deltas.
            byte[] delta = new byte[Math.Max(newData.Length, initialData.Length)];
            for (int i = 0; i < newData.Length; i++)
            {
                delta[i] = (byte)(initialData[i] - newData[i]);
            }

            if (newData.Length > initialData.Length)
            {
                Array.Copy(newData, initialData.Length, delta, initialData.Length, newData.Length - initialData.Length);
            }
            else if (initialData.Length > newData.Length)
            {
                Array.Copy(initialData, newData.Length, delta, newData.Length, initialData.Length - newData.Length);
            }

            // Then run length encode the data.
            using (MemoryStream ms = new MemoryStream())
            {
                byte value = delta[0];
                short count = 1;
                for (int i = 1; i < delta.Length; i++)
                {
                    if (delta[i] == value)
                    {
                        count++;
                    }
                    else
                    {
                        ms.WriteByte(value);
                        ms.WriteByte((byte)(count & 0xFF));
                        ms.WriteByte((byte)((count >> 8) & 0xFF));

                        value = delta[i];
                        count = 1;
                    }
                }

                ms.WriteByte(value);
                ms.WriteByte((byte)(count & 0xFF));
                ms.WriteByte((byte)((count >> 8) & 0xFF));

                return ms.ToArray();
            }
        }

        public static byte[] Decompress(byte[] initialData, byte[] delta)
        {
            byte[] newData;
            // Decode the run length encoding.
            using (MemoryStream ms = new MemoryStream())
            {
                for (int i =0; i < delta.Length; i+=3)
                {
                    int value = delta[i];
                    int countLo = delta[i + 1];
                    int countHi = delta[i + 2];

                    int count = countLo | (countHi << 8);

                    for (int j = 0; j < count; j++)
                    {
                        ms.WriteByte((byte)value);
                    }
                }

                newData = ms.ToArray();
            }

            // Decode the deltas.
            for (int i = 0; i < newData.Length; i++)
            {
                newData[i] = (byte) (initialData[i] - newData[i]);
            }

            return newData;
        }
    }
}
