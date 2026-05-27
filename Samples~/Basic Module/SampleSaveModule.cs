using System.Collections.Generic;
using UnityEngine;

namespace General.Module.Samples
{
    [Module(ModuleOpCode.None)]
    public class SampleSaveModule : ModuleBase, IDataPack
    {
        private readonly Dictionary<uint, string> _items = new Dictionary<uint, string>
        {
            { 1, "Sword" },
            { 2, "Shield" }
        };

        private int _score;

        public int SubDataCount()
        {
            return _items.Count;
        }

        public IEnumerable<uint> GetSubDataIDList()
        {
            return _items.Keys;
        }

        public void IncrementScore()
        {
            _score++;
            Debug.Log($"Sample score: {_score}");
        }

        public void SaveMainData(DataPack pack, string fileName)
        {
            pack.Write(_score);
        }

        public void SetMainDataDirty(DataPack pack, string fileName)
        {
            _score = pack.ReadInt32();
            Debug.Log($"Loaded sample score: {_score}");
        }

        public void SaveSubData(uint id, DataPack pack, string fileName)
        {
            pack.Write(_items.TryGetValue(id, out var itemName) ? itemName : string.Empty);
        }

        public void SetSubDataDirty(uint id, DataPack pack, string fileName)
        {
            _items[id] = pack.ReadString();
            Debug.Log($"Loaded sample item {id}: {_items[id]}");
        }
    }
}
