// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Abi;
using System;
using Nethermind.Blockchain.Contracts;
using Nethermind.Core.Specs;
using Nethermind.Core;
using Nethermind.Evm.TransactionProcessing;

namespace Nethermind.AccountAbstraction.Test.TestContracts
{
    public class SingletonFactory : Contract
    {
        public SingletonFactory(ISpecProvider specProvider) : base(specProvider)
        {
        }
    }
}
