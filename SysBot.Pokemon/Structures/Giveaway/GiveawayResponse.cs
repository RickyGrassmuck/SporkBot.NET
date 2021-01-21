using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveawayResponse<T> where T : PKM, new()
    {
        public T Receive { get; }
        public GiveawayResponseType Type { get; }

        public GiveawayResponse(T pk, GiveawayResponseType type)
        {
            Receive = pk;
            Type = type;
        }
    }
}