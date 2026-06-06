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

    private static int TrimmedLength(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        return length;
    }

    private static uint[] NormalizeDigits(uint[] digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        if (length == 0)
        {
            return [];
        }

        if (length == digits.Length)
        {
            return digits;
        }

        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }
}