// SPDX-License-Identifier: MIT
// Copyright (c) 2024-2026 Orlys
//
// TaiwanUtilities - Taiwan Postal Code Query Library
// Postal code data from Chunghwa Post under OGDL-Taiwan-1.0

namespace TaiwanUtilities.Internals;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// 編譯後的道路匹配 delegate
/// </summary>
/// <param name="number">門牌主號</param>
/// <param name="subNumber">附號（0 表示無附號）</param>
/// <param name="lane">巷號（0 表示無巷）</param>
/// <param name="alley">弄號（0 表示無弄）</param>
/// <returns>匹配的規則索引（-1 = 無匹配）</returns>
internal delegate int CompiledRoadMatcher(int number, int subNumber, int lane, int alley);

/// <summary>
/// 每條規則的元資料（編譯後保留的資訊）
/// </summary>
internal readonly struct RuleMetadata
{
    public readonly string ZipCode;
    public readonly string? Department;
    public readonly string? Office;
    public readonly string? Scope;

    public RuleMetadata(string zipCode, string? department, string? office, string? scope)
    {
        ZipCode = zipCode;
        Department = department;
        Office = office;
        Scope = scope;
    }
}

/// <summary>
/// 編譯後的道路規則（包含 Expression Tree 編譯的決策樹和元資料）
/// </summary>
internal readonly struct CompiledRoad
{
    /// <summary>編譯的決策樹 delegate</summary>
    public readonly CompiledRoadMatcher Matcher;

    /// <summary>每條規則的元資料（按索引對應）</summary>
    public readonly RuleMetadata[] Metadata;

    public CompiledRoad(CompiledRoadMatcher matcher, RuleMetadata[] metadata)
    {
        Matcher = matcher;
        Metadata = metadata;
    }
}

/// <summary>
/// 道路規則編譯器：將 PostalRule 集合編譯為 Expression Tree 決策樹
/// </summary>
internal static class RoadRuleCompiler
{
    /// <summary>
    /// 將一條路的所有規則編譯為單一決策樹 delegate
    /// </summary>
    /// <param name="rules">該路的所有 PostalRule</param>
    /// <returns>編譯後的 CompiledRoad</returns>
    public static CompiledRoad Compile(List<PostalRule> rules)
    {
        // 按特異性排序：越具體的規則越先匹配
        var sorted = rules
            .Select((r, i) => (Rule: r, OriginalIndex: i))
            .OrderByDescending(x => GetSpecificity(x.Rule))
            .ToList();

        // 建立元資料陣列（按排序後的順序）
        var metadata = new RuleMetadata[sorted.Count];
        for (int i = 0; i < sorted.Count; i++)
        {
            var rule = sorted[i].Rule;
            metadata[i] = new RuleMetadata(
                rule.ZipCode,
                rule.Department,
                rule.Office,
                rule.Scope);
        }

        // 建立 Expression Tree
        var numberParam = Expression.Parameter(typeof(int), "number");
        var subNumberParam = Expression.Parameter(typeof(int), "subNumber");
        var laneParam = Expression.Parameter(typeof(int), "lane");
        var alleyParam = Expression.Parameter(typeof(int), "alley");

        // 從最後一條規則開始往前堆疊 if-else 鏈
        Expression body = Expression.Constant(-1); // 預設：無匹配

        for (int i = sorted.Count - 1; i >= 0; i--)
        {
            var condition = BuildCondition(
                sorted[i].Rule, numberParam, subNumberParam, laneParam, alleyParam);

            body = Expression.Condition(
                condition,
                Expression.Constant(i),
                body);
        }

        var lambda = Expression.Lambda<CompiledRoadMatcher>(
            body, numberParam, subNumberParam, laneParam, alleyParam);

        var matcher = lambda.Compile();
        return new CompiledRoad(matcher, metadata);
    }

