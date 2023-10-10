// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Threading;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Specs.Forks
{
    public class Cancun : Shanghai
    {
        private static IReleaseSpec _instance;

        protected Cancun()
        {
            Name = "Cancun";
            IsEip1153Enabled = true;
            IsEip5656Enabled = true;
            IsEip4844Enabled = true;
            IsEip6780Enabled = true;
            IsEip4788Enabled = true;
            Eip4788ContractAddress = Address.FromNumber(0x0b);

            GasPerBlob = 1 << 17;

            MaxBlobGasPerBlock = GasPerBlob * 6;

            MinBlobGasPrice = 1;

            TargetBlobGasPerBlock = GasPerBlob * 3;
        }

        public new static IReleaseSpec Instance => LazyInitializer.EnsureInitialized(ref _instance, () => new Cancun());
    }
}
