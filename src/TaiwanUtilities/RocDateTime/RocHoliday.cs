namespace TaiwanUtilities;

using System;

/// <summary>
/// 假日適用對象
/// </summary>
[Flags]
public enum HolidayRole
{
    /// <summary>無（預設值）</summary>
    None = 0,

    /// <summary>勞工適用</summary>
    Labor = 1 << 0,

    /// <summary>軍人適用</summary>
    Soldier = 1 << 1,

    /// <summary>教師適用</summary>
    Teacher = 1 << 2,

    /// <summary>全體適用</summary>
    All = Labor | Soldier | Teacher,
}

/// <summary>
/// 表示台灣國定假日的資訊
/// </summary>
public readonly record struct RocHoliday(bool IsHoliday, HolidayRole Role, string Description)
{
    /// <summary>
    /// 將 <see cref="RocHoliday"/> 隱含轉換為 <see cref="bool"/>，表示是否為假日
    /// </summary>
    public static implicit operator bool(RocHoliday h) => h.IsHoliday;

    /// <summary>
    /// 表示非假日
    /// </summary>
    public static readonly RocHoliday None = default;
}
