using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Archiver
    {
        private const string archiveName = "Archive";

        /// <summary>
        /// Copies the specified file to the archive.
        /// </summary>
        public static void ArchiveAsset(string name)
        {
            if (!Directory.Exists(archiveName))
            {
                Directory.CreateDirectory(archiveName);
            }

            string filename = Path.GetFileName(name);

            int maxRevision = -1;
            foreach (var file in Directory.GetFiles(archiveName, $"{filename}*"))
            {
                string[] parts = file.Split('#');
                if (parts.Length > 1)
                {
                    int revision = int.Parse(parts[parts.Length - 1]);

                    maxRevision = Math.Max(maxRevision, revision);
                }
            }

            if (maxRevision > -1)
            {
                maxRevision++;
            }
            else
            {
                maxRevision = 1;
            }
            
            File.Copy(name, archiveName + "\\" + filename + "#" + maxRevision);
        }

        /// <summary>
        /// Restores an asset and create a new revision of that asset.
        /// </summary>
        public static bool RestoreAsset(string name, int revision)
        {
            bool success = false;

            if (Directory.Exists(archiveName))
            {
                string filename = Path.GetFileName(name);

                string fileToRestore = $"{archiveName}\\{filename}#{revision}";
                if (File.Exists(fileToRestore))
                {
                    File.Copy(fileToRestore, name, true);
                    ArchiveAsset(name);
                    success = true;
                }
            }

            return success;
        }

        public static string GetAssetHistorySummary(string name)
        {
            string summary = string.Empty;

            if (Directory.Exists(archiveName))
            {
                string filename = Path.GetFileName(name);

                foreach (var file in Directory.GetFiles(archiveName, $"{filename}*"))
                {
                    string[] parts = file.Split('#');
                    if (parts.Length > 1)
                    {
                        int revision = int.Parse(parts[parts.Length - 1]);

                        summary += $"Revision: {revision} {File.GetCreationTime(file)} {Environment.NewLine}";
                    }
                }
            }

            return summary;
        }
    }
}
