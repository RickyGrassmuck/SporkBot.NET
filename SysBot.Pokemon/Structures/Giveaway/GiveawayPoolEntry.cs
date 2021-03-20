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
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? Tag { get; set; }
        public string? Uploader { get; set; }
        public PK8? PK8 { get; set; }
        public string? Pokemon { get; set; }

        public static GiveawayPoolEntry CreateUploadEntry(string name, string description, PK8 pkm, string status, string tag, string uploader)
        {
            var entry = new GiveawayPoolEntry();
            entry.Name = name;
            entry.Pokemon = SpeciesName.GetSpeciesName(pkm.Species, (int)LanguageID.English);
            entry.Description = description;
            entry.Status = status;
            entry.Tag = tag;
            entry.Uploader = uploader;
            entry.PK8 = pkm;
            return entry;
        }
        public string GetSummary(bool isItem)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            if (Name != null)
            {
                string summ = Id + ": " + textInfo.ToTitleCase(Name);
                if (!isItem)
                {
                    if (PK8 != null)
                    {
                        if (PK8.OT_Name != null)
                            summ += $" (" + PK8.OT_Name + ") ";
                    }
                    

                    summ += "[" + Tag + "]";
                    if (PK8 != null)
                    {
                        if (PK8.IsShiny)
                            summ += "✷";
                    }
                }
                return summ;
            }
            return "";
        }

    }
}