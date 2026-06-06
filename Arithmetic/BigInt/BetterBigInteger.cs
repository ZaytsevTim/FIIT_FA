using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private const string DigitAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int _signBit;

    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits is null)
        {
            throw new ArgumentNullException(nameof(digits));
        }

        SetFromDigits(digits, isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
        : this(digits?.ToArray() ?? throw new ArgumentNullException(nameof(digits)), isNegative)
    {
    }

    public BetterBigInteger(string value, int radix)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Value cannot be empty.");
        }

        bool isNegative = false;
        int index = 0;
        if (trimmed[0] == '+' || trimmed[0] == '-')
        {
            isNegative = trimmed[0] == '-';
            index = 1;
        }

        if (index == trimmed.Length)
        {
            throw new FormatException("Value does not contain digits.");
        }

        uint[] current = [];
        for (int i = index; i < trimmed.Length; i++)
        {
            int digit = ParseDigit(trimmed[i]);
            if (digit >= radix)
            {
                throw new FormatException($"Digit '{trimmed[i]}' is not valid for base {radix}.");
            }

            current = MultiplyByUInt(current, (uint)radix);
            current = AddUInt(current, (uint)digit);
        }

        SetFromDigits(current, isNegative);
    }


    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }

    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (IsNegative != other.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        int magnitudeComparison = CompareMagnitude(GetDigits(), other.GetDigits());
        return IsNegative ? -magnitudeComparison : magnitudeComparison;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is null)
        {
            return false;
        }

        return IsNegative == other.IsNegative && GetDigits().SequenceEqual(other.GetDigits());
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);

        foreach (uint digit in GetDigits())
        {
            hash.Add(digit);
        }

        return hash.ToHashCode();
    }


    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.IsNegative == b.IsNegative)
        {
            return FromDigits(AddMagnitude(a.GetDigits(), b.GetDigits()), a.IsNegative);
        }

        int cmp = CompareMagnitude(a.GetDigits(), b.GetDigits());
        if (cmp == 0)
        {
            return Zero;
        }

        if (cmp > 0)
        {
            return FromDigits(SubtractMagnitude(a.GetDigits(), b.GetDigits()), a.IsNegative);
        }

        return FromDigits(SubtractMagnitude(b.GetDigits(), a.GetDigits()), b.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (a.IsZero)
        {
            return Zero;
        }

        return FromDigits(a.GetDigits(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        (uint[] quotient, _) = DivRemMagnitude(a.GetDigits(), b.GetDigits());
        bool isNegative = !IsZeroDigits(quotient) && (a.IsNegative ^ b.IsNegative);
        return FromDigits(quotient, isNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        (_, uint[] remainder) = DivRemMagnitude(a.GetDigits(), b.GetDigits());
        bool isNegative = !IsZeroDigits(remainder) && a.IsNegative;
        return FromDigits(remainder, isNegative);
    }


    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        IMultiplier multiplier = SelectMultiplier(a, b);
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);

        int width = GetBitLength(a.GetDigits()) + 1;
        uint[] twos = ToTwosComplement(a, WidthToWords(width));
        for (int i = 0; i < twos.Length; i++)
        {
            twos[i] = ~twos[i];
        }

        return FromTwosComplement(twos);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, static (x, y) => x & y);

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, static (x, y) => x | y);

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
        => ApplyBitwise(a, b, static (x, y) => x ^ y);

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || a.IsZero)
        {
            return FromDigits(a.GetDigits(), a.IsNegative);
        }

        return FromDigits(ShiftLeftMagnitude(a.GetDigits(), shift), a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || a.IsZero)
        {
            return FromDigits(a.GetDigits(), a.IsNegative);
        }

        if (!a.IsNegative)
        {
            return FromDigits(ShiftRightMagnitude(a.GetDigits(), shift), false);
        }

        BetterBigInteger adjustment = (One << shift) - One;
        BetterBigInteger shifted = FromDigits(ShiftRightMagnitude((a.Abs() + adjustment).GetDigits(), shift), false);
        return -shifted;
    }

    public static bool operator ==(BetterBigInteger? a, BetterBigInteger? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(BetterBigInteger? a, BetterBigInteger? b) => !(a == b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix));
        }

        if (IsZero)
        {
            return "0";
        }

        uint[] value = [.. GetDigits()];
        List<char> chars = [];

        while (!IsZeroDigits(value))
        {
            (value, uint remainder) = DivRemByUInt(value, (uint)radix);
            chars.Add(DigitAlphabet[(int)remainder]);
        }

        if (IsNegative)
        {
            chars.Add('-');
        }

        chars.Reverse();
        return new string([.. chars]);
    }


    internal static BetterBigInteger FromDigits(ReadOnlySpan<uint> digits, bool isNegative = false)
    {
        return new BetterBigInteger([.. digits], isNegative);
    }

    internal uint[] ToDigitArray() => [.. GetDigits()];

    internal int Sign => IsZero ? 0 : (IsNegative ? -1 : 1);

    internal BetterBigInteger Abs() => FromDigits(GetDigits(), false);

    private static IMultiplier SelectMultiplier(BetterBigInteger a, BetterBigInteger b)
    {
        int maxLength = Math.Max(TrimmedLength(a.GetDigits()), TrimmedLength(b.GetDigits()));
        if (maxLength < 32)
        {
            return new SimpleMultiplier();
        }

        if (maxLength < 256)
        {
            return new KaratsubaMultiplier();
        }

        return new FftMultiplier();
    }

    private static BetterBigInteger ApplyBitwise(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        int width = Math.Max(GetBitLength(a.GetDigits()), GetBitLength(b.GetDigits())) + 1;
        int words = WidthToWords(width);
        uint[] left = ToTwosComplement(a, words);
        uint[] right = ToTwosComplement(b, words);
        uint[] result = new uint[words];

        for (int i = 0; i < words; i++)
        {
            result[i] = op(left[i], right[i]);
        }

        return FromTwosComplement(result);
    }

    private static uint[] ToTwosComplement(BetterBigInteger value, int words)
    {
        uint[] result = new uint[words];
        ReadOnlySpan<uint> digits = value.GetDigits();
        int count = Math.Min(digits.Length, words);
        for (int i = 0; i < count; i++)
        {
            result[i] = digits[i];
        }

        if (!value.IsNegative)
        {
            return result;
        }

        for (int i = 0; i < words; i++)
        {
            result[i] = ~result[i];
        }

        AddOneInPlace(result);
        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] words)
    {
        uint[] copy = [.. words];
        bool isNegative = (copy[^1] & 0x80000000u) != 0;
        if (!isNegative)
        {
            return FromDigits(copy, false);
        }

        SubtractOneInPlace(copy);
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = ~copy[i];
        }

        return FromDigits(copy, true);
    }

    private static void AddOneInPlace(uint[] digits)
    {
        ulong carry = 1;
        for (int i = 0; i < digits.Length && carry != 0; i++)
        {
            ulong current = digits[i] + carry;
            digits[i] = (uint)current;
            carry = current >> 32;
        }
    }

    private static void SubtractOneInPlace(uint[] digits)
    {
        for (int i = 0; i < digits.Length; i++)
        {
            if (digits[i] != 0)
            {
                digits[i]--;
                return;
            }

            digits[i] = uint.MaxValue;
        }
    }

    private static (uint[] Quotient, uint[] Remainder) DivRemMagnitude(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        uint[] normalizedDividend = NormalizeDigits([.. dividend]);
        uint[] normalizedDivisor = NormalizeDigits([.. divisor]);
        if (normalizedDivisor.Length == 0)
        {
            throw new DivideByZeroException();
        }

        if (normalizedDividend.Length == 0)
        {
            return ([], []);
        }

        int cmp = CompareMagnitude(normalizedDividend, normalizedDivisor);
        if (cmp < 0)
        {
            return ([], normalizedDividend);
        }

        if (cmp == 0)
        {
            return ([1u], []);
        }

        int shift = GetBitLength(normalizedDividend) - GetBitLength(normalizedDivisor);
        uint[] quotient = new uint[(shift / 32) + 1];
        uint[] remainder = normalizedDividend;
        uint[] shiftedDivisor = ShiftLeftMagnitude(normalizedDivisor, shift);

        for (int bit = shift; bit >= 0; bit--)
        {
            if (CompareMagnitude(remainder, shiftedDivisor) >= 0)
            {
                remainder = SubtractMagnitude(remainder, shiftedDivisor);
                SetBit(quotient, bit);
            }

            shiftedDivisor = ShiftRightMagnitude(shiftedDivisor, 1);
        }

        return (NormalizeDigits(quotient), NormalizeDigits(remainder));
    }

    private static (uint[] Quotient, uint Remainder) DivRemByUInt(uint[] dividend, uint divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        if (dividend.Length == 0)
        {
            return ([], 0);
        }

        uint[] quotient = new uint[dividend.Length];
        ulong remainder = 0;
        for (int i = dividend.Length - 1; i >= 0; i--)
        {
            ulong current = (remainder << 32) | dividend[i];
            quotient[i] = (uint)(current / divisor);
            remainder = current % divisor;
        }

        return (NormalizeDigits(quotient), (uint)remainder);
    }

    private static void SetBit(uint[] digits, int bitIndex)
    {
        int wordIndex = bitIndex / 32;
        int offset = bitIndex % 32;
        digits[wordIndex] |= 1u << offset;
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        int length = TrimmedLength(digits);
        if (length == 0)
        {
            return [];
        }

        if (shift == 0)
        {
            return NormalizeDigits([.. digits[..length]]);
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[length + wordShift + 1];
        ulong carry = 0;

        for (int i = 0; i < length; i++)
        {
            ulong current = ((ulong)digits[i] << bitShift) | carry;
            result[i + wordShift] = (uint)current;
            carry = current >> 32;
        }

        result[length + wordShift] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        int length = TrimmedLength(digits);
        if (length == 0)
        {
            return [];
        }

        if (shift == 0)
        {
            return NormalizeDigits([.. digits[..length]]);
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        if (wordShift >= length)
        {
            return [];
        }

        int resultLength = length - wordShift;
        uint[] result = new uint[resultLength];

        if (bitShift == 0)
        {
            for (int i = 0; i < resultLength; i++)
            {
                result[i] = digits[i + wordShift];
            }

            return NormalizeDigits(result);
        }

        uint carry = 0;
        for (int i = length - 1; i >= wordShift; i--)
        {
            uint current = digits[i];
            result[i - wordShift] = (current >> bitShift) | (carry << (32 - bitShift));
            carry = current;
        }

        return NormalizeDigits(result);
    }

    private static uint[] AddMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = TrimmedLength(left);
        int rightLength = TrimmedLength(right);
        int maxLength = Math.Max(leftLength, rightLength);
        uint[] result = new uint[maxLength + 1];
        ulong carry = 0;

        for (int i = 0; i < maxLength; i++)
        {
            ulong current = carry;
            if (i < leftLength)
            {
                current += left[i];
            }

            if (i < rightLength)
            {
                current += right[i];
            }

            result[i] = (uint)current;
            carry = current >> 32;
        }

        result[maxLength] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] AddUInt(ReadOnlySpan<uint> digits, uint value)
    {
        if (value == 0)
        {
            return NormalizeDigits([.. digits]);
        }

        int length = TrimmedLength(digits);
        if (length == 0)
        {
            return [value];
        }

        uint[] result = new uint[length + 1];
        for (int i = 0; i < length; i++)
        {
            result[i] = digits[i];
        }

        ulong carry = value;
        int index = 0;
        while (carry != 0)
        {
            ulong current = result[index] + carry;
            result[index] = (uint)current;
            carry = current >> 32;
            index++;
        }

        return NormalizeDigits(result);
    }

    private static uint[] MultiplyByUInt(ReadOnlySpan<uint> digits, uint multiplier)
    {
        int length = TrimmedLength(digits);
        if (length == 0 || multiplier == 0)
        {
            return [];
        }

        if (multiplier == 1)
        {
            return NormalizeDigits([.. digits[..length]]);
        }

        uint[] result = new uint[length + 1];
        ulong carry = 0;
        for (int i = 0; i < length; i++)
        {
            ulong current = (ulong)digits[i] * multiplier + carry;
            result[i] = (uint)current;
            carry = current >> 32;
        }

        result[length] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] SubtractMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = TrimmedLength(left);
        int rightLength = TrimmedLength(right);
        uint[] result = new uint[leftLength];
        long borrow = 0;

        for (int i = 0; i < leftLength; i++)
        {
            long current = (long)left[i] - borrow - (i < rightLength ? right[i] : 0);
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

        return NormalizeDigits(result);
    }

    private static int CompareMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int leftLength = TrimmedLength(left);
        int rightLength = TrimmedLength(right);
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

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        int length = TrimmedLength(digits);
        if (length == 0)
        {
            return 0;
        }

        uint mostSignificant = digits[length - 1];
        int bits = 32;
        while ((mostSignificant & 0x80000000u) == 0)
        {
            mostSignificant <<= 1;
            bits--;
        }

        return (length - 1) * 32 + bits;
    }

    private static int WidthToWords(int bitWidth)
    {
        return Math.Max(1, (bitWidth + 31) / 32);
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

    private static bool IsZeroDigits(ReadOnlySpan<uint> digits) => TrimmedLength(digits) == 0;

    private void SetFromDigits(uint[] digits, bool isNegative)
    {
        uint[] normalized = NormalizeDigits(digits);
        if (normalized.Length == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;
        if (normalized.Length == 1)
        {
            _smallValue = normalized[0];
            _data = null;
            if (_smallValue == 0)
            {
                _signBit = 0;
            }
            return;
        }

        _smallValue = 0;
        _data = normalized;
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

        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }

    private static int ParseDigit(char c)
    {
        if (c is >= '0' and <= '9')
        {
            return c - '0';
        }

        if (c is >= 'A' and <= 'Z')
        {
            return c - 'A' + 10;
        }

        if (c is >= 'a' and <= 'z')
        {
            return c - 'a' + 10;
        }

        throw new FormatException($"Invalid digit '{c}'.");
    }

    private bool IsZero => _data == null && _smallValue == 0;

    private static BetterBigInteger Zero => new([0u]);
    private static BetterBigInteger One => new([1u]);
}