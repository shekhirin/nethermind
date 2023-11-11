// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Evm.Precompiles;

public class ZkWormhole : IPrecompile
{
    public static Address Address { get; } = new("4200000000000000000000000000000000000000");
    public static readonly ZkWormhole Instance = new();

    public long BaseGasCost(IReleaseSpec releaseSpec)
    {
        return 0;
    }

    public long DataGasCost(in ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec)
    {
        return 0;
    }

    public bool VerifyProof(byte[] proof, UInt256 nullifier, UInt256 value, Address sender, Hash256 stateRoot)
    {
        byte[] nullifierEncoded = nullifier.ToLittleEndian();
        byte[] valueEncoded = value.ToLittleEndian();
        byte[] senderEncoded = sender.Bytes;
        byte[] stateRootEncoded = stateRoot.BytesToArray();
        return ZkVerifierFFI.Verify(proof, (UIntPtr)proof.Length,
                                    nullifierEncoded, (UIntPtr)nullifierEncoded.Length,
                                    valueEncoded, (UIntPtr)valueEncoded.Length,
                                    senderEncoded, (UIntPtr)senderEncoded.Length,
                                    stateRootEncoded, (UIntPtr)stateRootEncoded.Length);
    }

    public (ReadOnlyMemory<byte>, bool) Run(in ReadOnlyMemory<byte> inputData, IReleaseSpec releaseSpec)
    {
        throw new NotImplementedException();
    }
}
