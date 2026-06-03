using System;
using System.Collections.Generic;

namespace ContextHubDev
{
    public class FenceData
    {
        public string Name { get; set; } = "Нова плитка";
        public int X { get; set; } = 100;
        public int Y { get; set; } = 100;
        public int W { get; set; } = 310;
        public int H { get; set; } = 420;
        public List<string> Tabs { get; set; } = new List<string>();
        public int SelectedTabIndex { get; set; } = 0;
    }
}