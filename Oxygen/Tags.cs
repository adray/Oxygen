using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Tags
    {
        private DataFile tags = new DataFile(@"Data\tags.data");
        private Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>();
        /// <summary>
        /// List of tags by asset.
        /// </summary>
        private Dictionary<string, List<string>> assets = new Dictionary<string, List<string>>();
        private int nextID = 0;

        public Tags()
        {
            LoadTags();
        }

        private void AddTag(string tag, string asset)
        {
            if (groups.TryGetValue(tag, out List<string>? tagGroup) && tagGroup != null)
            {
                tagGroup.Add(asset);
            }
            else
            {
                var group = new List<string>
                {
                    asset
                };
                groups.Add(tag, group);
            }

            if (assets.TryGetValue(asset, out List<string>? assetGroup) && assetGroup != null)
            {
                assetGroup.Add(tag);
            }
            else
            {
                var group = new List<string>
                {
                    tag
                };
                assets.Add(asset, group);
            }
        }

        private void LoadTags()
        {
            if (!tags.ReadFile())
            {
                tags.CreateFile(new string[]
                {
                    "id",
                    "asset",
                    "tag"
                }, new DataFile.Column[]
                {
                    DataFile.Column.Int32,
                    DataFile.Column.String,
                    DataFile.Column.String
                });
            }
            else
            {
                tags.SeekBegin();

                if (tags.NumRows > 0)
                {
                    do
                    {
                        int? id = tags.GetInt32("id");
                        string? asset = tags.GetString("asset");
                        string? tag = tags.GetString("tag");

                        if (id != null && asset != null && tag != null)
                        {
                            AddTag(tag, asset);

                            nextID = Math.Max(nextID, id.Value + 1);
                        }

                    } while (tags.NextRow());
                }
            }
        }

        public void Save()
        {
            tags.SaveFile();
        }

        public void AddNewTag(string asset, string tag)
        {
            using (var transaction = tags.CreateTransaction())
            {
                tags.SetData("asset", asset);
                tags.SetData("tag", tag);
                tags.SetData("id", nextID++);
            }

            AddTag(tag, asset);
            Save();
        }

        public void DeleteTag(string asset, string tag)
        {
            tags.SeekBegin();

            do
            {
                string? row = tags.GetString("asset");
                string? rowTag = tags.GetString("tag");
                if (row == asset && rowTag == tag)
                {
                    tags.DeleteRow();
                }

            } while (tags.NextRow());

            Save();
        }

        public IList<string>? QueryTags(string tag)
        {
            if (tag != null && groups.TryGetValue(tag, out List<string>? group))
            {
                return group;
            }

            return null;
        }

        public IList<string>? QueryTagsByName(string name)
        {
            if (name != null && assets.TryGetValue(name, out List<string>? group))
            {
                return group;
            }

            return null;
        }
    }
}
