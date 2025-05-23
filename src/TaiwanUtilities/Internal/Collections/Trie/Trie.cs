// source: https://github.com/kpol/trie/

//
// MIT License
// 
// Copyright (c) 2020 Kirill Polishchuk
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace TaiwanUtilities.Internals;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

internal class CharTrieNode(char key)
{
    public char Key { get; } = key;

    public virtual bool IsTerminal => false;

    public CharTrieNode[] Children { get; set; } = [];

    public void AddChild(CharTrieNode node)
    {
        var children = new CharTrieNode[Children.Length + 1];
        Array.Copy(Children, children, Children.Length);
        children[^1] = node;
        Children = children;
    }

    public void RemoveChildAt(int index)
    {
        var children = new CharTrieNode[Children.Length - 1];
        Children[index] = Children[^1];
        Array.Copy(Children, children, children.Length);
        Children = children;
    }

    public void CopyChildren(CharTrieNode[] toCopy)
    {
        Children = new CharTrieNode[toCopy.Length];
        Array.Copy(toCopy, Children, Children.Length);
    }

    public override string ToString() => $"Key: {Key}";
}

internal class TerminalCharTrieNode(char key) : CharTrieNode(key)
{
    public override bool IsTerminal => true;

    public string Word { get; init; } = null!;

    public override string ToString() => $"Key: {Key}, Word: {Word}";
}

internal readonly record struct Character(char? Char)
{
    public static Character Any { get; } = new();

    public static implicit operator Character(char c) => new(c);
}

internal sealed class Trie : ICollection<string>, IReadOnlyCollection<string>
{
    private readonly IEqualityComparer<char> _comparer;

    private readonly CharTrieNode _root = new(char.MinValue);

    public Trie(IEqualityComparer<char>? comparer = null)
    {
        _comparer = comparer ?? EqualityComparer<char>.Default;
    }

    public int Count { get; private set; }

    bool ICollection<string>.IsReadOnly => false;

    public bool Add(string word)
    {
        Guard.ThrowIfNullOrEmpty(word);

        var (existingTerminalNode, parent) = AddNodesFromUpToBottom(word.AsSpan());

        if (existingTerminalNode is not null && existingTerminalNode.IsTerminal) return false; // already exists

        var newTerminalNode = new TerminalCharTrieNode(word[^1]) { Word = word };

        AddTerminalNode(parent, existingTerminalNode, newTerminalNode, word);

        return true;
    }

    public void Clear()
    {
        _root.Children = [];
        Count = 0;
    }

    public bool Contains(string word) => Contains(word.AsSpan());

    public bool Contains(ReadOnlySpan<char> word)
    {
        if (word.IsEmpty)
        {
            Guard.ThrowIfNullOrEmpty(word.ToString(), nameof(word));
        }

        var node = GetNode(word);

        return node is not null && node.IsTerminal;
    }

    public bool Remove(string word)
    {
        Guard.ThrowIfNullOrEmpty(word.ToString());

        var nodesUpToBottom = GetNodesForRemoval(word);

        if (nodesUpToBottom.Count == 0) return false;

        RemoveNode(nodesUpToBottom);

        return true;
    }

    public IEnumerable<string> StartsWith(string value)
    {
        Guard.ThrowIfNullOrEmpty(value);

        return _();

        IEnumerable<string> _() => GetTerminalNodesByPrefix(value.AsSpan()).Select(n => n.Word);
    }

    public IEnumerable<string> Matches(IReadOnlyList<Character> pattern)
    {
        Guard.ThrowIfNull(pattern);
        Guard.ThrowIfZero(pattern.Count);

        return _();

        IEnumerable<string> _() =>
            GetNodesByPattern(pattern)
                .Where(n => n.IsTerminal)
                .Cast<TerminalCharTrieNode>()
                .Select(n => n.Word);
    }

    public IEnumerable<string> StartsWith(IReadOnlyList<Character> pattern)
    {
        Guard.ThrowIfNull(pattern);
        Guard.ThrowIfZero(pattern.Count);

        return _();

        IEnumerable<string> _()
        {
            foreach (var n in GetNodesByPattern(pattern))
            {
                if (n.IsTerminal)
                {
                    yield return ((TerminalCharTrieNode)n).Word;
                }

                foreach (var terminalNode in GetDescendantTerminalNodes(n))
                {
                    yield return terminalNode.Word;
                }
            }
        }
    }

