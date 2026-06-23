using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        bool isNegative = a.IsNegative ^ b.IsNegative;
        uint[] product = MultiplyMagnitude(a.GetDigits(), b.GetDigits());
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    internal static uint[] MultiplyMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = TrimmedLength(left);
        int rightLength = TrimmedLength(right);
        if (leftLength == 0 || rightLength == 0)
        {
            return [];
        }

        // Base case: маленькие числа — столбиком
        if (leftLength <= 32 || rightLength <= 32)
        {
            return MultiplyMagnitudeNaive(left, right, leftLength, rightLength);
        }

        // Разбиение на половинки
        int m = (Math.Max(leftLength, rightLength) + 1) / 2;

        uint[] a = GetHighPart(left, m, leftLength);
        uint[] b = GetLowPart(left, m, leftLength);
        uint[] c = GetHighPart(right, m, rightLength);
        uint[] d = GetLowPart(right, m, rightLength);

        // Рекурсивно 4 умножения
        uint[] ac = MultiplyMagnitude(a, b);
        uint[] ad = MultiplyMagnitude(a, d);
        uint[] bc = MultiplyMagnitude(b, c);
        uint[] bd = MultiplyMagnitude(b, d);

        // Сборка: ac * BASE^(2m) + (ad + bc) * BASE^m + bd
        uint[] ad_plus_bc = AddMagnitudes(ad, bc);
        uint[] ac_shifted = ShiftLeft(ac, 2 * m);
        uint[] mid_shifted = ShiftLeft(ad_plus_bc, m);

        return AddMagnitudes(AddMagnitudes(ac_shifted, mid_shifted), bd);
    }

    // Наивное умножение столбиком
    private static uint[] MultiplyMagnitudeNaive(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right, int leftLength, int rightLength)
    {
        uint[] result = new uint[leftLength + rightLength];
        for (int i = 0; i < leftLength; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < rightLength; j++)
            {
                ulong current = result[i + j] + carry + (ulong)left[i] * right[j];
                result[i + j] = (uint)current;
                carry = current >> 32;
            }

            int index = i + rightLength;
            while (carry != 0)
            {
                ulong current = result[index] + carry;
                result[index] = (uint)current;
                carry = current >> 32;
                index++;
            }
        }

        return NormalizeDigits(result);
    }

    private static uint[] GetHighPart(ReadOnlySpan<uint> digits, int m, int length)
    {
        if (length <= m)
            return [0];

        int highLength = length - m;
        uint[] high = new uint[highLength];
        for (int i = 0; i < highLength; i++)
            high[i] = digits[m + i];
        return high;
    }

    private static uint[] GetLowPart(ReadOnlySpan<uint> digits, int m, int length)
    {
        int lowLength = Math.Min(length, m);
        if (lowLength <= 0)
            return [0];

        uint[] low = new uint[lowLength];
        for (int i = 0; i < lowLength; i++)
            low[i] = digits[i];
        return low;
    }

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

        return NormalizeDigits(result);
    }

    private static uint[] ShiftLeft(uint[] digits, int shiftAmount)
    {
        if (shiftAmount <= 0 || digits.Length == 0)
            return digits;

        uint[] shifted = new uint[digits.Length + shiftAmount];
        Array.Copy(digits, 0, shifted, shiftAmount, digits.Length);
        return shifted;
    }

    private static int TrimmedLength(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
            length--;
        return length;
    }

    private static uint[] NormalizeDigits(uint[] digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
            length--;

        if (length == 0)
            return [];

        if (length == digits.Length)
            return digits;

        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }
}