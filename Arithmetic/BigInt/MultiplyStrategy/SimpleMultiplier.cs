using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class DivideAndConquerMultiplier : IMultiplier
{
    private const int BaseCaseThreshold = 32;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        bool isNegative = a.IsNegative ^ b.IsNegative;
        uint[] product = MultiplyMagnitude(a.GetDigits(), b.GetDigits());
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    private static uint[] MultiplyMagnitude(uint[] left, uint[] right)
    {
        // Base case: маленькие числа — столбиком
        if (left.Length <= BaseCaseThreshold || right.Length <= BaseCaseThreshold)
        {
            return SimpleMultiplier.MultiplyMagnitude(left, right);
        }

        // Длина: максимальная из двух, округлённая до чётного
        int n = Math.Max(left.Length, right.Length);
        int m = (n + 1) / 2;

        // Разбиваем левое число: left = a * BASE^m + b
        uint[] a = GetHighPart(left, m);
        uint[] b = GetLowPart(left, m);

        // Разбиваем правое число: right = c * BASE^m + d
        uint[] c = GetHighPart(right, m);
        uint[] d = GetLowPart(right, m);

        // Рекурсивно вычисляем ЧЕТЫРЕ произведения
        uint[] ac = MultiplyMagnitude(a, c);
        uint[] ad = MultiplyMagnitude(a, d);
        uint[] bc = MultiplyMagnitude(b, c);
        uint[] bd = MultiplyMagnitude(b, d);

        // Собираем результат: ac * BASE^(2m) + (ad + bc) * BASE^m + bd
        uint[] ad_plus_bc = AddMagnitudes(ad, bc);
        uint[] ac_shifted = ShiftLeft(ac, 2 * m);
        uint[] mid_shifted = ShiftLeft(ad_plus_bc, m);

        return AddMagnitudes(AddMagnitudes(ac_shifted, mid_shifted), bd);
    }

    // Берёт старшую часть числа: цифры от индекса m и выше
    private static uint[] GetHighPart(uint[] digits, int m)
    {
        if (digits.Length <= m)
            return [0];

        int highLength = digits.Length - m;
        uint[] high = new uint[highLength];
        Array.Copy(digits, m, high, 0, highLength);
        return high;
    }

    // Берёт младшую часть числа: первые m цифр
    private static uint[] GetLowPart(uint[] digits, int m)
    {
        int lowLength = Math.Min(digits.Length, m);
        if (lowLength <= 0)
            return [0];

        uint[] low = new uint[lowLength];
        Array.Copy(digits, 0, low, 0, lowLength);
        return low;
    }

    // Сложение двух неотрицательных чисел
    private static uint[] AddMagnitudes(uint[] left, uint[] right)
    {
        int maxLen = Math.Max(left.Length, right.Length);
        uint[] result = new uint[maxLen + 1];
        ulong carry = 0;

        for (int i = 0; i < maxLen; i++)
        {
            ulong a = i < left.Length ? left[i] : 0;
            ulong b = i < right.Length ? right[i] : 0;
            ulong sum = a + b + carry;
            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        if (carry > 0)
            result[maxLen] = (uint)carry;

        return Normalize(result);
    }

    // Сдвиг влево на shiftAmount разрядов (умножение на BASE^shiftAmount)
    private static uint[] ShiftLeft(uint[] digits, int shiftAmount)
    {
        if (shiftAmount <= 0 || digits.Length == 0)
            return digits;

        uint[] shifted = new uint[digits.Length + shiftAmount];
        Array.Copy(digits, 0, shifted, shiftAmount, digits.Length);
        return shifted;
    }

    // Убирает ведущие нули
    private static uint[] Normalize(uint[] digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
            length--;

        if (length == 0)
            return [0];

        if (length == digits.Length)
            return digits;

        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }
}
