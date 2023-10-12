// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;

namespace Nethermind.State;

interface IState
{
    void Set(Address address, Account? account);

    Account? Get(Address address);

    byte[] GetStorageAt(in StorageCell storageCell);

    void SetStorage(in StorageCell changeStorageCell, byte[] changeValue);
}

interface IStateOwner
{
    IState State { get; }
}
