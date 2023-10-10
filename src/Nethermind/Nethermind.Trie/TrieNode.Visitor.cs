// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Nethermind.Core;
using Nethermind.Core.Buffers;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Serialization.Rlp;
using Nethermind.Trie.Pruning;

[assembly: InternalsVisibleTo("Ethereum.Trie.Test")]
[assembly: InternalsVisibleTo("Nethermind.Blockchain.Test")]
[assembly: InternalsVisibleTo("Nethermind.Trie.Test")]

namespace Nethermind.Trie
{
    public partial class TrieNode
    {
        private const int BranchesCount = 16;

        /// <summary>
        /// Like `Accept`, but does not execute its children. Instead it return the next trie to visit in the list
        /// `nextToVisit`. Also, it assume the node is already resolved.
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="nodeResolver"></param>
        /// <param name="trieVisitContext"></param>
        /// <param name="currentLevel"></param>
        /// <param name="nextToVisit"></param>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="TrieException"></exception>
        internal void AcceptResolvedNode<TCtx>(
            IGenericTreeVisitor<TCtx> visitor,
            ITrieNodeResolver nodeResolver,
            TCtx trieVisitContext,
            byte currentLevel,
            IList<(TrieNode, TCtx, byte)> nextToVisit
        ) where TCtx : struct
        {
            switch (NodeType)
            {
                case NodeType.Branch:
                    {
                        visitor.VisitBranch(this, trieVisitContext);

                        for (int i = 0; i < BranchesCount; i++)
                        {
                            TrieNode child = GetChild(nodeResolver, i);
                            if (child is not null)
                            {
                                child.ResolveKey(nodeResolver, false);
                                TCtx? childCtx = visitor.ShouldVisitChild(this, trieVisitContext, child, i);
                                if (childCtx != null)
                                {
                                    nextToVisit.Add((child, childCtx.Value, (byte)(currentLevel+1)));
                                }

                                if (child.IsPersisted)
                                {
                                    UnresolveChild(i);
                                }
                            }
                        }

                        break;
                    }
                case NodeType.Extension:
                    {
                        visitor.VisitExtension(this, trieVisitContext);
                        TrieNode child = GetChild(nodeResolver, 0);
                        if (child is null)
                        {
                            throw new InvalidDataException($"Child of an extension {Key} should not be null.");
                        }

                        child.ResolveKey(nodeResolver, false);
                        TCtx? childCtx = visitor.ShouldVisitExtension(this, trieVisitContext, child);
                        if (childCtx != null)
                        {
                            nextToVisit.Add((child, childCtx.Value, (byte)(currentLevel+Key?.Length??0)));
                        }

                        break;
                    }

                case NodeType.Leaf:
                    {
                        visitor.VisitLeaf(this, trieVisitContext);
                        if (currentLevel <= 32 && visitor.ShouldVisitAccount(this, trieVisitContext)) // Or is it 64?
                        {
                            Account account = _accountDecoder.Decode(Value.AsRlpStream());
                            visitor.VisitAccount(this, trieVisitContext, account);

                            if (account.HasStorage)
                            {
                                TCtx? childCtx = visitor.ShouldVisitStorage(this, trieVisitContext, account);

                                if (childCtx != null && TryResolveStorageRoot(nodeResolver, out TrieNode? storageRoot))
                                {
                                    nextToVisit.Add((storageRoot!, childCtx.Value, (byte)(currentLevel + Key?.Length??0)));
                                }
                                else
                                {
                                    visitor.VisitMissingAccount(account, trieVisitContext);
                                }
                            }
                        }

                        break;
                    }

                default:
                    throw new TrieException($"An attempt was made to visit a node {Keccak} of type {NodeType}");
            }
        }

