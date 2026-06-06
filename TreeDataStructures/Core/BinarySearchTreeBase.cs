using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();


    public virtual void Add(TKey key, TValue value)
    {
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Count = 1;
            OnNodeAdded(Root);
            return;
        }
        TNode? current = Root;
        TNode? parent = null;
        while (current != null)
        {
            parent = current;
            int compRes = Comparer.Compare(key, current.Key);
            if (compRes == 0)
            {
                current.Value = value;
                return;
            }
            else if (compRes > 0)
            {
                current = current.Right;
            }
            else
            {
                current = current.Left;
            }
        }
        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;
        int res = Comparer.Compare(key, parent!.Key);
        if (res > 0)
        {
            parent.Right = newNode;
        }
        else
        {
            parent.Left = newNode;
        }
        Count++;
        OnNodeAdded(newNode);
    }


    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }


    protected virtual void RemoveNode(TNode node)
    {
        TNode? parentAfterRemove;
        TNode? childAfterRemove;
        if (node.Left == null)
        {
            parentAfterRemove = node.Parent;
            childAfterRemove = node.Right;
            Transplant(node, node.Right);
            OnNodeRemoved(parentAfterRemove, childAfterRemove);
            return;
        }
        else if (node.Right == null)
        {
            parentAfterRemove = node.Parent;
            childAfterRemove = node.Left;
            Transplant(node, node.Left);
            OnNodeRemoved(parentAfterRemove, childAfterRemove);
            return;
        }
        TNode nextNode = node.Right;
        while (nextNode.Left != null)
        {
            nextNode = nextNode.Left;
        }
        if (nextNode.Parent != node)
        {
            parentAfterRemove = nextNode.Parent;
            childAfterRemove = nextNode.Right;
            Transplant(nextNode, nextNode.Right);
            nextNode.Right = node.Right;
            nextNode.Right!.Parent = nextNode;
        }
        else
        {
            parentAfterRemove = nextNode;
            childAfterRemove = nextNode.Right;
        }
        Transplant(node, nextNode);
        nextNode.Left = node.Left;
        nextNode.Left!.Parent = nextNode;
        OnNodeRemoved(parentAfterRemove, childAfterRemove);
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }




    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }




    protected abstract TNode CreateNode(TKey key, TValue value);


    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        TNode? y = x.Right;
        if (y == null) { return; }

        x.Right = y.Left;
        if (y.Left != null){
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;
        if (x.Parent == null){
            Root = y;
        }
        else if (x.IsLeftChild){
            x.Parent.Left = y;
        }
        else{
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        TNode? x = y.Left;
        if (x == null) { return; }

        y.Left = x.Right;
        if (x.Right != null){
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;
        if (y.Parent == null){
            Root = x;
        }
        else if (y.IsLeftChild){
            y.Parent.Left = x;
        }
        else{
            y.Parent.Right = x;
        }

        x.Right = y;
        y.Parent = x;
    }

    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null) { return; }
        RotateRight(x.Right);
        RotateLeft(x);
    }

    protected void RotateBigRight(TNode y)
    {
        if (y.Left == null) { return; }
        RotateLeft(y.Left);
        RotateRight(y);
    }

    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Right == null) { return; }
        RotateRight(x.Right);
        RotateLeft(x);
    }

    protected void RotateDoubleRight(TNode y)
    {
        if (y.Left == null) { return; }
        RotateLeft(y.Left);
        RotateRight(y);
    }

    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        if (v != null)
            v.Parent = u.Parent;
    }
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (тк он забанен).
    /// </summary>
    private struct TreeIterator :
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private Stack<TraversalFrame>? _stack;
        private bool _initialized;
        private bool _hasCurrent;
        private TreeEntry<TKey, TValue> _current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _stack = null;
            _initialized = false;
            _hasCurrent = false;
            _current = default;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => new TreeIterator(_root, _strategy);
        IEnumerator IEnumerable.GetEnumerator() => new TreeIterator(_root, _strategy);

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (!_hasCurrent)
                {
                    throw new InvalidOperationException("Enumerator is not positioned on a valid element.");
                }

                return _current;
            }
        }

        object IEnumerator.Current => Current;


        public bool MoveNext()
        {
            EnsureInitialized();
            if (_stack == null) { return false; }

            _hasCurrent = false;
            while (_stack.Count > 0)
            {
                TraversalFrame frame = _stack.Pop();
                switch (_strategy)
                {
                    case TraversalStrategy.InOrder:
                        if (TryMoveInOrder(frame)) { return true; }
                        break;
                    case TraversalStrategy.PreOrder:
                        if (TryMovePreOrder(frame)) { return true; }
                        break;
                    case TraversalStrategy.PostOrder:
                        if (TryMovePostOrder(frame)) { return true; }
                        break;
                    case TraversalStrategy.InOrderReverse:
                        if (TryMoveInOrderReverse(frame)) { return true; }
                        break;
                    case TraversalStrategy.PreOrderReverse:
                        if (TryMovePreOrderReverse(frame)) { return true; }
                        break;
                    case TraversalStrategy.PostOrderReverse:
                        if (TryMovePostOrderReverse(frame)) { return true; }
                        break;
                }
            }

            _current = default;
            return false;
        }

        public void Reset()
        {
            _stack = null;
            _initialized = false;
            _hasCurrent = false;
            _current = default;
        }


        public void Dispose() { }

        private void EnsureInitialized()
        {
            if (_initialized) { return; }
            _initialized = true;
            _stack = [];
            Push(_root, 0);
        }

        private void Push(TNode? node, byte state)
        {
            if (node != null)
            {
                _stack!.Push(new TraversalFrame(node, state));
            }
        }

        private void SetCurrent(TNode node)
        {
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, GetSubtreeHeight(node));
            _hasCurrent = true;
        }

        private static int GetSubtreeHeight(TNode? node)
        {
            if (node == null) { return 0; }

            int leftHeight = GetSubtreeHeight(node.Left);
            int rightHeight = GetSubtreeHeight(node.Right);
            return 1 + Math.Max(leftHeight, rightHeight);
        }

        private bool TryMoveInOrder(TraversalFrame frame)
        {
            if (frame.State == 0)
            {
                Push(frame.Node.Right, 0);
                Push(frame.Node, 1);
                Push(frame.Node.Left, 0);
                return false;
            }

            SetCurrent(frame.Node);
            return true;
        }

        private bool TryMovePreOrder(TraversalFrame frame)
        {
            if (frame.State == 0)
            {
                Push(frame.Node.Right, 0);
                Push(frame.Node.Left, 0);
                SetCurrent(frame.Node);
                return true;
            }

            return false;
        }

        private bool TryMovePostOrder(TraversalFrame frame)
        {
            if (frame.State == 0)
            {
                Push(frame.Node, 1);
                Push(frame.Node.Right, 0);
                Push(frame.Node.Left, 0);
                return false;
            }

            SetCurrent(frame.Node);
            return true;
        }

    private bool TryMovePreOrderReverse(TraversalFrame frame)
    {
        if (frame.State == 0)
        {
            Push(frame.Node, 1);      
            Push(frame.Node.Left, 0);
            Push(frame.Node.Right, 0); 
            return false;
        }
        SetCurrent(frame.Node);
        return true;
    }

        private bool TryMoveInOrderReverse(TraversalFrame frame)
        {
            if (frame.State == 0)
            {
                Push(frame.Node.Left, 0);  
                Push(frame.Node, 1);     
                Push(frame.Node.Right, 0);
                return false;
            }
            SetCurrent(frame.Node);   
            return true;
        }

        private bool TryMovePostOrderReverse(TraversalFrame frame)
        {
            // На самом деле тест ожидает C-R-L (это Reversed Prefix)
            SetCurrent(frame.Node);       
            Push(frame.Node.Left, 0);     
            Push(frame.Node.Right, 0);     
            return true;
        }
        private readonly record struct TraversalFrame(TNode Node, byte State);
    }


    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder()
            .Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) { throw new ArgumentException("is null"); }
        if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentException("Idx err"); }
        if (array.Length - arrayIndex < Count) { throw new ArgumentException("wrong length"); }
        int i = arrayIndex;
        foreach (var kv in this)
        {
            array[i++] = kv;
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}