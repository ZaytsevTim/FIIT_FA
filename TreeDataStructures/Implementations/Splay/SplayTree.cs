using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

// Splay-дерево (Косое дерево) — самобалансирующееся бинарное дерево поиска.
// Не хранит явной информации о балансе (высот, цветов).
// Вместо этого после каждой операции (поиск, вставка, удаление) узел,
// к которому обратились, поднимается в корень с помощью операции splay.
// Это даёт амортизированную сложность O(log n) на операцию.
// Часто используемые элементы оказываются ближе к корню
public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
{
    public SplayTree() : base() { }

    public SplayTree(IComparer<TKey>? comparer) : base(comparer) { }

    // Фабричный метод: создаёт новый узел.
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new BstNode<TKey, TValue>(key, value);
    }

    // После вставки нового узла поднимаем его в корень.
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }

    // После удаления узла поднимаем родителя удалённого узла в корень.
    // parent — узел, чей ребёнок был удалён или изменился.
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            Splay(parent);
        }
    }

    // Поиск значения по ключу.
    // Если узел найден — поднимаем его в корень и возвращаем значение.
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

    // Проверка наличия ключа.
    // Если ключ найден — поднимаем соответствующий узел в корень.
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

    // Операция splay: поднимает узел node в корень дерева
    // с помощью серии вращений.
    // Различают три типа шагов:
    // - Zig: родитель узла — корень (один поворот).
    // - Zig-Zig: узел и родитель оба слева или оба справа
    //   (два поворота в одну сторону).
    // - Zig-Zag: узел слева, родитель справа, или наоборот
    //   (два поворота в разные стороны, как большое вращение в AVL).
    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null)
        {
            // Zig: родитель — корень.
            // Один поворот вокруг родителя.
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
            // Zig-Zig: узел и родитель оба левые дети.
            // Два правых поворота: сначала дедушка, потом родитель.
            else if (node.IsLeftChild && node.Parent.IsLeftChild)
            {
                RotateRight(node.Parent.Parent);
                RotateRight(node.Parent);
            }
            // Zig-Zig: узел и родитель оба правые дети.
            // Два левых поворота: сначала дедушка, потом родитель.
            else if (node.IsRightChild && node.Parent.IsRightChild)
            {
                RotateLeft(node.Parent.Parent);
                RotateLeft(node.Parent);
            }
            // Zig-Zag: узел — правый ребёнок, родитель — левый ребёнок.
            // Левый поворот вокруг родителя, затем правый вокруг бывшего родителя.
            else if (node.IsRightChild && node.Parent.IsLeftChild)
            {
                RotateLeft(node.Parent);
                RotateRight(node.Parent); // node.Parent теперь — бывший родитель родителя
            }
            // Zig-Zag: узел — левый ребёнок, родитель — правый ребёнок.
            // Правый поворот вокруг родителя, затем левый вокруг бывшего родителя.
            else
            {
                RotateRight(node.Parent);
                RotateLeft(node.Parent);
            }
        }

        // После всех вращений узел становится корнем.
        Root = node;
    }
}