﻿using FluentAssertions;
using PKHeX.Core;
using SysBot.Pokemon;
using Xunit;

namespace SysBot.Tests
{
    public class PokeHubTests
    {
        [Fact]
        public void TestHub()
        {
            var cfg = new PokeTradeHubConfig { Distribution = { DistributeWhileIdle = true } };
            var hub = new PokeTradeHub<PK8>(cfg);

            var trade = hub.Queues.TryDequeue(PokeRoutineType.FlexTrade, out _, out _);
            trade.Should().BeFalse();

        }
    }
}