    internal (CharTrieNode? existingTerminalNode, CharTrieNode parent) AddNodesFromUpToBottom(ReadOnlySpan<char> word)
    {
        var current = _root;

        for (int i = 0; i < word.Length - 1; i++)
        {
            var n = GetChildNode(current, word[i]);

            if (n is not null)
            {
                current = n;
            }
            else
            {
                CharTrieNode node = new(word[i]);
                AddToNode(current, node);
                current = node;
            }
        }

        var terminalNode = GetChildNode(current, word[^1]);

        return (terminalNode, current);
    }

    internal void AddTerminalNode(CharTrieNode parent, CharTrieNode? existingNode, CharTrieNode newTerminalNode, string word)
    {
        if (existingNode is not null)
        {
            newTerminalNode.CopyChildren(existingNode.Children);

            RemoveChildFromNode(parent, word[^1]);
        }

        AddToNode(parent, newTerminalNode);
        Count++;
    }

    internal IEnumerable<TerminalCharTrieNode> GetTerminalNodesByPrefix(ReadOnlySpan<char> prefix)
    {
        var node = GetNode(prefix);
        return GetTerminalNodes(node);
    }

    private IEnumerable<TerminalCharTrieNode> GetTerminalNodes(CharTrieNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        if (node.IsTerminal)
        {
            yield return (TerminalCharTrieNode)node;
        }

        foreach (var n in GetDescendantTerminalNodes(node))
        {
            yield return n;
        }
    }

    public IEnumerator<string> GetEnumerator() => GetAllTerminalNodes().Select(n => n.Word).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    void ICollection<string>.Add(string word)
    {
        Guard.ThrowIfNullOrEmpty(word);

        Add(word);
    }

    void ICollection<string>.CopyTo(string[] array, int arrayIndex)
    {
        Guard.ThrowIfNull(array);
        Guard.ThrowIfNegative(arrayIndex);

        if (Count > array.Length - arrayIndex)
        {
            throw new ArgumentException(
                "The number of elements in the trie is greater than the available space from index to the end of the destination array.");
        }

        foreach (var node in GetAllTerminalNodes())
        {
            array[arrayIndex++] = node.Word;
        }
    }

    internal IEnumerable<TerminalCharTrieNode> GetAllTerminalNodes() => GetDescendantTerminalNodes(_root);

    internal static IEnumerable<TerminalCharTrieNode> GetDescendantTerminalNodes(CharTrieNode node)
    {
        Queue<CharTrieNode> queue = new(node.Children);

        while (queue.Count > 0)
        {
            var n = queue.Dequeue();

            if (n.IsTerminal)
            {
                yield return (TerminalCharTrieNode)n;
            }

            for (var i = 0; i < n.Children.Length; i++)
            {
                queue.Enqueue(n.Children[i]);
            }
        }
    }

