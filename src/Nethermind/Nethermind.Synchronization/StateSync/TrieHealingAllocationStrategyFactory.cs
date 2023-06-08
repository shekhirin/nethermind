﻿// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Stats;
using Nethermind.Synchronization.FastSync;
using Nethermind.Synchronization.ParallelSync;
using Nethermind.Synchronization.Peers;
using Nethermind.Synchronization.Peers.AllocationStrategies;

namespace Nethermind.Synchronization.StateSync;

public class TrieHealingAllocationStrategyFactory : StaticPeerAllocationStrategyFactory<StateSyncBatch>
{
    private static readonly IPeerAllocationStrategy DefaultStrategy =
        new AllocationStrategy(
            new TotalDiffStrategy(
                new BySpeedStrategy(TransferSpeedType.NodeData, true), TotalDiffStrategy.TotalDiffSelectionType.CanBeSlightlyWorse));

    public TrieHealingAllocationStrategyFactory() : base(DefaultStrategy)
    {
    }

    public class AllocationStrategy : FilterPeerAllocationStrategy
    {
        public AllocationStrategy(IPeerAllocationStrategy strategy) : base(strategy)
        {
        }

        protected override bool Filter(PeerInfo peerInfo) => peerInfo.CanGetNodeData();
    }
}
