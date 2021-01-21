using PKHeX.Core;

namespace SysBot.Pokemon
{
    public class GiveawayRequest<T> where T : PKM, new()
    {
        public readonly string ItemName;
        public readonly T RequestInfo;

        public GiveawayRequest(T requestInfo, string requestName)
        {
            RequestInfo = requestInfo;
            ItemName = requestName;
        }
    }
}