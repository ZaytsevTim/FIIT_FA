using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class TreapNode<TKey, TValue> : Node<TKey, TValue, TreapNode<TKey, TValue>>
{
    public int Priority { get; set; }
    
    public TreapNode(TKey key, TValue value) : base(key, value)
    {
        Priority = Random.Shared.Next();
    }
}