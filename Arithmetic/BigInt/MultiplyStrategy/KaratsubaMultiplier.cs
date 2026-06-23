using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int SchoolbookThreshold = 32;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        bool isNegative = a.IsNegative ^ b.IsNegative;
        uint[] left = Normalize(a.ToDigitArray());
        uint[] right = Normalize(b.ToDigitArray());
        uint[] product = MultiplyKaratsuba(left, right);
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    private static uint[] MultiplyKaratsuba(uint[] left, uint[] right)
    {
        left = Normalize(left);
        right = Normalize(right);

        int leftLength = left.Length;
        int rightLength = right.Length;
        if (leftLength == 0 || rightLength == 0)
        {
            return [];
        }

        int n = Math.Max(leftLength, rightLength);
        if (n <= SchoolbookThreshold)
        {
            return SimpleMultiplier.MultiplyMagnitude(left, right);
        }

        int split = n / 2;

        uint[] leftLow = Slice(left, 0, Math.Min(split, leftLength));
        uint[] leftHigh = Slice(left, Math.Min(split, leftLength), leftLength - Math.Min(split, leftLength));
        uint[] rightLow = Slice(right, 0, Math.Min(split, rightLength));
        uint[] rightHigh = Slice(right, Math.Min(split, rightLength), rightLength - Math.Min(split, rightLength));

        uint[] z0 = MultiplyKaratsuba(leftLow, rightLow);
        uint[] z2 = MultiplyKaratsuba(leftHigh, rightHigh);

        uint[] sumLeft = AddMagnitude(leftLow, leftHigh);
        uint[] sumRight = AddMagnitude(rightLow, rightHigh);
        uint[] z1 = MultiplyKaratsuba(sumLeft, sumRight);
        z1 = SubtractMagnitude(z1, z0);
        z1 = SubtractMagnitude(z1, z2);

        uint[] partLow = z0;
        uint[] partMid = ShiftWords(z1, split);
        uint[] partHigh = ShiftWords(z2, 2 * split);

        return AddMagnitude(partLow, AddMagnitude(partMid, partHigh));
    }

    private static uint[] AddMagnitude(uint[] left, uint[] right)
    {
        int max = Math.Max(left.Length, right.Length);
        uint[] result = new uint[max + 1];
        ulong carry = 0;

        for (int i = 0; i < max; i++)
        {
            ulong current = carry;
            if (i < left.Length)
            {
                current += left[i];
            }

            if (i < right.Length)
            {
                current += right[i];
            }

            result[i] = (uint)current;
            carry = current >> 32;
        }

        result[max] = (uint)carry;
        return Normalize(result);
    }

    private static uint[] SubtractMagnitude(uint[] left, uint[] right)
    {
        if (CompareMagnitude(left, right) < 0)
        {
            throw new InvalidOperationException("Karatsuba intermediate result became negative.");
        }

        uint[] result = new uint[left.Length];
        long borrow = 0;

        for (int i = 0; i < left.Length; i++)
        {
            long current = (long)left[i] - borrow - (i < right.Length ? right[i] : 0);
            if (current < 0)
            {
                current += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)current;
        }

        return Normalize(result);
    }

    private static int CompareMagnitude(uint[] left, uint[] right)
    {
        int leftLength = left.Length;
        int rightLength = right.Length;
        if (leftLength != rightLength)
        {
            return leftLength.CompareTo(rightLength);
        }

        for (int i = leftLength - 1; i >= 0; i--)
        {
            if (left[i] != right[i])
            {
                return left[i] < right[i] ? -1 : 1;
            }
        }

        return 0;
    }

    private static uint[] ShiftWords(uint[] digits, int wordShift)
    {
        if (digits.Length == 0)
        {
            return [];
        }

        if (wordShift <= 0)
        {
            return [.. digits];
        }

        uint[] result = new uint[digits.Length + wordShift];
        Array.Copy(digits, 0, result, wordShift, digits.Length);
        return result;
    }

    private static uint[] Slice(uint[] source, int start, int length)
    {
        if (length <= 0)
        {
            return [];
        }

        uint[] result = new uint[length];
        Array.Copy(source, start, result, 0, length);
        return Normalize(result);
    }

    private static uint[] Normalize(uint[] digits)
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