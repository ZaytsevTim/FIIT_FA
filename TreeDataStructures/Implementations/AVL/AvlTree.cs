using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

// AVL-дерево — самобалансирующееся бинарное дерево поиска.
// Поддерживает инвариант: для любого узла разница высот левого и правого
// поддеревьев (balance factor) не превышает 1.
// При нарушении выполняются вращения: малое правое (LL), малое левое (RR),
// большое правое (LR), большое левое (RL).
public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
{
    // Конструктор по умолчанию.
    public AvlTree() : base() { }

    // Конструктор с пользовательским компаратором ключей.
    public AvlTree(IComparer<TKey>? comparer) : base(comparer) { }

    // Фабричный метод: создаёт новый узел AVL-дерева.
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new AvlNode<TKey, TValue>(key, value);
    }

    // Вызывается после вставки нового узла.
    // Начинаем балансировку от родителя вставленного узла вверх к корню.
    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        BalanceUpward(newNode.Parent);
    }

    // Вызывается после удаления узла.
    // parent — узел, чей ребёнок изменился (или был удалён).
    // Начинаем балансировку от parent вверх к корню.
    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        BalanceUpward(parent);
    }

    // Поднимаемся от указанного узла вверх к корню и восстанавливаем
    // AVL-инвариант на каждом шагу.
    private void BalanceUpward(AvlNode<TKey, TValue>? node)
    {
        while (node != null)
        {
            // Обновляем высоту текущего узла на основе высот детей.
            UpdateHeight(node);

            // Вычисляем баланс-фактор: разницу высот левого и правого поддеревьев.
            int balance = GetBalanceFactor(node);

            // Левый перекос (balance > 1): левое поддерево выше правого.
            if (balance > 1)
            {
                // Если у левого ребёнка правый перекос (balance < 0),
                // это случай LR (большой правый поворот).
                // Сначала делаем левый поворот вокруг левого ребёнка,
                // чтобы свести к случаю LL.
                if (GetBalanceFactor(node.Left) < 0)
                {
                    if (node.Left != null)
                    {
                        RotateLeft(node.Left);
                    }
                }

                // Малое правое вращение (LL или LR после сведения).
                RotateRight(node);

                // После вращения переходим к родителю (бывшему левому ребёнку,
                // который теперь на месте node).
                node = node.Parent;
            }
            // Правый перекос (balance < -1): правое поддерево выше левого.
            else if (balance < -1)
            {
                // Если у правого ребёнка левый перекос (balance > 0),
                // это случай RL (большой левый поворот).
                // Сначала делаем правый поворот вокруг правого ребёнка,
                // чтобы свести к случаю RR.
                if (GetBalanceFactor(node.Right) > 0)
                {
                    if (node.Right != null)
                    {
                        RotateRight(node.Right);
                    }
                }

                // Малое левое вращение (RR или RL после сведения).
                RotateLeft(node);

                // После вращения переходим к родителю.
                node = node.Parent;
            }

            // Поднимаемся на уровень выше.
            node = node?.Parent;
        }
    }

    // Пересчитывает высоту узла на основе высот его детей.
    // Высота листа (без детей) = 1.
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

    // Вычисляет баланс-фактор узла: разницу высот левого и правого поддеревьев.
    // Положительное значение — перекос влево, отрицательное — вправо.
    // Допустимые значения: -1, 0, 1.
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