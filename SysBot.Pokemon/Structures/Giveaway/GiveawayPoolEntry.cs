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
        public string Pool { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Tag { get; set; }
        public string Uploader { get; set; }
        public PK8 PK8 { get; set; }
        public string Pokemon { get; set; }
        public int Count { get; set; }

        public GiveawayPoolEntry()
        {
            Id = 0;
            Pool = "giveawaypool";
            Name = "Default Name";
            Description = "";
            Status = "active";
            Tag = "";
            Uploader = "";
            PK8 = new PK8();
            Pokemon = "";
            Count = 1;
        }
        // For creating a new entry where the ID is not yet known
        public GiveawayPoolEntry(string pool, string name, string tag, string uploader, string description, string status)
        {
            Id = 0;
            Pool = pool;
            Name = name;
            Description = description;
            Status = status;
            Tag = tag;
            Uploader = uploader;
            PK8 = new PK8();
            Pokemon = "";
            Count = 1;
        }
        // Created when looking up an entry from the database
        public GiveawayPoolEntry(int id, string pool, string name, string tag, string uploader, PK8 pkm, string description, string status, string speciesName)
        {
            Id = id;
            Pool = pool;
            Name = name;
            Description = description;
            Status = status;
            Tag = tag;
            Uploader = uploader;
            PK8 = pkm;
            Pokemon = speciesName;
            Count = 1;
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
