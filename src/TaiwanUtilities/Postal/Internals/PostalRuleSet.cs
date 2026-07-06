// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities.Internals;

using System.Numerics;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

/// <summary>
/// 一組投遞規則的視圖（8 bytes）：指向 PostalData 全域 SoA 陣列的切片。
/// 規則資料本體存放於 PE .rdata（primitive array initializer → RVA blob），
/// 此結構不持有任何陣列。
/// </summary>
internal readonly struct PostalRuleSet
{
    public readonly int Offset;
    public readonly int Count;

    public PostalRuleSet(int offset, int count)
    {
        Offset = offset;
        Count = count;
    }

    // RuleFlags 位元佈局：bit0 = HasLane, bit1 = HasAlley, bits2-3 = EvenOdd(0=不限,1=單,2=雙)
    private const byte FlagLane  = 1;
    private const byte FlagAlley = 2;
    private const int  EvenOddShift = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasLane(int i)      => (PostalData.RuleFlags[Offset + i] & FlagLane) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasAlley(int i)     => (PostalData.RuleFlags[Offset + i] & FlagAlley) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  EvenOdd(int i)      => PostalData.RuleFlags[Offset + i] >> EvenOddShift;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  NumberStart(int i)  => PostalData.NumberStarts[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  NumberEnd(int i)    => PostalData.NumberEnds[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  LaneStart(int i)    => PostalData.LaneStarts[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  LaneEnd(int i)      => PostalData.LaneEnds[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  AlleyStart(int i)   => PostalData.AlleyStarts[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  AlleyEnd(int i)     => PostalData.AlleyEnds[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  SubStart(int i)     => PostalData.SubStarts[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  SubEnd(int i)       => PostalData.SubEnds[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  ZipCodeIndex(int i) => PostalData.ZipIdx[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  DeptIndex(int i)    => PostalData.DeptIdx[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  OfficeIndex(int i)  => PostalData.OfficeIdx[Offset + i];
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int  ScopeIndex(int i)   => PostalData.ScopeIdx[Offset + i];

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
                var starts = Vector256.LoadUnsafe(ref PostalData.NumberStarts[Offset + i]);
                var ends   = Vector256.LoadUnsafe(ref PostalData.NumberEnds[Offset + i]);
                uint mask  = (uint)(Vector256.GreaterThanOrEqual(vNum, starts)
                                  & Vector256.LessThanOrEqual(vNum, ends))
                                  .ExtractMostSignificantBits();

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask);
                    int idx = i + bit;
                    if (ScalarVerify(idx, number, subNumber, lane, alley))
                    {
                        zipCodeIdx = ZipCodeIndex(idx);
                        deptIdx    = DeptIndex(idx);
                        officeIdx  = OfficeIndex(idx);
                        scopeIdx   = ScopeIndex(idx);
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
                zipCodeIdx = ZipCodeIndex(i);
                deptIdx    = DeptIndex(i);
                officeIdx  = OfficeIndex(i);
                scopeIdx   = ScopeIndex(i);
                return true;
            }
        }

        zipCodeIdx = deptIdx = officeIdx = scopeIdx = -1;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ScalarVerify(int i, int number, int subNumber, int lane, int alley)
    {
        byte flags = PostalData.RuleFlags[Offset + i];

        // Lane
        if ((flags & FlagLane) != 0)
        {
            if (lane < LaneStart(i) || lane > LaneEnd(i)) return false;
        }
        else if (lane != 0)
        {
            return false;
        }

        // Alley: no constraint means rule covers whole lane (any alley allowed)
        if ((flags & FlagAlley) != 0)
        {
            if (alley < AlleyStart(i) || alley > AlleyEnd(i)) return false;
        }

        // Number range (needed for scalar-only path)
        if (number < NumberStart(i) || number > NumberEnd(i)) return false;

        // EvenOdd
        int eo = flags >> EvenOddShift;
        if (eo != 0 && (number & 1) != (eo == 1 ? 1 : 0)) return false;

        // SubNumber
        if (SubStart(i) > 0 && subNumber < SubStart(i)) return false;
        if (SubEnd(i) < int.MaxValue && subNumber > SubEnd(i)) return false;

        return true;
    }
}