        internal void Accept(ITreeVisitor visitor, ITrieNodeResolver nodeResolver, TrieVisitContext trieVisitContext)
        {
            try
            {
                ResolveNode(nodeResolver);
            }
            catch (TrieException)
            {
                visitor.VisitMissingNode(Keccak, trieVisitContext);
                return;
            }

            ResolveKey(nodeResolver, trieVisitContext.Level == 0);

            switch (NodeType)
            {
                case NodeType.Branch:
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        void VisitChild(int i, TrieNode? child, ITrieNodeResolver resolver, ITreeVisitor v,
                            TrieVisitContext context)
                        {
                            if (child is not null)
                            {
                                child.ResolveKey(resolver, false);
                                if (v.ShouldVisit(child.Keccak!))
                                {
                                    context.BranchChildIndex = i;
                                    child.Accept(v, resolver, context);
                                }

                                if (child.IsPersisted)
                                {
                                    UnresolveChild(i);
                                }
                            }
                        }

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        void VisitSingleThread(ITreeVisitor treeVisitor, ITrieNodeResolver trieNodeResolver,
                            TrieVisitContext visitContext)
                        {
                            // single threaded route
                            for (int i = 0; i < BranchesCount; i++)
                            {
                                VisitChild(i, GetChild(trieNodeResolver, i), trieNodeResolver, treeVisitor, visitContext);
                            }
                        }

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        void VisitMultiThread(ITreeVisitor treeVisitor, ITrieNodeResolver trieNodeResolver,
                            TrieVisitContext visitContext, TrieNode?[] children)
                        {
                            // multithreaded route
                            Parallel.For(0, BranchesCount, i =>
                            {
                                visitContext.Semaphore.Wait();
                                try
                                {
                                    // we need to have separate context for each thread as context tracks level and branch child index
                                    TrieVisitContext childContext = visitContext.Clone();
                                    VisitChild(i, children[i], trieNodeResolver, treeVisitor, childContext);
                                }
                                finally
                                {
                                    visitContext.Semaphore.Release();
                                }
                            });
                        }

                        visitor.VisitBranch(this, trieVisitContext);
                        trieVisitContext.AddVisited();
                        trieVisitContext.Level++;

                        if (trieVisitContext.MaxDegreeOfParallelism != 1 && trieVisitContext.Semaphore.CurrentCount > 1)
                        {
                            // we need to preallocate children
                            TrieNode?[] children = new TrieNode?[BranchesCount];
                            for (int i = 0; i < BranchesCount; i++)
                            {
                                children[i] = GetChild(nodeResolver, i);
                            }

                            if (trieVisitContext.Semaphore.CurrentCount > 1)
                            {
                                VisitMultiThread(visitor, nodeResolver, trieVisitContext, children);
                            }
                            else
                            {
                                VisitSingleThread(visitor, nodeResolver, trieVisitContext);
                            }
                        }
                        else
                        {
                            VisitSingleThread(visitor, nodeResolver, trieVisitContext);
                        }

                        trieVisitContext.Level--;
                        trieVisitContext.BranchChildIndex = null;
                        break;
                    }

                case NodeType.Extension:
                    {
                        visitor.VisitExtension(this, trieVisitContext);
                        trieVisitContext.AddVisited();
                        TrieNode child = GetChild(nodeResolver, 0);
                        if (child is null)
                        {
                            throw new InvalidDataException($"Child of an extension {Key} should not be null.");
                        }

                        child.ResolveKey(nodeResolver, false);
                        if (visitor.ShouldVisit(child.Keccak!))
                        {
                            trieVisitContext.Level++;
                            trieVisitContext.BranchChildIndex = null;
                            child.Accept(visitor, nodeResolver, trieVisitContext);
                            trieVisitContext.Level--;
                        }

                        break;
                    }

                case NodeType.Leaf:
                    {
                        visitor.VisitLeaf(this, trieVisitContext, Value.ToArray());
                        trieVisitContext.AddVisited();
                        if (!trieVisitContext.IsStorage && trieVisitContext.ExpectAccounts) // can combine these conditions
                        {
                            Account account = _accountDecoder.Decode(Value.AsRlpStream());
                            if (account.HasCode && visitor.ShouldVisit(account.CodeHash))
                            {
                                trieVisitContext.Level++;
                                trieVisitContext.BranchChildIndex = null;
                                visitor.VisitCode(account.CodeHash, trieVisitContext);
                                trieVisitContext.Level--;
                            }

                            if (account.HasStorage && visitor.ShouldVisit(account.StorageRoot))
                            {
                                trieVisitContext.IsStorage = true;
                                trieVisitContext.Level++;
                                trieVisitContext.BranchChildIndex = null;

                                if (TryResolveStorageRoot(nodeResolver, out TrieNode? storageRoot))
                                {
                                    storageRoot!.Accept(visitor, nodeResolver, trieVisitContext);
                                }
                                else
                                {
                                    visitor.VisitMissingNode(account.StorageRoot, trieVisitContext);
                                }

                                trieVisitContext.Level--;
                                trieVisitContext.IsStorage = false;
                            }
                        }

                        break;
                    }

                default:
                    throw new TrieException($"An attempt was made to visit a node {Keccak} of type {NodeType}");
            }
        }

