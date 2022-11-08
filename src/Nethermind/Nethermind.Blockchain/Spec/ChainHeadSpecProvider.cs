//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using Nethermind.Blockchain.Find;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Blockchain.Spec
{
    public class ChainHeadSpecProvider : IChainHeadSpecProvider
    {
        private readonly ISpecProvider _specProvider;
        private readonly IBlockFinder _blockFinder;
        private long _lastHeader = -1;
        private IReleaseSpec? _headerSpec = null;
        private readonly object _lock = new();

        public ChainHeadSpecProvider(ISpecProvider specProvider, IBlockFinder blockFinder)
        {
            _specProvider = specProvider ?? throw new ArgumentNullException(nameof(specProvider));
            _blockFinder = blockFinder ?? throw new ArgumentNullException(nameof(blockFinder));
        }

        public void UpdateMergeTransitionInfo(long? blockNumber, UInt256? terminalTotalDifficulty = null)
        {
            _specProvider.UpdateMergeTransitionInfo(blockNumber, terminalTotalDifficulty);
        }

        public ForkActivation? MergeBlockNumber => _specProvider.MergeBlockNumber;

        public UInt256? TerminalTotalDifficulty => _specProvider.TerminalTotalDifficulty;

        public IReleaseSpec GenesisSpec => _specProvider.GenesisSpec;

        public IReleaseSpec GetSpec(ForkActivation forkActivation) => _specProvider.GetSpec(forkActivation);

        public long? DaoBlockNumber => _specProvider.DaoBlockNumber;

        public ulong ChainId => _specProvider.ChainId;

        public ForkActivation[] TransitionBlocks => _specProvider.TransitionBlocks;

        public IReleaseSpec GetCurrentHeadSpec()
        {
            BlockHeader? header = _blockFinder.FindBestSuggestedHeader();
            long headerNumber = header?.Number ?? 0;

            // we are fine with potential concurrency issue here, that the spec will change
            // between this if and getting actual header spec
            // this is used only in tx pool and this is not a problem there
            if (headerNumber == _lastHeader)
            {
                IReleaseSpec releaseSpec = _headerSpec;
                if (releaseSpec is not null)
                {
                    return releaseSpec;
                }
            }

            // we want to make sure updates to both fields are consistent though
            lock (_lock)
            {
                _lastHeader = headerNumber;
                if (header is not null)
                    return _headerSpec = _specProvider.GetSpec(header);
                return _headerSpec = GetSpec(headerNumber);
            }
        }
    }
}
