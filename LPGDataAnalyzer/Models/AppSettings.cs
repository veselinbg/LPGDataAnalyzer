using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Models
{
    internal class AppSettings
    {
        public string LastSavedFilePath { get; set; } = string.Empty;
        public string LastLoadedFuelTable { get; set;  } = string.Empty;
        public string ImagePath { get; set; } = @"C:\Users\veselin.ivanov\Desktop\Untitled.png";
    }
}
