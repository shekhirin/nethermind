// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;

namespace Nethermind.State;

/// <summary>
/// The actual state accessor that is both: disposable when no longer needed and commitable with <see cref="Commit"/>.
/// </summary>
public interface IState : IDisposable
{
    void Set(Address address, Account? account);

    Account? Get(Address address);

    byte[] GetStorageAt(in StorageCell storageCell);

    void SetStorage(in StorageCell storageCell, byte[] changeValue);

    void Accept(ITreeVisitor treeVisitor, VisitingOptions? visitingOptions = null);

    /// <summary>
    /// Commits the changes.
    /// </summary>
    void Commit(long blockNumber);

    /// <summary>
    /// Resets all the changes.
    /// </summary>
    void Reset();

    Keccak StateRoot { get; }
}

/// <summary>
/// The factory allowing to get a state at the given keccak.
/// </summary>
public interface IStateFactory : IDisposable
{
    IState Get(Keccak stateRoot);

    bool HasRoot(Keccak stateRoot);

    event EventHandler<ReorgBoundaryReached>? ReorgBoundaryReached;
}

public interface IStateOwner
{
    IState State { get; }
}
