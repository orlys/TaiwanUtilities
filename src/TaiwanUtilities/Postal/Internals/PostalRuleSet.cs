// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities.Internals;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

internal readonly struct PostalRuleSet
{
    public readonly int Count;
    public readonly int[] NumberStarts;
    public readonly int[] NumberEnds;
    public readonly byte[] HasLaneConstraint;
    public readonly int[] LaneStarts;
    public readonly int[] LaneEnds;
    public readonly byte[] HasAlleyConstraint;
    public readonly int[] AlleyStarts;
    public readonly int[] AlleyEnds;
    public readonly byte[] EvenOdds;
    public readonly int[] NumberStartSubs;
    public readonly int[] NumberEndSubs;
    public readonly int[] ZipCodeIndices;
    public readonly int[] DeptIndices;
    public readonly int[] OfficeIndices;
    public readonly int[] ScopeIndices;

    public PostalRuleSet(
        int count,
        int[] numberStarts, int[] numberEnds,
        byte[] hasLaneConstraint, int[] laneStarts, int[] laneEnds,
        byte[] hasAlleyConstraint, int[] alleyStarts, int[] alleyEnds,
        byte[] evenOdds,
        int[] numberStartSubs, int[] numberEndSubs,
        int[] zipCodeIndices, int[] deptIndices, int[] officeIndices, int[] scopeIndices)
    {
        Count = count;
        NumberStarts = numberStarts;
        NumberEnds = numberEnds;
        HasLaneConstraint = hasLaneConstraint;
        LaneStarts = laneStarts;
        LaneEnds = laneEnds;
        HasAlleyConstraint = hasAlleyConstraint;
        AlleyStarts = alleyStarts;
        AlleyEnds = alleyEnds;
        EvenOdds = evenOdds;
        NumberStartSubs = numberStartSubs;
        NumberEndSubs = numberEndSubs;
        ZipCodeIndices = zipCodeIndices;
        DeptIndices = deptIndices;
        OfficeIndices = officeIndices;
        ScopeIndices = scopeIndices;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryMatch(int number, int subNumber, int lane, int alley,
        out int zipCodeIdx, out int deptIdx, out int officeIdx, out int scopeIdx)
    {
        int count = Count;
        int i = 0;

#if NET8_0_OR_GREATER
        if (Vector256.IsHardwareAccelerated && count >= 8)
        {
            var vNum = Vector256.Create(number);
            for (; i <= count - 8; i += 8)
            {
                var starts = Vector256.LoadUnsafe(ref NumberStarts[i]);
                var ends   = Vector256.LoadUnsafe(ref NumberEnds[i]);
                uint mask  = (uint)(Vector256.GreaterThanOrEqual(vNum, starts)
                                  & Vector256.LessThanOrEqual(vNum, ends))
                                  .ExtractMostSignificantBits();

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask);
                    int idx = i + bit;
                    if (ScalarVerify(idx, number, subNumber, lane, alley))
                    {
                        zipCodeIdx = ZipCodeIndices[idx];
                        deptIdx    = DeptIndices[idx];
                        officeIdx  = OfficeIndices[idx];
                        scopeIdx   = ScopeIndices[idx];
                        return true;
                    }
                    mask &= mask - 1;
                }
            }
        }
#endif

        for (; i < count; i++)
        {
            if (ScalarVerify(i, number, subNumber, lane, alley))
            {
                zipCodeIdx = ZipCodeIndices[i];
                deptIdx    = DeptIndices[i];
                officeIdx  = OfficeIndices[i];
                scopeIdx   = ScopeIndices[i];
                return true;
            }
        }

        zipCodeIdx = deptIdx = officeIdx = scopeIdx = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ScalarVerify(int i, int number, int subNumber, int lane, int alley)
    {
        // Lane
        if (HasLaneConstraint[i] != 0)
        {
            if (lane < LaneStarts[i] || lane > LaneEnds[i]) return false;
        }
        else if (lane != 0)
        {
            return false;
        }

        // Alley: no constraint means rule covers whole lane (any alley allowed)
        if (HasAlleyConstraint[i] != 0)
        {
            if (alley < AlleyStarts[i] || alley > AlleyEnds[i]) return false;
        }

        // Number range (needed for scalar-only path)
        if (number < NumberStarts[i] || number > NumberEnds[i]) return false;

        // EvenOdd
        byte eo = EvenOdds[i];
        if (eo != 0 && (number & 1) != (eo == 1 ? 1 : 0)) return false;

        // SubNumber
        if (NumberStartSubs[i] > 0 && subNumber < NumberStartSubs[i]) return false;
        if (NumberEndSubs[i] < int.MaxValue && subNumber > NumberEndSubs[i]) return false;

        return true;
    }
}
