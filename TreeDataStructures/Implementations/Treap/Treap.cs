using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

// Декартово дерево (Treap = Tree + Heap).
// Объединяет два свойства:
// - По ключу (Key) — бинарное дерево поиска (BST):
//   левый потомок меньше, правый больше.
// - По приоритету (Priority) — куча (обычно max-куча):
//   приоритет родителя всегда больше приоритетов детей.
// Приоритеты назначаются случайно, что даёт вероятностную
// сбалансированность: высота дерева O(log n) в среднем.
// Основные операции: Split (разделение) и Merge (слияние).
public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    public Treap() : base() { }

    public Treap(IComparer<TKey>? comparer) : base(comparer) { }

    // Генератор случайных чисел для приоритетов.
    private Random _random = Random.Shared;

    // Разделяет дерево с корнем root на два дерева по ключу key.
    // В левое дерево попадают все узлы с ключом < key,
    // в правое — все узлы с ключом >= key.
    // Возвращает кортеж (левое, правое).
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right)
        Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null)
            return (null, null);

        int cmp = Comparer.Compare(key, root.Key);

        // Ключ меньше корня — корень и его правое поддерево уходят в правое дерево,
        // а левое поддерево рекурсивно разделяется.
        if (cmp < 0)
        {
            var (left, right) = Split(root.Left, key);
            root.Left = right;
            if (right != null) right.Parent = root;
            if (left != null) left.Parent = null;
            return (left, root);
        }
        // Ключ больше или равен корню — корень и его левое поддерево уходят в левое дерево,
        // а правое поддерево рекурсивно разделяется.
        else
        {
            var (left, right) = Split(root.Right, key);
            root.Right = left;
            if (left != null) left.Parent = root;
            if (right != null) right.Parent = null;
            return (root, right);
        }
    }

    // Сливает два дерева left и right в одно.
    // Предусловие: все ключи в left строго меньше всех ключей в right.
    // Корнем становится узел с максимальным приоритетом.
    protected virtual TreapNode<TKey, TValue>?
        Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        // У левого корня приоритет больше — он становится корнем.
        // Его левое поддерево не трогаем, правое сливаем с right.
        if (left.Priority > right.Priority)
        {
            left.Right = Merge(left.Right, right);
            if (left.Right != null) left.Right.Parent = left;
            return left;
        }
        // У правого корня приоритет больше или равен — он становится корнем.
        // Его правое поддерево не трогаем, левое сливаем с left.
        else
        {
            right.Left = Merge(left, right.Left);
            if (right.Left != null) right.Left.Parent = right;
            return right;
        }
    }

    // Вставка нового ключа со значением.
    // Если ключ уже существует, старый узел удаляется, затем вставляется новый.
    // Использует Split + Merge: разрезаем дерево по ключу, вставляем новый узел
    // как отдельное дерево, сливаем все три части.
    public override void Add(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        // Удаляем старый узел с таким же ключом, если он есть.
        Root = RemoveRecursive(Root, key);

        // Создаём новый узел.
        var newNode = CreateNode(key, value);

        // Разрезаем дерево по ключу: left (< key), right (>= key).
        var (left, right) = Split(Root, key);

        // Сливаем: left + новый узел + right.
        Root = Merge(Merge(left, newNode), right);

        Count++;
        OnNodeAdded(newNode);
    }

    // Рекурсивное удаление узла с заданным ключом.
    // Возвращает новый корень поддерева после удаления.
    private TreapNode<TKey, TValue>? RemoveRecursive(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root == null) return null;

        int cmp = Comparer.Compare(key, root.Key);

        // Нашли узел — сливаем его левое и правое поддеревья,
        // тем самым исключая его из дерева.
        if (cmp == 0)
        {
            var merged = Merge(root.Left, root.Right);
            if (merged != null) merged.Parent = root.Parent;
            root.Left = root.Right = root.Parent = null;
            Count--;
            return merged;
        }
        // Ключ меньше — ищем в левом поддереве.
        else if (cmp < 0)
        {
            root.Left = RemoveRecursive(root.Left, key);
            if (root.Left != null) root.Left.Parent = root;
        }
        // Ключ больше — ищем в правом поддереве.
        else
        {
            root.Right = RemoveRecursive(root.Right, key);
            if (root.Right != null) root.Right.Parent = root;
        }

        return root;
    }

    // Удаление ключа из дерева. Возвращает true, если ключ был найден и удалён.
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

    // Фабричный метод: создаёт новый узел Treap со случайным приоритетом.
    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value);
    }

    // Колбэки не используются в Treap, т.к. балансировка происходит
    // не после вставки/удаления, а в самих операциях Split/Merge.
    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }

    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }
}