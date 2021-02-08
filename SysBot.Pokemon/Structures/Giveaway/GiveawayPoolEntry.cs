using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;
using System.Globalization;

namespace SysBot.Pokemon
{
    public class GiveawayPoolEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Tag { get; set; }
        public string? Uploader { get; set; }
        public PK8? PK8 { get; set; }
        public string? Pokemon { get; set; }

        public static GiveawayPoolEntry CreateUploadEntry(string name, string description, PK8 pkm, string status, string tag, string uploader)
        {
            var entry = new GiveawayPoolEntry();
            entry.Name = name;
            entry.Description = description;
            entry.Status = status;
            entry.Tag = tag;
            entry.Uploader = uploader;
            entry.PK8 = new PK8();
            return entry;
        }
        public string GetSummary()
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            string summ = Id + ": " + textInfo.ToTitleCase(Name);
            if (PK8.OT_Name != null)
                summ += $" (" + PK8.OT_Name + ") ";
           
            summ += "[" + Tag + "]";

            if (PK8.IsShiny)
                summ += "✷";
            return summ;
        }

    }
}