using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace SysBot.Pokemon
{
    public class GiveawaySettings : IGiveaway
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string Files = nameof(Files);
        public override string ToString() => "Giveaway Settings";

        [Category(FeatureToggle), Description("When enabled, allows uploading to the giveaway pool")]
        public bool GiveawayUpload { get; set; }

        [Category(Files), Description("GiveawayFolder folder: parent directory which houses both inactive and active giveaway pool directories.")]
        public string GiveawayFolder { get; set; } = string.Empty;
        public void CreateDefaults(string path)
        {
            var giveawayPaths = new List<string> { "active", "inactive", "upload" };
            var giveaway = Path.Combine(path, "giveaway");

            foreach (string subdir in giveawayPaths)
            {
                var dirPath = Path.Combine(giveaway, subdir);
                Directory.CreateDirectory(dirPath);
            }
            
            GiveawayFolder = giveaway;


        }
    }
}