        internal void Accept(ITreeLeafVisitor visitor, ITrieNodeResolver nodeResolver, bool parallel)
        {
            if (parallel)
            {
                ResolveNode(nodeResolver);
                if (NodeType == NodeType.Branch)
                {
                    TrieNode?[] children = new TrieNode?[BranchesCount];
                    for (byte i = 0; i < BranchesCount; i++)
                    {
                        children[i] = GetChild(nodeResolver, i);
                    }

                    Parallel.ForEach(children, (child, state, index) =>
                    {
                        byte[] nibbles = new byte[ValueKeccak.MemorySize * 2];
                        nibbles[0] = (byte)index;
                        Visit(child, nodeResolver, visitor, 1, nibbles, null);
                    });

                    return;
                }
            }

            byte[] address = new byte[ValueKeccak.MemorySize * 2];
            Visit(this, nodeResolver, visitor, 0, address, null);

            static void Visit(TrieNode node, ITrieNodeResolver resolver, ITreeLeafVisitor visitor, int depth, byte[] nibbles, Keccak? account)
            {
                node.ResolveNode(resolver);

                Span<byte> path;
                switch (node.NodeType)
                {
                    case NodeType.Branch:
                        for (byte i = 0; i < BranchesCount; i++)
                        {
                            TrieNode? child = node.GetChild(resolver, i);
                            if (child != null)
                            {
                                nibbles[depth] = i;
                                Visit(child, resolver, visitor, depth + 1, nibbles, account);
                            }
                        }
                        break;
                    case NodeType.Extension:
                        TrieNode branch = node.GetChild(resolver, 0);
                        path = node.Key.AsSpan();
                        path.CopyTo(nibbles.Slice(depth));

                        Debug.Assert(branch != null);

                        Visit(branch, resolver, visitor, depth + path.Length, nibbles, account);
                        break;
                    case NodeType.Leaf:
                        path = node.Key.AsSpan();
                        Span<byte> destination = nibbles.AsSpan(depth);

                        Debug.Assert(path.Length == destination.Length);

                        path.CopyTo(destination);

                        ValueKeccak keccak = new(Nibbles.ToBytes(nibbles));

                        if (account != null)
                        {
                            // this is a storage
                            visitor.VisitLeafStorage(account, keccak, node.Value);
                        }
                        else
                        {
                            Account a = Rlp.Decode<Account>(node.Value.AsRlpStream());
                            visitor.VisitLeafAccount(keccak, a);

                            if (a.HasStorage)
                            {
                                TrieNode storageRoot = resolver.FindCachedOrUnknown(a.StorageRoot);
                                Visit(storageRoot, resolver, visitor, 0, new byte[ValueKeccak.MemorySize * 2],
                                    keccak.ToKeccak());
                            }
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
