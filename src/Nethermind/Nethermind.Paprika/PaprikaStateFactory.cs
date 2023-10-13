// SPDX-FileCopyrightText: 2023 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.State;
using Nethermind.Trie;
using Nethermind.Trie.Pruning;
using Paprika.Chain;
using Paprika.Merkle;
using Paprika.Store;
using IWorldState = Paprika.Chain.IWorldState;
using PaprikaKeccak = global::Paprika.Crypto.Keccak;

namespace Nethermind.Paprika;

public class PaprikaStateFactory : IStateFactory
{
    private static readonly ulong _sepolia = (ulong)32.GiB();
    private static readonly TimeSpan _flushFileEvery = TimeSpan.FromSeconds(10);

    private readonly PagedDb _db;
    private readonly Blockchain _blockchain;

    public PaprikaStateFactory(string directory)
    {
        _db = PagedDb.MemoryMappedDb(_sepolia, 64, directory, true);
        _blockchain = new Blockchain(_db, new ComputeMerkleBehavior(true, 2, 2), _flushFileEvery);
    }

    public IState Get(Keccak stateRoot)
    {
        return new State(_blockchain.StartNew(Convert(stateRoot)));
    }

    public bool HasRoot(Keccak stateRoot)
    {
        throw new NotImplementedException();
    }

    public event EventHandler<ReorgBoundaryReached>? ReorgBoundaryReached;

    public async ValueTask DisposeAsync()
    {
        await _blockchain.DisposeAsync();
        _db.Dispose();
    }

    private static PaprikaKeccak Convert(Keccak keccak) => new(keccak.Bytes);
    private static PaprikaKeccak Convert(in ValueKeccak keccak) => new(keccak.Bytes);
    private static Keccak Convert(PaprikaKeccak keccak) => new(keccak.BytesAsSpan);
    private static PaprikaKeccak Convert(Address address) => Convert(ValueKeccak.Compute(address.Bytes));

    class State : IState
    {
        private readonly IWorldState _wrapped;

        public State(IWorldState wrapped)
        {
            _wrapped = wrapped;
        }

        public void Set(Address address, Account? account)
        {
            _wrapped.SetAccount(Convert(address), account);
        }

        public Account? Get(Address address)
        {
            throw new NotImplementedException();
        }

        public byte[] GetStorageAt(in StorageCell storageCell)
        {
            throw new NotImplementedException();
        }

        public void SetStorage(in StorageCell storageCell, byte[] changeValue)
        {
            throw new NotImplementedException();
        }

        public void Accept(ITreeVisitor treeVisitor, VisitingOptions? visitingOptions = null)
        {
            throw new NotImplementedException();
        }

        public void Commit(long blockNumber) => _wrapped.Commit((uint)blockNumber);

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Keccak StateRoot { get; }

        public void Dispose()
        {
            _wrapped.Dispose();
        }
    }
}
