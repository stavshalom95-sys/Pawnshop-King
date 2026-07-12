using System;
using System.Collections.Generic;

namespace PawnshopKing.Data.Runtime
{
    /// <summary>Physical shop capabilities, grown through upgrades (GDD 23, 38.2).</summary>
    [Serializable]
    public class ShopState
    {
        public int storageSize;
        public int displaySize;
        public int securityLevel;
        public List<string> installedToolIds = new List<string>();
        public List<string> unlockedSystemIds = new List<string>();
    }
}
