using System.Collections.Generic;

namespace General.Module
{
    public interface IDataPack
    {
        /// <summary>
        /// Gets the number of sub-data records.
        /// </summary>
        public int SubDataCount();

        /// <summary>
        /// Loads main data from the save pack.
        /// </summary>
        public void SetMainDataDirty(DataPack pack, string fileName);

        /// <summary>
        /// Loads sub-data from the save pack.
        /// </summary>
        public void SetSubDataDirty(uint id, DataPack pack, string fileName);

        /// <summary>
        /// Saves main data into the save pack.
        /// </summary>
        public void SaveMainData(DataPack pack, string fileName);

        /// <summary>
        /// Saves sub-data into the save pack.
        /// </summary>
        public void SaveSubData(uint id, DataPack pack, string fileName);

        /// <summary>
        /// Gets all sub-data IDs.
        /// </summary>
        public IEnumerable<uint> GetSubDataIDList();
    }
}