    internal CharTrieNode? GetNode(ReadOnlySpan<char> prefix)
    {
        var current = _root;

        for (var i = 0; i < prefix.Length; i++)
        {
            current = GetChildNode(current, prefix[i]);

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }

    internal IEnumerable<CharTrieNode> GetNodesByPattern(IReadOnlyList<Character> pattern)
    {
        Queue<(CharTrieNode node, int index)> queue = [];
        queue.Enqueue((_root, 0));

        while (queue.Count > 0)
        {
            var (node, index) = queue.Dequeue();

            if (index == pattern.Count - 1)
            {
                if (pattern[index].Char is { } ch)
                {
                    var n = GetChildNode(node, ch);

                    if (n is not null)
                    {
                        yield return n;
                    }
                }
                else
                {
                    for (var i = 0; i < node.Children.Length; i++)
                    {
                        yield return node.Children[i];
                    }
                }
            }
            else
            {
                if (pattern[index].Char is { } ch)
                {
                    var n = GetChildNode(node, ch);

                    if (n is not null)
                    {
                        queue.Enqueue((n, index + 1));
                    }
                }
                else
                {
                    for (var i = 0; i < node.Children.Length; i++)
                    {
                        queue.Enqueue((node.Children[i], index + 1));
                    }
                }
            }
        }
    }

    private Stack<CharTrieNode> GetNodesForRemoval(string prefix)
    {
        var current = _root;

        Stack<CharTrieNode> nodesUpToBottom = [];
        nodesUpToBottom.Push(_root);

        for (var i = 0; i < prefix.Length; i++)
        {
            var c = prefix[i];
            current = GetChildNode(current, c);

            if (current is not null)
            {
                nodesUpToBottom.Push(current);
            }
            else
            {
                return [];
            }
        }

        return current.IsTerminal ? nodesUpToBottom : [];
    }

    private void RemoveNode(Stack<CharTrieNode> nodesUpToBottom)
    {
        Count--;

        var node = nodesUpToBottom.Pop();

        if (node.Children.Length == 0)
        {
            while (node.Children.Length == 0 && nodesUpToBottom.Count > 0)
            {
                var parent = nodesUpToBottom.Pop();
                RemoveChildFromNode(parent, node.Key);

                if (parent.IsTerminal) return;

                node = parent;

            }
        }
        else
        {
            // convert node to non-terminal node
            CharTrieNode n = new(node.Key);
            n.CopyChildren(node.Children);

            var parent = nodesUpToBottom.Count == 0 ? _root : nodesUpToBottom.Pop();

            RemoveChildFromNode(parent, node.Key);
            AddToNode(parent, n);
        }
    }

    private void AddToNode(CharTrieNode node, CharTrieNode nodeToAdd)
    {
        for (var i = 0; i < node.Children.Length; i++)
        {
            if (_comparer.Equals(nodeToAdd.Key, node.Children[i].Key))
            {
                return;
            }
        }

        node.AddChild(nodeToAdd);
    }

    private void RemoveChildFromNode(CharTrieNode node, char key)
    {
        for (int i = 0; i < node.Children.Length; i++)
        {
            if (_comparer.Equals(key, node.Children[i].Key))
            {
                node.RemoveChildAt(i);

                break;
            }
        }
    }

    private CharTrieNode? GetChildNode(CharTrieNode node, char key)
    {
        for (var i = 0; i < node.Children.Length; i++)
        {
            var n = node.Children[i];

            if (_comparer.Equals(key, n.Key))
            {
                return n;
            }
        }

        return null;
    }
}

internal sealed class TrieDictionary<TValue>(IEqualityComparer<char>? comparer = null)
    : IDictionary<string, TValue>, IReadOnlyDictionary<string, TValue>
{
    private readonly Trie _trie = new(comparer);

    public ICollection<string> Keys => [.. _trie];

    public ICollection<TValue> Values =>
        [.. _trie.GetAllTerminalNodes().Select(t => ((TerminalValueCharTrieNode)t).Value)];

    public int Count => _trie.Count;

    bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => false;

    IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Values;

    public TValue this[string key]
    {
        get
        {
            Guard.ThrowIfNullOrEmpty(key);

            if (!TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException();
            }

            return value;
        }
        set
        {
            Guard.ThrowIfNullOrEmpty(key);

            TryAdd(key, value, InsertionBehavior.OverwriteExisting);
        }
    }

    public void Clear() => _trie.Clear();

    public void Add(string key, TValue value)
    {
        Guard.ThrowIfNullOrEmpty(key);

        TryAdd(key, value, InsertionBehavior.ThrowOnExisting);
    }

    public bool TryAdd(string key, TValue value)
    {
        Guard.ThrowIfNullOrEmpty(key);

        return TryAdd(key, value, InsertionBehavior.None);
    }

    public IEnumerable<KeyValuePair<string, TValue>> StartsWith(string value)
    {
        return StartsWith(value.AsSpan());
    }

    public IEnumerable<KeyValuePair<string, TValue>> StartsWith(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            Guard.ThrowIfNullOrEmpty(value.ToString(), nameof(value));
        }

        var nodes = _trie.GetTerminalNodesByPrefix(value);

        return nodes.Select(t => new KeyValuePair<string, TValue>(t.Word, ((TerminalValueCharTrieNode)t).Value));
    }

