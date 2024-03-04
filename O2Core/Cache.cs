using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public class Cache
    {
        private string? cacheFilename;
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        public string? CacheItem(string filename)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(filename);
            }
            catch (IOException)
            {
                return null;
            }

            byte[] hashedBytes = SHA256.HashData(bytes);
            string checksum = Convert.ToBase64String(hashedBytes);
            cache[filename] = checksum;
            return checksum;
        }

        public void CacheItem(string filename, string checksum)
        {
            cache[filename] = checksum;
        }

        public string? GetChecksum(string filename)
        {
            string? checksum;
            cache.TryGetValue(filename, out checksum);
            return checksum;
        }

        public void Remove(string filename)
        {
            cache.Remove(filename);
        }

        public void LoadCache(string cacheFilename)
        {
            this.cacheFilename = cacheFilename;
            this.cache.Clear();

            if (!File.Exists(cacheFilename))
            {
                return;
            }

            using (FileStream stream = File.OpenRead(cacheFilename))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int numCache = reader.ReadInt32();
                    for (int i = 0; i < numCache; i++)
                    {
                        string filename = reader.ReadString();
                        string checksum = reader.ReadString();

                        cache.Add(filename, checksum);
                    }
                }
            }
        }

        public void SaveCache()
        {
            if (!string.IsNullOrEmpty(this.cacheFilename))
            {
                using (FileStream stream = File.OpenWrite(cacheFilename))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(cache.Count);

                        foreach (var item in cache)
                        {
                            writer.Write(item.Key);
                            writer.Write(item.Value);
                        }
                    }
                }
            }
        }
    }
}
