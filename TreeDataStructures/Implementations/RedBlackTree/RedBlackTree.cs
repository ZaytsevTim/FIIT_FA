using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

// Красно-чёрное дерево (Red-Black Tree) — самобалансирующееся BST.
// Инварианты:
// 1. Каждый узел либо красный, либо чёрный.
// 2. Корень всегда чёрный.
// 3. У красного узла оба ребёнка чёрные (нет двух красных подряд).
// 4. Все пути от любого узла до null-листьев содержат одинаковое
//    количество чёрных узлов (чёрная высота).
public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    public RedBlackTree() : base() { }

    public RedBlackTree(IComparer<TKey>? comparer) : base(comparer) { }

    // Фабричный метод: создаёт новый узел КЧД.
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RbNode<TKey, TValue>(key, value);
    }

    // Вызывается после вставки нового узла.
    // Новый узел всегда красится в красный, затем выполняется
    // балансировка для восстановления инвариантов КЧД.
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        newNode.Color = RbColor.Red;
        BalanceAfterInsert(newNode);
    }

    // Вызывается после удаления узла.
    // Если замещающий узел чёрный — чёрная высота могла нарушиться,
    // требуется балансировка.
    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (child != null && child.Color == RbColor.Black)
        {
            BalanceAfterRemove(child, parent);
        }
    }

    // Балансировка после вставки.
    // Поднимаемся вверх, пока узел не станет корнем или его родитель
    // не станет чёрным (тогда два красных подряд отсутствуют).
    private void BalanceAfterInsert(RbNode<TKey, TValue> node)
    {
        while (node.Parent != null && node.Parent.Color == RbColor.Red)
        {
            // Родитель — левый ребёнок дедушки.
            if (node.Parent.IsLeftChild)
            {
                // Дядя — правый ребёнок дедушки.
                RbNode<TKey, TValue>? uncle = node.Parent.Parent?.Right;

                // Случай 1: дядя красный.
                // Перекрашиваем родителя и дядю в чёрный, дедушку в красный.
                // Проблема переходит на уровень дедушки.
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    node.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    // Случай 2: дядя чёрный, узел — правый ребёнок (внутренний внук).
                    // Левый поворот вокруг родителя сводит к Случаю 3.
                    if (node.IsRightChild)
                    {
                        node = node.Parent;
                        RotateLeft(node);
                    }

                    // Случай 3: дядя чёрный, узел — левый ребёнок (внешний внук).
                    // Правый поворот вокруг дедушки + перекраска.
                    node.Parent!.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    RotateRight(node.Parent.Parent);
                }
            }
            // Родитель — правый ребёнок дедушки (зеркальный случай).
            else
            {
                // Дядя — левый ребёнок дедушки.
                RbNode<TKey, TValue>? uncle = node.Parent.Parent?.Left;

                // Случай 1: дядя красный (зеркально).
                if (uncle != null && uncle.Color == RbColor.Red)
                {
                    node.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    // Случай 2: дядя чёрный, узел — левый ребёнок (внутренний внук).
                    if (node.IsLeftChild)
                    {
                        node = node.Parent;
                        RotateRight(node);
                    }

                    // Случай 3: дядя чёрный, узел — правый ребёнок (внешний внук).
                    node.Parent!.Color = RbColor.Black;
                    node.Parent.Parent!.Color = RbColor.Red;
                    RotateLeft(node.Parent.Parent);
                }
            }
        }

        // Корень всегда должен быть чёрным.
        Root!.Color = RbColor.Black;
    }

    // Балансировка после удаления.
    // node — узел, который встал на место удалённого.
    // parent — его родитель.
    // Цель: восстановить чёрную высоту, нарушенную удалением чёрного узла.
    private void BalanceAfterRemove(RbNode<TKey, TValue> node, RbNode<TKey, TValue>? parent)
    {
        while (node != Root && GetColor(node) == RbColor.Black)
        {
            // Узел — левый ребёнок родителя.
            if (node == parent?.Left)
            {
                // Брат — правый ребёнок родителя.
                RbNode<TKey, TValue>? sibling = parent.Right;

                // Случай 1: брат красный.
                // Перекрашиваем брата в чёрный, родителя в красный,
                // левый поворот вокруг родителя. Сводим к случаям 2-4.
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent.Color = RbColor.Red;
                    RotateLeft(parent);
                    sibling = parent.Right;
                }

                // Случай 2: брат чёрный, оба его ребёнка чёрные.
                // Красим брата в красный, снимаем лишнюю черноту с node,
                // передаём проблему родителю.
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
                    // Случай 3: брат чёрный, дальний ребёнок чёрный,
                    // ближний — красный.
                    // Правый поворот вокруг брата + перекраска → Случай 4.
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

                    // Случай 4: брат чёрный, дальний ребёнок красный.
                    // Левый поворот вокруг родителя + перекраска.
                    // Баланс восстановлен — выходим из цикла.
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
            // Узел — правый ребёнок родителя (зеркальный случай).
            else
            {
                // Брат — левый ребёнок родителя.
                RbNode<TKey, TValue>? sibling = parent?.Left;

                // Случай 1: брат красный (зеркально).
                if (GetColor(sibling) == RbColor.Red)
                {
                    sibling!.Color = RbColor.Black;
                    parent!.Color = RbColor.Red;
                    RotateRight(parent);
                    sibling = parent.Left;
                }

                // Случай 2: брат чёрный, оба ребёнка чёрные (зеркально).
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
                    // Случай 3: брат чёрный, дальний чёрный,
                    // ближний красный (зеркально).
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

                    // Случай 4: брат чёрный, дальний красный (зеркально).
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

        // Узел стал корнем или красным — красим в чёрный.
        node.Color = RbColor.Black;
    }

    // Возвращает цвет узла. Null-листья считаются чёрными.
    private RbColor GetColor(RbNode<TKey, TValue>? node)
    {
        return node?.Color ?? RbColor.Black;
    }
}