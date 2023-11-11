using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

namespace Nethermind.Evm.Precompiles;

public class ZkVerifierFFI
{
    [DllImport("ckzg", EntryPoint = "free_trusted_setup_wrap", CallingConvention = CallingConvention.Cdecl)]
    private static extern void InternalFreeTrustedSetup(IntPtr ts);
}
