// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Db;
using Nethermind.Int256;
using Nethermind.Logging;
using Nethermind.Trie;
using Metrics = Nethermind.Db.Metrics;

namespace Nethermind.State
{
    public class StateReader : IStateReader
    {
        private readonly IStateFactory _factory;
        private readonly IDb _codeDb;
        private readonly ILogger _logger;

        public StateReader(IStateFactory factory, IDb? codeDb, ILogManager? logManager)
        {
            _logger = logManager?.GetClassLogger<StateReader>() ?? throw new ArgumentNullException(nameof(logManager));
            _factory = factory;
            _codeDb = codeDb ?? throw new ArgumentNullException(nameof(codeDb));
        }

        public Account? GetAccount(Keccak stateRoot, Address address)
        {
            return GetState(stateRoot, address);
        }

        public byte[]? GetStorage(Keccak stateRoot, Address address, in UInt256 index)
        {
            Metrics.StorageTreeReads++;

            using IReadOnlyState state = GetReadOnlyState(stateRoot);
            return state.GetStorageAt(new StorageCell(address, index));
        }

        private IReadOnlyState GetReadOnlyState(Keccak stateRoot) => _factory.GetReadOnly(stateRoot);

        public UInt256 GetBalance(Keccak stateRoot, Address address)
        {
            return GetState(stateRoot, address)?.Balance ?? UInt256.Zero;
        }

        public byte[]? GetCode(Keccak codeHash)
        {
            if (codeHash == Keccak.OfAnEmptyString)
            {
                return Array.Empty<byte>();
            }

            return _codeDb[codeHash.Bytes];
        }

        public void RunTreeVisitor(ITreeVisitor treeVisitor, Keccak rootHash, VisitingOptions? visitingOptions = null)
        {
            if (treeVisitor is RootCheckVisitor rootCheck)
            {
                rootCheck.HasRoot = _factory.HasRoot(rootHash);
            }

            throw new NotImplementedException($"The type of visitor {treeVisitor.GetType()} is not handled now");
        }

        public byte[]? GetCode(Keccak stateRoot, Address address)
        {
            Account? account = GetState(stateRoot, address);
            return account is null ? Array.Empty<byte>() : GetCode(account.CodeHash);
        }

        private Account? GetState(Keccak stateRoot, Address address)
        {
            if (stateRoot == Keccak.EmptyTreeHash)
            {
                return null;
            }

            Metrics.StateTreeReads++;

            using IReadOnlyState state = GetReadOnlyState(stateRoot);
            return state.Get(address);
        }
    }
}
