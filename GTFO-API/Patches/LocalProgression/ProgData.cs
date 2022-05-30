using System.Collections.Generic;

namespace GTFO.API.Patches.LocalProgression
{
    public struct LocalExpProgData
    {
        public string expeditionKey { set; get; } 
        public int MainCompletionCount { set; get; }
        public int SecondaryCompletionCount { set; get; }
        public int ThirdCompletionCount { set; get; }
        public int AllClearCount { set; get; }
    }

    public class LocalRundownProgData
    {
        public string Rundown_Name { set; get; }
        public List<LocalExpProgData> ExpProgs { set; get; }

        public LocalRundownProgData()
        {
            ExpProgs = new List<LocalExpProgData>();
        }
    }
}
