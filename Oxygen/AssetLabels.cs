using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal static class AssetLabels
    {
        private const string LabelFolder = "Labels";
        private static DataFile labelFile = new DataFile(@"Data\labels.data");

        public static void LoadLabelsFile()
        {
            if (!labelFile.ReadFile())
            {
                CreateLabelsFile();
            }
        }

        private static void CreateLabelsFile()
        {
            labelFile.CreateFile(
                new string[] { "NAME", "LABEL_FILE", "CREATOR" },
                new DataFile.Column[] { DataFile.Column.String, DataFile.Column.String, DataFile.Column.String }
            );
        }

        private static string GetLabelName(string label)
        {
            return LabelFolder + "\\" + label + ".data";
        }

        public static bool LabelExists(string label)
        {
            string filename = GetLabelName(label);
            return File.Exists(filename);
        }

        public static bool CreateLabel(AssetLabel label)
        {
            if (!Directory.Exists(LabelFolder))
            {
                Directory.CreateDirectory(LabelFolder);
            }

            string filename = GetLabelName(label.Name);
            if (!File.Exists(filename))
            {
                DataFile dataFile = new DataFile(filename);
                dataFile.CreateFile(
                    new string[] { "NAME", "REVISION" },
                    new DataFile.Column[] { DataFile.Column.String, DataFile.Column.Int32 });

                for (int i = 0; i < label.NumItems; i++)
                {
                    dataFile.NewRow();
                    dataFile.SetData("NAME", label.GetAssetName(i));
                    dataFile.SetData("REVISION", label.GetAssetRevision(i));
                    dataFile.NextRow();
                }

                dataFile.SaveFile();

                labelFile.SeekEnd();
                labelFile.NewRow();

                labelFile.SetData("NAME", label.Name);
                labelFile.SetData("CREATOR", label.Creator);
                labelFile.SetData("LABEL_FILE", filename);

                labelFile.NextRow();
                labelFile.SaveFile();

                return true;
            }

            return false;
        }

        private static void LoadLabel(string filename, AssetLabel label)
        {
            if (Directory.Exists(LabelFolder))
            {
                DataFile dataFile = new DataFile(filename);
                dataFile.ReadFile();

                dataFile.SeekBegin();

                do
                {
                    string? name = dataFile.GetString("NAME");
                    int? revision = dataFile.GetInt32("REVISION");

                    if (name != null &&
                        revision != null)
                    {
                        label.AddItem(name, revision.Value);
                    }

                } while (dataFile.NextRow());
            }
        }

        public static AssetLabel? FindLabel(string label)
        {
            labelFile.SeekBegin();

            do
            {
                string? name = labelFile.GetString("NAME");
                string? file = labelFile.GetString("LABEL_FILE");
                string creator = labelFile.GetString("CREATOR") ?? string.Empty;

                if (name == label && file != null)
                {
                    AssetLabel assetLabel = new AssetLabel(name, creator);
                    LoadLabel(file, assetLabel);
                    return assetLabel;
                }

            } while (labelFile.NextRow());

            return null;
        }
    }
}
