// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Ethereum.Test.Base;
using NUnit.Framework;

namespace Ethereum.Blockchain.Block.Test;

public class Eip4844Tests : BlockchainTestBase
{
    [TestCaseSource(nameof(LoadTests))]
    public async Task Test(BlockchainTest test)
    {
        await RunTest(test);
    }

    public static IEnumerable<BlockchainTest> LoadTests()
    {
        var loader = new TestsSourceLoader(new LoadBlockchainTestsStrategy(), "bcEIP4844");
        return (IEnumerable<BlockchainTest>)loader.LoadTests();
    }
}