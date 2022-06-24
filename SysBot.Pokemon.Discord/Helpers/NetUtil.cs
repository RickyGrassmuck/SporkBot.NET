﻿using Discord;
using PKHeX.Core;
using System.Net.Http;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    public static class NetUtil
    {
        private static readonly HttpClient webClient = new();

        public static async Task<byte[]> DownloadFromUrlAsync(string url)
        {
            return await webClient.GetByteArrayAsync(url).ConfigureAwait(false);
        }

        public static async Task<Download<PKM>> DownloadPKMAsync(IAttachment att)
        {
            var result = new Download<PKM> { SanitizedFileName = Format.Sanitize(att.Filename) };
            if (!EntityDetection.IsSizePlausible(att.Size))
            {
                result.ErrorMessage = $"{result.SanitizedFileName}: Invalid size.";
                return result;
            }

            string url = att.Url;

            // Download the resource and load the bytes into a buffer.
            var buffer = await DownloadFromUrlAsync(url).ConfigureAwait(false);
            var pkm = EntityFormat.GetFromBytes(buffer, result.SanitizedFileName.Contains("pk6") ? 6 : 7);
            if (pkm == null)
            {
                result.ErrorMessage = $"{result.SanitizedFileName}: Invalid pkm attachment.";
                return result;
            }

            result.Data = pkm;
            result.Success = true;
            return result;
        }
    }

    public sealed class Download<T> where T : class
    {
        public bool Success;
        public T? Data;
        public string? SanitizedFileName;
        public string? ErrorMessage;
    }
}