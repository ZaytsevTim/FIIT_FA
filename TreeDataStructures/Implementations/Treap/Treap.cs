using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    public Treap() : base() { }
    
    public Treap(IComparer<TKey>? comparer) : base(comparer) { }
    
    private Random _random = Random.Shared;

    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);
        
        int cmp = Comparer.Compare(key, root.Key);
        
        if (cmp < 0)
        {
            var (left, right) = Split(root.Left, key);
            root.Left = right;
            if (right != null) right.Parent = root;
            if (left != null) left.Parent = null;
            return (left, root);
        }
        else
        {
            var (left, right) = Split(root.Right, key);
            root.Right = left;
            if (left != null) left.Parent = root;
            if (right != null) right.Parent = null;
            return (root, right);
        }
    }

    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;
        
        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) left.Right.Parent = left;
            return left;
        }
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) right.Left.Parent = right;
            return right;
        }
    }

    public override void Add(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        Root = RemoveRecursive(Root, key);
        var newNode = CreateNode(key, value);
        var (left, right) = Split(Root, key);
        Root = Merge(Merge(left, newNode), right);
        
        Count++;
        OnNodeAdded(newNode);
    }

    private TreapNode<TKey, TValue>? RemoveRecursive(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return null;
        
        int cmp = Comparer.Compare(key, root.Key);
        
        if (cmp == 0)
        {
            var merged = Merge(root.Left, root.Right);
            if (merged != null) merged.Parent = root.Parent;
            root.Left = root.Right = root.Parent = null;
            Count--;
            return merged;
        }
        else if (cmp < 0)
        {
            root.Left = RemoveRecursive(root.Left, key);
            if (root.Left != null) root.Left.Parent = root;
        }
        else
        {
            root.Right = RemoveRecursive(root.Right, key);
            if (root.Right != null) root.Right.Parent = root;
        }
        
        return root;
    }

    public override bool Remove(TKey key)
    {
        if (Root == null) return false;
        
        int oldCount = Count;
        Root = RemoveRecursive(Root, key);
        
        if (Count < oldCount)
        {
            OnNodeRemoved(null, Root);
            return true;
        }
        
        return false;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }
}