using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    public RedBlackTree() : base() { }
    
    public RedBlackTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        newNode.Color = RbColor.Red;
        BalanceAfterInsert(newNode);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (child != null && child.Color == RbColor.Black)
        {
            BalanceAfterRemove(child, parent);
        }
    }

    private void BalanceAfterInsert(RbNode<TKey, TValue> node)
    {
        while (node.Parent != null && node.Parent.Color == RbColor.Red)
        {
            if (node.Parent.IsLeftChild)
            {
                RbNode<TKey, TValue>? uncle = node.Parent.Parent?.Right;
                
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    node.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    if (node.IsRightChild)
                    {
                        node = node.Parent;
                        RotateLeft(node);
                    }
                    
                    node.Parent!.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    RotateRight(node.Parent.Parent);
                }
            }
            else
            {
                RbNode<TKey, TValue>? uncle = node.Parent.Parent?.Left;
                
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    node.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    if (node.IsLeftChild)
                    {
                        node = node.Parent;
                        RotateRight(node);
                    }
                    
                    node.Parent!.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    RotateLeft(node.Parent.Parent);
                }
            }
        }
        
        Root!.Color = RbColor.Black;
    }

    private void BalanceAfterRemove(RbNode<TKey, TValue> node, RbNode<TKey, TValue>? parent)
    {
        while (node != Root && GetColor(node) == RbColor.Black)
        {
            if (node == parent?.Left)
            {
                RbNode<TKey, TValue>? sibling = parent.Right;
                
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }
                
                if (GetColor(sibling?.Left) == RbColor.Black && 
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    node = parent;
                    parent = node.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Right) == RbColor.Black)
                    {
                        if (sibling?.Left != null)
                        {
                            sibling.Left.Color = RbColor.Black;
                        }
                        if (sibling != null)
                        {
                            sibling.Color = RbColor.Red;
                        }
                        RotateRight(sibling!);
                        sibling = parent.Right;
                    }
                    
                    if (sibling != null)
                    {
                        sibling.Color = parent.Color;
                    }
                    parent.Color = RbColor.Black;
                    if (sibling?.Right != null)
                    {
                        sibling.Right.Color = RbColor.Black;
                    }
                    RotateLeft(parent);
                    node = Root!;
                }
            }
            else
            {
                RbNode<TKey, TValue>? sibling = parent?.Left;
                
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }
                
                if (GetColor(sibling?.Left) == RbColor.Black && 
                    GetColor(sibling?.Right) == RbColor.Black)
                {
                    if (sibling != null)
                    {
                        sibling.Color = RbColor.Red;
                    }
                    node = parent!;
                    parent = node.Parent;
                }
                else
                {
                    if (GetColor(sibling?.Left) == RbColor.Black)
                    {
                        if (sibling?.Right != null)
                        {
                            sibling.Right.Color = RbColor.Black;
                        }
                        if (sibling != null)
                        {
                            sibling.Color = RbColor.Red;
                        }
                        RotateLeft(sibling!);
                        sibling = parent?.Left;
                    }
                    
                    if (sibling != null)
                    {
                        sibling.Color = parent!.Color;
                    }
                    parent!.Color = RbColor.Black;
                    if (sibling?.Left != null)
                    {
                        sibling.Left.Color = RbColor.Black;
                    }
                    RotateRight(parent);
                    node = Root!;
                }
            }
        }
        
        node.Color = RbColor.Black;
    }

    private RbColor GetColor(RbNode<TKey, TValue>? node)
    {
        return node?.Color ?? RbColor.Black;
    }
}