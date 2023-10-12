// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Trie;

namespace Nethermind.State;

public interface IState : IDisposable
{
    void Set(Address address, Account? account);

    Account? Get(Address address);

    byte[] GetStorageAt(in StorageCell storageCell);

    void SetStorage(in StorageCell changeStorageCell, byte[] changeValue);

    void Accept(ITreeVisitor treeVisitor, VisitingOptions? visitingOptions = null);

    /// <summary>
    /// Commits the changes.
    /// </summary>
    void Commit(long blockNumber);
}

public interface IStateFactory
{
    IState Get(Keccak stateRoot);
}

public interface IStateOwner
{
    IState State { get; }
}
