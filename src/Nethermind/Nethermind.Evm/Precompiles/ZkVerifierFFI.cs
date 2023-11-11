using System;
using System.Runtime.InteropServices;

// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Evm.Precompiles;

public class ZkVerifierFFI
{
    [DllImport("verifier", EntryPoint = "verify", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool Verify(byte[] proof, UIntPtr proofLen,
                                     byte[] nullifier, UIntPtr nullifierLen,
                                     byte[] value, UIntPtr valueLen,
                                     byte[] sender, UIntPtr senderLen,
                                     byte[] stateRoot, UIntPtr stateRootLen);
}
