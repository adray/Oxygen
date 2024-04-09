using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class AssetLabel
    {
        private struct Item
        {
            public string AssetName { get; private set; }
            public int Revision { get; private set; }

            public Item(string name, int revision)
            {
                AssetName = name;
                Revision = revision;
            }
        }

        public string Creator { get; private init; }
        public string Name { get; private init; }
        private readonly List<Item> items = new List<Item>();

        public AssetLabel(string name, string creator)
        {
            Name = name;
            Creator = creator;
        }

        public void AddItem(string assetName, int revision)
        {
            items.Add(new Item(assetName, revision));
        }

        public int NumItems => items.Count;

        public string GetAssetName(int index)
        {
            return items[index].AssetName;
        }

        public int GetAssetRevision(int index)
        {
            return items[index].Revision;
        }

        public void ClearItems()
        {
            items.Clear();
        }
    }
}
