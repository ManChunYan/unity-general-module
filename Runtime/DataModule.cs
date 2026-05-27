using System.Collections.Generic;

namespace General.Module
{
    public class EmptySubData
    {
        public uint ID;
    }

    public class EmptyMainData
    {
        
    }

    public abstract partial class DataModule<TMain,TSub> : ModuleBase where TMain : EmptyMainData where TSub : EmptySubData
    {

        public abstract TMain GetMainData();
        public abstract TSub GetSubData(uint id);
        public abstract IEnumerable<TSub> GetSubDataList();
    }
}



