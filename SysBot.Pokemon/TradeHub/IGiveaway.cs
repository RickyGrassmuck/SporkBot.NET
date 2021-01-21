namespace SysBot.Pokemon
{
    public interface IGiveaway
    {
        bool GiveawayUpload { get; set; }
        string GiveawayFolder { get; set; }
    }
}