// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Blockchain.Contracts;
using Nethermind.Core.Specs;

namespace Nethermind.AccountAbstraction.Test.TestContracts
{
    public class TokenPaymaster : Contract
    {
        public TokenPaymaster(ISpecProvider specProvider) : base(specProvider)
        {
        }
    }
}
