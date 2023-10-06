// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Nethermind.Evm;

public static class BlobGasCalculator
{
    public static ulong CalculateBlobGas(int blobCount, IReleaseSpec spec) =>
        (ulong)blobCount * spec.GasPerBlob.Value;

    public static ulong CalculateBlobGas(Transaction transaction, IReleaseSpec spec) =>
        CalculateBlobGas(transaction.BlobVersionedHashes?.Length ?? 0, spec);

    public static ulong CalculateBlobGas(Transaction[] transactions, IReleaseSpec spec)
    {
        int blobCount = 0;
        foreach (Transaction tx in transactions)
        {
            if (tx.SupportsBlobs)
            {
                blobCount += tx.BlobVersionedHashes!.Length;
            }
        }

        return CalculateBlobGas(blobCount, spec);
    }

    public static bool TryCalculateBlobGasPrice(BlockHeader header, Transaction transaction, IReleaseSpec spec, out UInt256 blobGasPrice)
    {
        if (!TryCalculateBlobGasPricePerUnit(header.ExcessBlobGas.Value, spec, out UInt256 blobGasPricePerUnit))
        {
            blobGasPrice = UInt256.MaxValue;
            return false;
        }
        return !UInt256.MultiplyOverflow(CalculateBlobGas(transaction, spec), blobGasPricePerUnit, out blobGasPrice);
    }

    public static bool TryCalculateBlobGasPricePerUnit(BlockHeader header, IReleaseSpec spec, out UInt256 blobGasPricePerUnit)
    {
        blobGasPricePerUnit = UInt256.MaxValue;
        return header.ExcessBlobGas is not null
            && TryCalculateBlobGasPricePerUnit(header.ExcessBlobGas.Value, spec, out blobGasPricePerUnit);
    }

    public static bool TryCalculateBlobGasPricePerUnit(ulong excessBlobGas, IReleaseSpec spec, out UInt256 blobGasPricePerUnit)
    {
        static bool FakeExponentialOverflow(UInt256 factor, UInt256 num, UInt256 denominator, out UInt256 blobGasPricePerUnit)
        {
            UInt256 output = UInt256.Zero;

            if (UInt256.MultiplyOverflow(factor, denominator, out UInt256 numAccum))
            {
                blobGasPricePerUnit = UInt256.MaxValue;
                return true;
            }

            for (UInt256 i = 1; numAccum > 0; i++)
            {
                if (UInt256.AddOverflow(output, numAccum, out output))
                {
                    blobGasPricePerUnit = UInt256.MaxValue;
                    return true;
                }

                if (UInt256.MultiplyOverflow(numAccum, num, out UInt256 updatedNumAccum))
                {
                    blobGasPricePerUnit = UInt256.MaxValue;
                    return true;
                }

                if (UInt256.MultiplyOverflow(i, denominator, out UInt256 multipliedDeniminator))
                {
                    blobGasPricePerUnit = UInt256.MaxValue;
                    return true;
                }

                numAccum = updatedNumAccum / multipliedDeniminator;
            }

            blobGasPricePerUnit = output / denominator;
            return false;
        }

        return !FakeExponentialOverflow(spec.MinBlobGasPrice, excessBlobGas, Eip4844Constants.BlobGasUpdateFraction, out blobGasPricePerUnit);
    }

    public static ulong? CalculateExcessBlobGas(BlockHeader? parentBlockHeader, IReleaseSpec releaseSpec)
    {
        if (!releaseSpec.IsEip4844Enabled)
        {
            return null;
        }

        if (parentBlockHeader is null)
        {
            return 0;
        }

        ulong excessBlobGas = parentBlockHeader.ExcessBlobGas ?? 0;
        excessBlobGas += parentBlockHeader.BlobGasUsed ?? 0;
        return excessBlobGas < releaseSpec.TargetBlobGasPerBlock
            ? 0
            : (excessBlobGas - releaseSpec.TargetBlobGasPerBlock);
    }
}
