using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
{
    public AvlTree() : base() { }
    
    public AvlTree(IComparer<TKey>? comparer) : base(comparer) { }

    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new AvlNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        BalanceUpward(newNode.Parent);
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        BalanceUpward(parent);
    }

    private void BalanceUpward(AvlNode<TKey, TValue>? node)
    {
        while (node != null)
        {
            UpdateHeight(node);
            int balance = GetBalanceFactor(node);

            if (balance > 1)
            {
                if (GetBalanceFactor(node.Left) < 0)
                {
                    if (node.Left != null)
                    {
                        RotateLeft(node.Left);
                    }
                }
                RotateRight(node);
                node = node.Parent;
            }
            else if (balance < -1)
            {
                if (GetBalanceFactor(node.Right) > 0)
                {
                    if (node.Right != null)
                    {
                        RotateRight(node.Right);
                    }
                }
                RotateLeft(node);
                node = node.Parent;
            }

            node = node?.Parent;
        }
    }

    private void UpdateHeight(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return;
        }
        
        int leftHeight = node.Left?.Height ?? 0;
        int rightHeight = node.Right?.Height ?? 0;
        node.Height = Math.Max(leftHeight, rightHeight) + 1;
    }

    private int GetBalanceFactor(AvlNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return 0;
        }
        
        int leftHeight = node.Left?.Height ?? 0;
        int rightHeight = node.Right?.Height ?? 0;
        return leftHeight - rightHeight;
    }
}