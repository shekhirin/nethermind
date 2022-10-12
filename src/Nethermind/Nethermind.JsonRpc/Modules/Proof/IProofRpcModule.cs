//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
//
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using Nethermind.Blockchain;
using Nethermind.Blockchain.Find;
using Nethermind.Core.Crypto;
using Nethermind.JsonRpc.Data;

namespace Nethermind.JsonRpc.Modules.Proof
{
    /// <summary>
    /// Allows to retrieve transaction, call and state data alongside the merkle proofs / witnesses.
    /// </summary>
    [RpcModule(ModuleType.Proof)]
    public interface IProofRpcModule : IRpcModule
    {
        [JsonRpcMethod(IsImplemented = false, Description = "This function returns the same result as `eth_getTransactionByHash` and also a tx proof and a serialized block header.", IsSharable = false)]
        ResultWrapper<CallResultWithProof> proof_call(TransactionForRpc tx, BlockParameter blockParameter);

        [JsonRpcMethod(IsImplemented = true,
            Description = "This function returns the same result as `eth_getTransactionReceipt` and also a tx proof, receipt proof and serialized block headers.",
            IsSharable = false,
            ExampleResponse = "\"transaction\":{\"hash\":\"0xb62594c08de66c683fbffe44792a1ccc0f9b80e43071048ed03c18a71fd3c19a\",\"nonce\":\"0x630\",\"blockHash\":\"0x42d72739c2b2659916d7b42a49661fdec317e780af1395c2c15aa89b4c42e220\",\"blockNumber\":\"0x88f194\",\"transactionIndex\":\"0x24\",\"from\":\"0x78ca86e8133ef9368b4537879cf2f38fddbb636b\",\"to\":\"0x1dfd95eb75a7486945d366a0bc0b937f0aaa526f\",\"value\":\"0x0\",\"gasPrice\":\"0x3b9aca00\",\"gas\":\"0xc9e2\",\"data\":\"0xa9059cbb000000000000000000000000e3ac1cc1453e70f80ff58f3bb56b0532238ae24a00000000000000000000000000000000000000000000003635c9adc5dea00000\",\"input\":\"0xa9059cbb000000000000000000000000e3ac1cc1453e70f80ff58f3bb56b0532238ae24a00000000000000000000000000000000000000000000003635c9adc5dea00000\",\"type\":\"0x0\",\"v\":\"0x2b\",\"s\":\"0x33a9425e84bf310d372a9f531b237baebccfdd2b426e817cc9553355a9165342\",\"r\":\"0xe14a066de4787a4c0192f5a2285fd835a85baa3a4f63b1e8a2d8d7f6e04425ca\"},\"txProof\":[\"0xf891a0311d3b27b7612bf40c2c5d623c62c2afe30a47f486700074e4c4d7cf603c90c8a0cd64d350a95e9286a580a75ae11fe58801992f9ac65ace8a0b853d16f87b09b0a0ae9d609ff06d19bb911d7ad05cfdd6c80a9f1fddccbdb76a78594536122345ce8080808080a09773b23452983c0ed65aebb64522af322967c62be34414e16b32b7e4bdaecdb68080808080808080\",\"0xf8b1a0715f91aae7675a1c8469685d18bc94241d275c82a3b52df6c4fab064fcba3017a0e77ac7615c08eaafccc876956f3dad1892f08c1f1128e2cdf9064664381a540fa06f2d934e5f7995657144ad66b8b5cdce6b6c141422f95d44eb91ca6765d4f819a0b265c005bad056db029945b3d68a631b624a77703733fa9b2042c0f211f8ef4ea0bb97f719cc5f6082fe5bab8588dc564a843a6b40c5494982ded868f19eef07b6808080808080808080808080\",\"0xf8af20b8acf8aa820630843b9aca0082c9e2941dfd95eb75a7486945d366a0bc0b937f0aaa526f80b844a9059cbb000000000000000000000000e3ac1cc1453e70f80ff58f3bb56b0532238ae24a00000000000000000000000000000000000000000000003635c9adc5dea000002ba0e14a066de4787a4c0192f5a2285fd835a85baa3a4f63b1e8a2d8d7f6e04425caa033a9425e84bf310d372a9f531b237baebccfdd2b426e817cc9553355a9165342\"]")]
        ResultWrapper<TransactionWithProof> proof_getTransactionByHash([JsonRpcParameter(ExampleValue = "\"[\"0xb62594c08de66c683fbffe44792a1ccc0f9b80e43071048ed03c18a71fd3c19a\", \"false\"]")] Keccak txHash, bool includeHeader);

        [JsonRpcMethod(IsImplemented = true,
            Description = "This function should return the same result as `eth_call` and also proofs of all USED accounts and their storages and serialized block headers",
            IsSharable = false,
            ExampleResponse = "{\"receipt\":{\"transactionHash\":\"0xfff473e0d10e9dcc18bb4585fb2ba17f682949996f5dfda41c20c425a53b4e71\",\"transactionIndex\":\"0x0\",\"blockHash\":\"0x539822db4041dac07f02819b1337f5f9d7291a996f80d9c05ada334c7a97264c\",\"blockNumber\":\"0x1\",\"cumulativeGasUsed\":\"0x0\",\"gasUsed\":\"0x0\",\"to\":null,\"contractAddress\":null,\"logs\":[],\"logsBloom\":\"0x00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000\",\"status\":\"0x0\",\"type\":\"0x0\"},\"txProof\":[\"0xf851a073ff16e6f3a3ca20ba99ad5bacc973e800ba7ec7092266fcd2520703613e3d9580808080808080a0a70de17dcf5a91c1b986463b4e8419665333b2a66e66f7127baae3d4d58d052d8080808080808080\",\"0xf86530b862f86080018252089400000000000000000000000000000000000000000181801ca0b4e030f395ed357d206b58d9a0ded408589a9e26f1a5b41010772cd0d84b8d16a04d9797a972bc308ea635f22455881c41c7c9fb946c93db6f99d2bd529675af13\"],\"receiptProof\":[\"0xf851a08e4cd3def722e9727e505d3798454165d832e1aabd5c56e5d0e4e9f0796a783280808080808080a05380738598f169c9e407a0f61558e53ea59a4c5e643aabc57679c7c0a3b761428080808080808080\",\"0xf9012f30b9012bf90128a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421825208b9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000c0\"],\"blockHeader\":\"0xf901f9a0b3157bcccab04639f6393042690a6c9862deebe88c781f911e8dfd265531e9ffa01dcc4de8dec75d7aab85b567b6ccd41ad312451b948a7413f0a142fd40d49347940000000000000000000000000000000000000000a056e81f171bcc55a6ff8345e692c0f86e5b48e01b996cadc001622fb5e363b421a0541c8844bd420f79a5f7f8db723e2106160d350043de7cf76d78ea13ed0ff6c9a0e1b1585a222beceb3887dc6701802facccf186c2d0f6aa69e26ae0c431fc2b5db9010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000830f424001833d090080830f424183010203a02ba5557a4c62a513c7e56d1bf13373e0da6bec016755483e91589fe1c6d212e28800000000000003e8\"}")]

        ResultWrapper<ReceiptWithProof> proof_getTransactionReceipt([JsonRpcParameter(ExampleValue = "[\"0xfff473e0d10e9dcc18bb4585fb2ba17f682949996f5dfda41c20c425a53b4e71\", \"true\"]")] Keccak txHash, bool includeHeader);
    }
}
