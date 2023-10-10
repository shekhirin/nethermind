// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using FluentAssertions;
using Nethermind.Abi;
using Nethermind.Consensus;
using Nethermind.Consensus.AuRa.Contracts;
using Nethermind.Consensus.AuRa.Transactions;
using Nethermind.Consensus.Transactions;
using Nethermind.Core;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.JsonRpc.Modules.Eth;
using Nethermind.Logging;
using Nethermind.TxPool;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.AuRa.Test.Contract
{
    public class ReportingValidatorContractTests
    {
        private ISpecProvider _specProvider;

        [SetUp]
        public void SetUp()
        {
            _specProvider = Substitute.For<ISpecProvider>();
        }

        [Test]
        public void Should_generate_malicious_transaction()
        {
            ReportingValidatorContract contract = new(_specProvider, AbiEncoder.Instance, new Address("0x1000000000000000000000000000000000000001"), Substitute.For<ISigner>());
            Transaction transaction = contract.ReportMalicious(new Address("0x75df42383afe6bf5194aa8fa0e9b3d5f9e869441"), 10, new byte[0]);
            transaction.Data.AsArray().ToHexString().Should().Be("c476dd4000000000000000000000000075df42383afe6bf5194aa8fa0e9b3d5f9e869441000000000000000000000000000000000000000000000000000000000000000a00000000000000000000000000000000000000000000000000000000000000600000000000000000000000000000000000000000000000000000000000000000");
        }

        [Test]
        public void Should_generate_benign_transaction()
        {
            ReportingValidatorContract contract = new(_specProvider, AbiEncoder.Instance, new Address("0x1000000000000000000000000000000000000001"), Substitute.For<ISigner>());
            Transaction transaction = contract.ReportBenign(new Address("0x75df42383afe6bf5194aa8fa0e9b3d5f9e869441"), 10);
            transaction.Data.AsArray().ToHexString().Should().Be("d69f13bb00000000000000000000000075df42383afe6bf5194aa8fa0e9b3d5f9e869441000000000000000000000000000000000000000000000000000000000000000a");
        }
    }
}
