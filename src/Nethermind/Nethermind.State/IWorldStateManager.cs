// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Trie.Pruning;

namespace Nethermind.State;

public interface IWorldStateManager
{
    IWorldState GlobalWorldState { get; }
    IStateReader GlobalStateReader { get; }
    (IWorldState, IStateReader, Action) CreateResettableWorldState();
    event EventHandler<ReorgBoundaryReached>? ReorgBoundaryReached;
}
