using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    public SplayTree() : base() { }
    
    public SplayTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new BstNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
    }

    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        
        BstNode<TKey, TValue>? node = base.FindNode(key);
        
        if (node != null)
        {
            value = node.Value;
            Splay(node);
            return true;
        }
        
        value = default;
        return false;
    }

    public override bool ContainsKey(TKey key)
    {
        BstNode<TKey, TValue>? node = base.FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        return false;
    }

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            if (node.Parent.Parent == null)
            {
                if (node.IsLeftChild)
                {
                    RotateRight(node.Parent);
                }
                else
                {
                    RotateLeft(node.Parent);
                }
            }
            else if (node.IsLeftChild && node.Parent.IsLeftChild)
            {
                RotateRight(node.Parent.Parent);
                RotateRight(node.Parent);
            }
            else if (node.IsRightChild && node.Parent.IsRightChild)
            {
                RotateLeft(node.Parent.Parent);
                RotateLeft(node.Parent);
            }
            else if (node.IsRightChild && node.Parent.IsLeftChild)
            {
                RotateLeft(node.Parent);
                RotateRight(node.Parent);
            }
            else
            {
                RotateRight(node.Parent);
                RotateLeft(node.Parent);
            }
        }
        
        Root = node;
    }
}