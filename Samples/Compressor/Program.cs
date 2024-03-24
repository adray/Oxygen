// This sample loads in two files and produces the data for a delta update.
// 

using System.IO;
using Oxygen;

namespace Compressor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                byte[] file1 = File.ReadAllBytes(args[0]);
                byte[] file2 = File.ReadAllBytes(args[1]);

                byte[] deltaData = DeltaCompress.Compress(file1, file2);
                Console.WriteLine("Compression OK");

                for (int i = 0; i < deltaData.Length; i++)
                {
                    if  (i > 0)
                    {
                        Console.Write(" ");
                    }

                    Console.Write(deltaData[i]);
                }
                Console.WriteLine();

                byte[] decompressedBytes = DeltaCompress.Decompress(file1, deltaData);
                if (decompressedBytes.Length == file2.Length)
                {
                    bool success = true;
                    for (int i = 0; i < decompressedBytes.Length; i++)
                    {
                        if (decompressedBytes[i] != file2[i])
                        {
                            success = false;
                            break;
                        }
                    }

                    if (success)
                    {
                        Console.WriteLine("Decompression OK");
                    }
                    else
                    {
                        Console.WriteLine("Decompression failed");
                    }
                }
                else
                {
                    Console.WriteLine("Decompression failed expected {0} bytes but was {1} bytes.", file2.Length, decompressedBytes.Length);
                }
            }
            else
            {
                Console.WriteLine("Usage: compressor <file1> <file2>");
            }
        }
    }
}