    /// <summary>
    /// 計算規則的特異性分數（越高越具體）
    /// </summary>
    private static int GetSpecificity(PostalRule rule)
    {
        int score = 0;

        // 有巷限制 +1000
        if (rule.LaneStart.HasValue)
            score += 1000;

        // 有弄限制 +500
        if (rule.AlleyStart.HasValue)
            score += 500;

        // 有附號限制 +100
        if (rule.NumberStartSub.HasValue && rule.NumberStartSub.Value > 0)
            score += 100;
        if (rule.NumberEndSub.HasValue && rule.NumberEndSub.Value > 0 && rule.NumberEndSub.Value < int.MaxValue)
            score += 100;

        // 特定門牌號碼 +50
        if (rule.NumberStart.HasValue && rule.NumberEnd.HasValue &&
            rule.NumberStart.Value == rule.NumberEnd.Value)
            score += 50;

        // 門牌範圍 +20
        if (rule.NumberStart.HasValue || rule.NumberEnd.HasValue)
            score += 20;

        // 單雙號限制 +10
        if (rule.EvenOdd.HasValue && rule.EvenOdd.Value != 0)
            score += 10;

        return score;
    }

    /// <summary>
    /// 為單一規則建立 Expression 條件（AND 鏈）
    /// </summary>
    private static Expression BuildCondition(
        PostalRule rule,
        ParameterExpression number,
        ParameterExpression subNumber,
        ParameterExpression lane,
        ParameterExpression alley)
    {
        var conditions = new List<Expression>();

        // === 巷號匹配 ===
        if (rule.LaneStart.HasValue)
        {
            // 規則有巷限制：地址必須有巷，且巷號在範圍內
            // lane >= LaneStart
            conditions.Add(Expression.GreaterThanOrEqual(
                lane, Expression.Constant(rule.LaneStart.Value)));

            // lane <= LaneEnd（如果有上限）
            if (rule.LaneEnd.HasValue)
            {
                conditions.Add(Expression.LessThanOrEqual(
                    lane, Expression.Constant(rule.LaneEnd.Value)));
            }
        }
        else
        {
            // 規則無巷限制：地址不應該有巷（lane == 0）
            conditions.Add(Expression.Equal(lane, Expression.Constant(0)));
        }

        // === 弄號匹配 ===
        if (rule.AlleyStart.HasValue)
        {
            // 規則有弄限制：地址必須有弄，且弄號在範圍內
            conditions.Add(Expression.GreaterThanOrEqual(
                alley, Expression.Constant(rule.AlleyStart.Value)));

            if (rule.AlleyEnd.HasValue)
            {
                conditions.Add(Expression.LessThanOrEqual(
                    alley, Expression.Constant(rule.AlleyEnd.Value)));
            }
        }
        else
        {
            // 規則無弄限制：地址不應該有弄（alley == 0）
            conditions.Add(Expression.Equal(alley, Expression.Constant(0)));
        }

        // === 單雙號匹配 ===
        if (rule.EvenOdd.HasValue && rule.EvenOdd.Value != 0)
        {
            if (rule.EvenOdd.Value == 1)
            {
                // 單號：(number & 1) == 1
                conditions.Add(Expression.Equal(
                    Expression.And(number, Expression.Constant(1)),
                    Expression.Constant(1)));
            }
            else if (rule.EvenOdd.Value == 2)
            {
                // 雙號：(number & 1) == 0
                conditions.Add(Expression.Equal(
                    Expression.And(number, Expression.Constant(1)),
                    Expression.Constant(0)));
            }
        }

        // === 主號範圍匹配 ===
        if (rule.NumberStart.HasValue && rule.NumberStart.Value > 0)
        {
            conditions.Add(Expression.GreaterThanOrEqual(
                number, Expression.Constant(rule.NumberStart.Value)));
        }

        if (rule.NumberEnd.HasValue && rule.NumberEnd.Value < int.MaxValue)
        {
            conditions.Add(Expression.LessThanOrEqual(
                number, Expression.Constant(rule.NumberEnd.Value)));
        }

        // === 附號範圍匹配 ===
        if (rule.NumberStartSub.HasValue && rule.NumberStartSub.Value > 0)
        {
            conditions.Add(Expression.GreaterThanOrEqual(
                subNumber, Expression.Constant(rule.NumberStartSub.Value)));
        }

        if (rule.NumberEndSub.HasValue && rule.NumberEndSub.Value > 0 && rule.NumberEndSub.Value < int.MaxValue)
        {
            conditions.Add(Expression.LessThanOrEqual(
                subNumber, Expression.Constant(rule.NumberEndSub.Value)));
        }

        // 合併所有條件為 AND 鏈
        if (conditions.Count == 0)
            return Expression.Constant(true);

        var result = conditions[0];
        for (int i = 1; i < conditions.Count; i++)
        {
            result = Expression.AndAlso(result, conditions[i]);
        }

        return result;
    }
}
