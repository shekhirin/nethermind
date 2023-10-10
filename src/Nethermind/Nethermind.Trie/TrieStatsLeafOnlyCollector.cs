// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Logging;

namespace Nethermind.Trie
{
    public class TrieStatsLeafOnlyCollector : ITreeLeafVisitor
    {
        private int _lastAccountNodeCount = 0;

        private readonly ILogger _logger;

        public TrieStatsLeafOnlyCollector(ILogManager logManager)
        {
            _logger = logManager.GetClassLogger();
        }

        public TrieStats Stats { get; } = new();

        public void VisitLeafAccount(in ValueKeccak account, Account value)
        {
            if (Stats.NodesCount - _lastAccountNodeCount > 1_000_000)
            {
                _lastAccountNodeCount = Stats.NodesCount;
                _logger.Warn($"Collected info from {Stats.NodesCount} nodes. Missing CODE {Stats.MissingCode} STATE {Stats.MissingState} STORAGE {Stats.MissingStorage}");

            }

            Interlocked.Increment(ref Stats._accountCount);
        }

        public void VisitLeafStorage(in ValueKeccak account, in ValueKeccak storage, ReadOnlySpan<byte> value)
        {
            Interlocked.Add(ref Stats._storageSize, value.Length);
            Interlocked.Increment(ref Stats._storageLeafCount);
        }
    }
}