    public IEnumerable<KeyValuePair<string, TValue>> Matches(IReadOnlyList<Character> pattern)
    {
        Guard.ThrowIfNull(pattern);
        Guard.ThrowIfZero(pattern.Count);

        return _trie.GetNodesByPattern(pattern)
            .Where(n => n.IsTerminal).Cast<TerminalValueCharTrieNode>()
            .Select(n => new KeyValuePair<string, TValue>(n.Word, n.Value));
    }

    public IEnumerable<KeyValuePair<string, TValue>> StartsWith(IReadOnlyList<Character> pattern)
    {
        Guard.ThrowIfNull(pattern);
        Guard.ThrowIfZero(pattern.Count);

        return _();

        IEnumerable<KeyValuePair<string, TValue>> _()
        {
            foreach (var n in _trie.GetNodesByPattern(pattern))
            {
                if (n.IsTerminal)
                {
                    var terminalNode = (TerminalValueCharTrieNode)n;

                    yield return new KeyValuePair<string, TValue>(terminalNode.Word, terminalNode.Value);
                }

                foreach (var terminalNode in Trie.GetDescendantTerminalNodes(n).Cast<TerminalValueCharTrieNode>())
                {
                    yield return new KeyValuePair<string, TValue>(terminalNode.Word, terminalNode.Value);
                }
            }
        }
    }

    public bool Remove(string key)
    {
        Guard.ThrowIfNullOrEmpty(key);

        return _trie.Remove(key);
    }

    public bool ContainsKey(string key)
    {
        Guard.ThrowIfNullOrEmpty(key);

        return _trie.Contains(key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
    {
        Guard.ThrowIfNullOrEmpty(key);

        var node = _trie.GetNode(key.AsSpan());

        if (node is not null && node.IsTerminal)
        {
            value = ((TerminalValueCharTrieNode)node).Value;

            return true;
        }

        value = default;

        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() =>
        _trie.GetAllTerminalNodes()
            .Select(t => new KeyValuePair<string, TValue>(t.Word, ((TerminalValueCharTrieNode)t).Value)).GetEnumerator();

    bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    void ICollection<KeyValuePair<string, TValue>>.CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
    {
        Guard.ThrowIfNull(array);
        Guard.ThrowIfNegative(arrayIndex);

        if (Count > array.Length - arrayIndex)
        {
            throw new ArgumentException(
                "The number of elements in the trie is greater than the available space from index to the end of the destination array.");
        }

        foreach (var node in _trie.GetAllTerminalNodes().Cast<TerminalValueCharTrieNode>())
        {
            array[arrayIndex++] = new KeyValuePair<string, TValue>(node.Word, node.Value);
        }
    }

    void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item)
    {
        if (TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
        {
            return Remove(item.Key);
        }

        return false;
    }

    private bool TryAdd(string key, TValue value, InsertionBehavior insertionBehavior)
    {
        var (existingTerminalNode, parent) = _trie.AddNodesFromUpToBottom(key.AsSpan());

        if (existingTerminalNode is not null && existingTerminalNode.IsTerminal)
        {
            switch (insertionBehavior)
            {
                case InsertionBehavior.ThrowOnExisting:
                    throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));
                case InsertionBehavior.None:
                    return false;
                case InsertionBehavior.OverwriteExisting:
                default:
                    ((TerminalValueCharTrieNode)existingTerminalNode).Value = value;

                    return true;
            }
        }

        var newTerminalNode = new TerminalValueCharTrieNode(key[^1]) { Word = key, Value = value };

        _trie.AddTerminalNode(parent, existingTerminalNode, newTerminalNode, key);

        return true;
    }

    private sealed class TerminalValueCharTrieNode(char key) : TerminalCharTrieNode(key)
    {
        public TValue Value { get; set; } = default!;

        public override string ToString() => $"Key: {Key}, Word: {Word}, Value: {Value}";
    }

    private enum InsertionBehavior : byte
    {
        None = 0,

        OverwriteExisting = 1,

        ThrowOnExisting = 2
    }
}