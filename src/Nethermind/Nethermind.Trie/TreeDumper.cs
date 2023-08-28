// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.IO;
using System.Text;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Logging;
using Nethermind.Serialization.Rlp;

namespace Nethermind.Trie
{
    public class TreeDumper : ITreeVisitor
    {
        public string FileName = "chiadoDump";
        private SimpleConsoleLogger _logger => SimpleConsoleLogger.Instance;

        private bool CollectLeafs(byte[] rootHash, byte[] key, byte[] value, bool isStorage)
        {
            string leafDescription = isStorage ? "LEAF " : "ACCOUNT ";
            _logger.Info($"COLLECTING {leafDescription}");
            if (isStorage) key = key[64..];
            File.AppendAllLines($"/root/chiadoDump/{FileName}.txt", new []{$"{rootHash.ToHexString()}:{Nibbles.ToBytes(key).ToHexString()}:{value.ToHexString()}"});
            return true;
        }

        public void Reset()
        {
        }

        public bool IsFullDbScan => true;

        public bool ShouldVisit(Keccak nextNode)
        {
            return true;
        }

        public void VisitTree(Keccak rootHash, TrieVisitContext trieVisitContext)
        {
            if (rootHash == Keccak.EmptyTreeHash)
            {
                _logger.Info("EMPTY TREEE");
            }
            else
            {
                _logger.Info(trieVisitContext.IsStorage ? "STORAGE TREE" : "STATE TREE");
            }
        }

        private string GetPrefix(TrieVisitContext context) => string.Concat($"{GetIndent(context.Level)}", context.IsStorage ? "STORAGE " : string.Empty, $"{GetChildIndex(context)}");

        private string GetIndent(int level) => new('+', level * 2);
        private string GetChildIndex(TrieVisitContext context) => context.BranchChildIndex is null ? string.Empty : $"{context.BranchChildIndex:x2} ";

        public void VisitMissingNode(Keccak nodeHash, TrieVisitContext trieVisitContext)
        {
            _logger.Info($"{GetIndent(trieVisitContext.Level)}{GetChildIndex(trieVisitContext)}MISSING {nodeHash}");
            throw new ArgumentException("node not found");
        }

        public void VisitBranch(TrieNode node, TrieVisitContext trieVisitContext)
        {
            // _builder.AppendLine($"{GetPrefix(trieVisitContext)}BRANCH | -> {KeccakOrRlpStringOfNode(node)}");
        }

        public void VisitExtension(TrieNode node, TrieVisitContext trieVisitContext)
        {
            // _builder.AppendLine($"{GetPrefix(trieVisitContext)}EXTENSION {Nibbles.FromBytes(node.Key).ToPackedByteArray().ToHexString(false)} -> {KeccakOrRlpStringOfNode(node)}");
        }

        private AccountDecoder decoder = new();

        public void VisitLeaf(TrieNode node, TrieVisitContext trieVisitContext, byte[] value = null)
        {
            CollectLeafs(trieVisitContext.RootHash.Bytes.ToArray(), trieVisitContext.AbsolutePathNibbles.ToArray(), value, trieVisitContext.IsStorage);
        }

        public void VisitCode(Keccak codeHash, TrieVisitContext trieVisitContext)
        {
            // _builder.AppendLine($"{GetPrefix(trieVisitContext)}CODE {codeHash}");
        }

        public override string ToString()
        {
            return "_builder.ToString()";
        }

        private string? KeccakOrRlpStringOfNode(TrieNode node)
        {
            return node.Keccak != null ? node.Keccak!.Bytes.ToHexString() : node.FullRlp?.ToHexString();
        }
    }
}
