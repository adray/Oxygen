using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Archiver
    {
        private class Asset
        {
            public string? AssetName { get; set; }
            public string? ArchiveName { get;set; }
            public int Version { get; set; }
        }

        private class AssetEvent
        {
            public DateTime Timestamp { get; set; }
            public string? AssetName { get; set; }
            public int Version { get; set; }
            public string? Username { get; set; }
            public string? EventText { get; set; }
        }

        private const string archiveName = "Archive";

        private static DataFile assetDataFile = new DataFile(@"Data\assets.data");
        private static DataFile assetLogDataFile = new DataFile(@"Data\asset_log.data");

        private static Dictionary<string, Asset> assets = new Dictionary<string, Asset>();
        private static List<AssetEvent> events = new List<AssetEvent>();

        public static void LoadAssetFile()
        {
            if (!assetDataFile.ReadFile())
            {
                assetDataFile.CreateFile(new string[] { "ASSET_NAME", "ARCHIVE_NAME", "VERSION" },
                    new DataFile.Column[]
                    {
                        DataFile.Column.String,
                        DataFile.Column.String,
                        DataFile.Column.Int32
                    });
            }
            else
            {
                assetDataFile.SeekBegin();

                do
                {
                    string? assetName = assetDataFile.GetString("ASSET_NAME");
                    string? archiveName = assetDataFile.GetString("ARCHIVE_NAME");
                    int? version = assetDataFile.GetInt32("VERSION");

                    if (assetName != null &&
                        archiveName != null &&
                        version != null)
                    {
                        assets.Add(assetName, new Asset()
                        {
                            ArchiveName = archiveName,
                            AssetName = assetName,
                            Version = version.Value
                        });
                    }

                } while (assetDataFile.NextRow());
            }

            if (!assetLogDataFile.ReadFile())
            {
                assetLogDataFile.CreateFile(new string[] { "ASSET_NAME", "USER_NAME", "VERSION", "TIMESTAMP", "EVENT_TEXT" },
                    new DataFile.Column[]
                    {
                        DataFile.Column.String,
                        DataFile.Column.String,
                        DataFile.Column.Int32,
                        DataFile.Column.Int64,
                        DataFile.Column.String
                    });
            }
            else
            {
                assetLogDataFile.SeekBegin();

                do
                {
                    string? assetName = assetLogDataFile.GetString("ASSET_NAME");
                    string? userName = assetLogDataFile.GetString("USER_NAME");
                    string? eventText = assetLogDataFile.GetString("EVENT_TEXT");
                    int? version = assetLogDataFile.GetInt32("VERSION");
                    long? timestamp = assetLogDataFile.GetInt64("TIMESTAMP");

                    if (assetName != null && userName != null && eventText != null &&
                        version != null && timestamp != null)
                    {
                        events.Add(new AssetEvent()
                        {
                            AssetName = assetName,
                            EventText = eventText,
                            Timestamp = DateTime.FromBinary(timestamp.Value),
                            Version = version.Value,
                            Username = userName
                        });
                    }

                } while (assetLogDataFile.NextRow());
            }
        }

        private static void WriteToEventLog(AssetEvent ev)
        {
            events.Add(ev);

            assetLogDataFile.NewRow();
            assetLogDataFile.SetData("ASSET_NAME", ev.AssetName);
            assetLogDataFile.SetData("USER_NAME", ev.Username);
            assetLogDataFile.SetData("VERSION", ev.Version);
            assetLogDataFile.SetData("EVENT_TEXT", ev.EventText);
            assetLogDataFile.SetData("TIMESTAMP", ev.Timestamp.Ticks);
            assetLogDataFile.NextRow();

            assetLogDataFile.SaveFile();
        }

        private static string GetBackupFilename(string name)
        {
            string filename = Path.GetFileName(name);
            return archiveName + "\\" + filename + ".backup";
        }

        private static string GetDeltaFilename(string name)
        {
            string filename = Path.GetFileName(name);
            return archiveName + "\\" + filename + ".delta";
        }

        /// <summary>
        /// Creates a backup of the specified asset.
        /// </summary>
        public static void BackupAsset(string name)
        {
            if (File.Exists(name))
            {
                if (!Directory.Exists(archiveName))
                {
                    Directory.CreateDirectory(archiveName);
                }

                File.Copy(name, GetBackupFilename(name));
                Logger.Instance.Log("Backup file created for {0}.", name);
            }
        }

        /// <summary>
        /// Calculates the delta for a file in the asset directory and the backup file.
        /// </summary>
        /// <param name="name">The full path to the asset directory.</param>
        /// <returns>The path to the deltas file.</returns>
        private static string CalculateDeltas(string name)
        {
            byte[] newData = File.ReadAllBytes(name);

            string backupFile = GetBackupFilename(name);

            byte[] initialData = File.ReadAllBytes(backupFile);

            string deltaPath = GetDeltaFilename(name);
            File.WriteAllBytes(deltaPath, DeltaCompress.Compress(initialData, newData));
            Logger.Instance.Log("Delta file created for {0}.", name);
            return deltaPath;
        }

        private static void CleanupBackupFile(string name)
        {
            File.Delete(GetDeltaFilename(name));
            File.Delete(GetBackupFilename(name));
            Logger.Instance.Log("Delta and backup files for {0} cleaned up.", name);
        }

        private static void SaveAssetFile()
        {
            assetDataFile.Clear();

            foreach (var asset in assets)
            {
                assetDataFile.NewRow();
                assetDataFile.SetData("ASSET_NAME", asset.Value.AssetName);
                assetDataFile.SetData("ARCHIVE_NAME", asset.Value.ArchiveName);
                assetDataFile.SetData("VERSION", asset.Value.Version);
                assetDataFile.NextRow();
            }

            assetDataFile.SaveFile();
        }

        private static ZipArchive OpenAssetArchive(Asset asset)
        {
            return ZipFile.Open(archiveName + "\\" + asset.ArchiveName, ZipArchiveMode.Update);
        }

        /// <summary>
        /// Copies the specified file to the archive.
        /// </summary>
        public static void ArchiveAsset(string name, string username)
        {
            if (!Directory.Exists(archiveName))
            {
                Directory.CreateDirectory(archiveName);
            }

            string filename = Path.GetFileName(name);

            if (assets.TryGetValue(filename, out Asset? asset))
            {
                asset.Version++;

                string deltaFile = CalculateDeltas(name);
                
                using (ZipArchive zipArchive = OpenAssetArchive(asset))
                {
                    zipArchive.CreateEntryFromFile(deltaFile, filename + "#" + asset.Version);
                }

                WriteToEventLog(new AssetEvent()
                {
                    AssetName = asset.AssetName,
                    EventText = "File edited.",
                    Timestamp = DateTime.Now,
                    Username = username,
                    Version = asset.Version
                });
            }
            else
            {
                string base64encoded = Convert.ToBase64String(Encoding.Default.GetBytes(filename));

                Asset newAsset = new Asset()
                {
                    AssetName = filename,
                    Version = 1,
                    ArchiveName = base64encoded[0].ToString()
                };

                assets.Add(filename, newAsset);

                using (ZipArchive zipArchive = OpenAssetArchive(newAsset))
                {
                    zipArchive.CreateEntryFromFile(name, filename + "#" + newAsset.Version);
                }

                WriteToEventLog(new AssetEvent()
                {
                    AssetName = newAsset.AssetName,
                    EventText = "File created.",
                    Timestamp = DateTime.Now,
                    Username = username,
                    Version = newAsset.Version
                });
            }

            CleanupBackupFile(name);
            SaveAssetFile();
        }

        /// <summary>
        /// Restores an asset and create a new revision of that asset.
        /// </summary>
        public static bool RestoreAsset(string name, int revision, string username)
        {
            bool success = true;
            Asset? asset = null;
            string filename = string.Empty;

            if (!Directory.Exists(archiveName))
            {
                success = false;
            }

            if (success)
            {
                filename = Path.GetFileName(name);

                if (!assets.TryGetValue(filename, out asset))
                {
                    success = false;
                }
            }

            if (success)
            {
                success = asset != null && asset.Version > revision && revision > 0;
            }

            if (success)
            {
                success = false;

                if (asset != null)
                {
                    BackupAsset(name);

                    byte[]? initialData = null;
                    string restoreFile = archiveName + "\\" + filename + ".restore";
                    string restoreDeltaFile = archiveName + "\\" + filename + ".restoredelta";

                    using (ZipArchive zipArchive = OpenAssetArchive(asset))
                    {
                        var entry = zipArchive.GetEntry(asset.AssetName + "#1");
                        if (entry != null)
                        {
                            entry.ExtractToFile(restoreFile);
                            Logger.Instance.Log("Create {0}", restoreFile);
                            initialData = File.ReadAllBytes(restoreFile);

                            for (int i = 2; i <= revision; i++)
                            {
                                entry = zipArchive.GetEntry(asset.AssetName + "#" + i);
                                if (entry != null)
                                {
                                    entry.ExtractToFile(restoreDeltaFile, true);
                                }

                                byte[] deltaData = File.ReadAllBytes(restoreDeltaFile);
                                initialData = DeltaCompress.Decompress(initialData, deltaData);
                            }
                        }
                    }

                    if (initialData != null)
                    {
                        File.Delete(name);
                        File.WriteAllBytes(name, initialData);

                        File.Delete(restoreFile);
                        File.Delete(restoreDeltaFile);

                        ArchiveAsset(filename, username);

                        success = true;
                    }
                }
            }

            return success;
        }

        public static string GetAssetHistorySummary(string name)
        {
            string summary = string.Empty;
            string filename = Path.GetFileName(name);
            foreach (var ev in events)
            {
                if (ev.AssetName == filename)
                {
                    summary += $"#{ev.Version},{ev.Username},{ev.EventText},{ev.Timestamp}";
                }
            }

            return summary;
        }

        public static IList<string> GetAssets()
        {
            List<string> assetList = new List<string>();

            foreach (var asset in assets)
            {
                assetList.Add(asset.Key);
            }

            return assetList;
        }
    }
}
