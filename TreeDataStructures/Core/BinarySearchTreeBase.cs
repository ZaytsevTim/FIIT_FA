using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

// Абстрактный базовый класс для бинарного дерева поиска.
// Содержит общую логику: вставку, удаление, поиск, вращения, обходы.
// Конкретные деревья (AVL, Красно-чёрное, Splay) наследуют его и
// переопределяют OnNodeAdded/OnNodeRemoved для балансировки.
public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>  // TNode — узел, знает о родителе, левом и правом ребёнке
{
    // Корень дерева. Может быть null, если дерево пустое.
    protected TNode? Root;

    // Компаратор для сравнения ключей. По умолчанию — Comparer<TKey>.Default.
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    // Количество узлов в дереве.
    public int Count { get; protected set; }

    // Дерево не является ReadOnly.
    public bool IsReadOnly => false;

    // Все ключи в порядке InOrder (возрастание).
    public ICollection<TKey> Keys => InOrder().Select(x => x.Key).ToList();

    // Все значения в порядке InOrder.
    public ICollection<TValue> Values => InOrder().Select(x => x.Value).ToList();

    // ВСТАВКА
    public virtual void Add(TKey key, TValue value)
    {
        // Если дерево пустое — создаём корень.
        if (Root == null)
        {
            Root = CreateNode(key, value);
            Count = 1;
            OnNodeAdded(Root);   // Уведомляем наследников (для балансировки)
            return;
        }

        // Ищем место для вставки (как в обычном BST).
        TNode? current = Root;
        TNode? parent = null;
        while (current != null)
        {
            parent = current;
            int compRes = Comparer.Compare(key, current.Key);
            if (compRes == 0)
            {
                // Ключ уже существует — обновляем значение.
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

        // Создаём новый узел и подвешиваем к родителю.
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
        OnNodeAdded(newNode);  // Уведомляем наследников (для балансировки)
    }

    // УДАЛЕНИЕ
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        this.Count--;
        return true;
    }

    // Удаляет указанный узел из дерева.
    // Использует стандартный алгоритм BST-удаления:
    // - Если нет левого ребёнка — заменяем правым.
    // - Если нет правого ребёнка — заменяем левым.
    // - Если оба есть — находим следующий по порядку (минимальный в правом поддереве),
    //   копируем его на место удаляемого, а его самого удаляем.
    protected virtual void RemoveNode(TNode node)
    {
        TNode? parentAfterRemove;  // Родитель, чей ребёнок изменился
        TNode? childAfterRemove;   // Узел, который встал на место удалённого

        // Случай 1: нет левого ребёнка.
        if (node.Left == null)
        {
            parentAfterRemove = node.Parent;
            childAfterRemove = node.Right;
            Transplant(node, node.Right);
            OnNodeRemoved(parentAfterRemove, childAfterRemove);
            return;
        }

        // Случай 2: нет правого ребёнка.
        if (node.Right == null)
        {
            parentAfterRemove = node.Parent;
            childAfterRemove = node.Left;
            Transplant(node, node.Left);
            OnNodeRemoved(parentAfterRemove, childAfterRemove);
            return;
        }

        // Случай 3: оба ребёнка есть.
        // Находим следующий узел (минимальный в правом поддереве).
        TNode nextNode = node.Right;
        while (nextNode.Left != null)
        {
            nextNode = nextNode.Left;
        }

        // Если следующий узел — не прямой правый ребёнок удаляемого.
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
            // Следующий узел — прямой правый ребёнок.
            parentAfterRemove = nextNode;
            childAfterRemove = nextNode.Right;
        }

        Transplant(node, nextNode);
        nextNode.Left = node.Left;
        nextNode.Left!.Parent = nextNode;
        OnNodeRemoved(parentAfterRemove, childAfterRemove);
    }


    // ПОИСК

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

    // Индексатор: получение значения по ключу (или исключение) и установка.
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    // КОЛЛБЭКИ ДЛЯ БАЛАНСИРОВКИ

    // Вызывается после успешной вставки узла.
    // Наследники (AVL, КЧД, Splay) переопределяют этот метод для балансировки.
    protected virtual void OnNodeAdded(TNode newNode) { }

    // Вызывается после удаления узла.
    // parent — узел, чей ребёнок изменился.
    // child — узел, который встал на место удалённого.
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }


    // ФАБРИЧНЫЙ МЕТОД И ПОИСК


    // Создаёт новый узел дерева. Должен быть переопределён в наследниках.
    protected abstract TNode CreateNode(TKey key, TValue value);

    // Ищет узел по ключу. Возвращает null, если не найден.
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

    // ВРАЩЕНИЯ

    // Левый поворот вокруг узла x.
    // x становится левым ребёнком своего бывшего правого ребёнка y.
    protected void RotateLeft(TNode x)
    {
        TNode? y = x.Right;
        if (y == null) { return; }

        // Перемещаем левое поддерево y в правое поддерево x.
        x.Right = y.Left;
        if (y.Left != null) { y.Left.Parent = x; }

        // Подвешиваем y на место x.
        y.Parent = x.Parent;
        if (x.Parent == null) { Root = y; }
        else if (x.IsLeftChild) { x.Parent.Left = y; }
        else { x.Parent.Right = y; }

        // x становится левым ребёнком y.
        y.Left = x;
        x.Parent = y;
    }

    // Правый поворот вокруг узла y.
    // y становится правым ребёнком своего бывшего левого ребёнка x.
    protected void RotateRight(TNode y)
    {
        TNode? x = y.Left;
        if (x == null) { return; }

        y.Left = x.Right;
        if (x.Right != null) { x.Right.Parent = y; }

        x.Parent = y.Parent;
        if (y.Parent == null) { Root = x; }
        else if (y.IsLeftChild) { y.Parent.Left = x; }
        else { y.Parent.Right = x; }

        x.Right = y;
        y.Parent = x;
    }

    // Большой левый поворот (RL): сначала правый вокруг правого ребёнка, потом левый вокруг x.
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null) { return; }
        RotateRight(x.Right);
        RotateLeft(x);
    }

    // Большой правый поворот (LR): сначала левый вокруг левого ребёнка, потом правый вокруг y.
    protected void RotateBigRight(TNode y)
    {
        if (y.Left == null) { return; }
        RotateLeft(y.Left);
        RotateRight(y);
    }

    // Синонимы для больших поворотов (Double = Big).
    protected void RotateDoubleLeft(TNode x) => RotateBigLeft(x);
    protected void RotateDoubleRight(TNode y) => RotateBigRight(y);

    // ТРАНСПЛАНТАЦИЯ (замена одного поддерева другим)
    // Заменяет поддерево с корнем u на поддерево с корнем v.
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

    // ОБХОДЫ ДЕРЕВА (возвращают итераторы)

    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    // ВНУТРЕННИЙ ИТЕРАТОР (реализует IEnumerable/IEnumerator вручную)

    // Использует стек для симуляции рекурсии.
    // Каждый фрейм в стеке хранит узел и состояние (0 = зайти в поддеревья, 1 = выдать узел).
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
                    throw new InvalidOperationException("Enumerator is not positioned on a valid element.");
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

        // Инициализация: кладём корень в стек.
        private void EnsureInitialized()
        {
            if (_initialized) { return; }
            _initialized = true;
            _stack = [];
            Push(_root, 0);
        }

        // Кладёт узел в стек с указанным состоянием.
        private void Push(TNode? node, byte state)
        {
            if (node != null)
                _stack!.Push(new TraversalFrame(node, state));
        }

        // Устанавливает текущий элемент для возврата через Current.
        private void SetCurrent(TNode node)
        {
            _current = new TreeEntry<TKey, TValue>(node.Key, node.Value, GetSubtreeHeight(node));
            _hasCurrent = true;
        }

        // Вычисляет высоту поддерева рекурсивно.
        private static int GetSubtreeHeight(TNode? node)
        {
            if (node == null) { return 0; }
            int leftHeight = GetSubtreeHeight(node.Left);
            int rightHeight = GetSubtreeHeight(node.Right);
            return 1 + Math.Max(leftHeight, rightHeight);
        }

        // InOrder (L-N-R): левое поддерево, узел, правое поддерево.
        private bool TryMoveInOrder(TraversalFrame frame)
        {
            if (frame.State == 0)
            {
                // Кладём в стек в обратном порядке: правое, узел, левое.
                Push(frame.Node.Right, 0);
                Push(frame.Node, 1);
                Push(frame.Node.Left, 0);
                return false;
            }
            SetCurrent(frame.Node);
            return true;
        }

        // PreOrder (N-L-R): узел, левое поддерево, правое поддерево.
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

        // PostOrder (L-R-N): левое, правое, узел.
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

        // PreOrderReverse (N-R-L): узел, правое, левое.
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

        // InOrderReverse (R-N-L): правое, узел, левое.
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

        // PostOrderReverse (C-R-L): узел, левое, правое (Reversed Prefix).
        private bool TryMovePostOrderReverse(TraversalFrame frame)
        {
            SetCurrent(frame.Node);
            Push(frame.Node.Left, 0);
            Push(frame.Node.Right, 0);
            return true;
        }

        // Фрейм стека: узел и состояние (0 = ещё не обработан, 1 = пора выдавать).
        private readonly record struct TraversalFrame(TNode Node, byte State);
    }

    // Стратегии обхода.
    private enum TraversalStrategy
    {
        InOrder, PreOrder, PostOrder,
        InOrderReverse, PreOrderReverse, PostOrderReverse
    }

    // РЕАЛИЗАЦИЯ ИНТЕРФЕЙСОВ IDictionary и ICollection

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder()
            .Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        if (array == null) { throw new ArgumentException("is null"); }
        if (arrayIndex < 0 || arrayIndex > array.Length) { throw new ArgumentException("Idx err"); }
        if (array.Length - arrayIndex < Count) { throw new ArgumentException("wrong length"); }
        int i = arrayIndex;
        foreach (var kv in this)
            array[i++] = kv;
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}