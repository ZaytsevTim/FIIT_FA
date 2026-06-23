using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    // Три модуля для NTT + CRT
    private static readonly long[] NTT_MODULI = {
        998244353,   // 119 * 2^23 + 1
        1004535809,  // 479 * 2^21 + 1
        469762049    // 7 * 2^26 + 1
    };

    // Соответствующие первообразные корни
    private static readonly long[] PRIMITIVE_ROOTS = { 3, 3, 3 };

    public BetterBigInteger Multiply(BetterBigInteger first, BetterBigInteger second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        if (first.Sign == 0 || second.Sign == 0)
            return BetterBigInteger.FromDigits([], false);

        bool resultIsNegative = first.IsNegative ^ second.IsNegative;
        string firstAbs = first.Abs().ToString(10);
        string secondAbs = second.Abs().ToString(10);

        int[] firstDigits = ToReversedDigits(firstAbs);
        int[] secondDigits = ToReversedDigits(secondAbs);
        long[] convolution = NttConvolveWithCRT(firstDigits, secondDigits);
        string product = NormalizeDecimalDigits(convolution);

        if (resultIsNegative && product != "0")
            product = "-" + product;

        return new BetterBigInteger(product, 10);
    }

    private static int[] ToReversedDigits(string value)
    {
        int[] digits = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
            digits[i] = value[value.Length - 1 - i] - '0';
        return digits;
    }

    // Свёртка через NTT с тремя модулями + CRT
    private static long[] NttConvolveWithCRT(int[] left, int[] right)
    {
        int fftSize = 1;
        int need = left.Length + right.Length;
        while (fftSize < need) fftSize <<= 1;

        long[][][] allConvolutions = new long[NTT_MODULI.Length][][];

        for (int modIndex = 0; modIndex < NTT_MODULI.Length; modIndex++)
        {
            long mod = NTT_MODULI[modIndex];
            long[] a = new long[fftSize];
            long[] b = new long[fftSize];

            for (int i = 0; i < left.Length; i++) a[i] = left[i] % mod;
            for (int i = 0; i < right.Length; i++) b[i] = right[i] % mod;

            Ntt(a, mod, PRIMITIVE_ROOTS[modIndex], false);
            Ntt(b, mod, PRIMITIVE_ROOTS[modIndex], false);

            for (int i = 0; i < fftSize; i++)
                a[i] = (a[i] * b[i]) % mod;

            Ntt(a, mod, PRIMITIVE_ROOTS[modIndex], true);
            allConvolutions[modIndex] = new long[][] { a };
        }

        long[] result = new long[need];
        for (int i = 0; i < need; i++)
        {
            result[i] = CRT(
                allConvolutions[0][0][i], NTT_MODULI[0],
                allConvolutions[1][0][i], NTT_MODULI[1],
                allConvolutions[2][0][i], NTT_MODULI[2]);
        }

        return result;
    }

    // NTT (Number Theoretic Transform)
    private static void Ntt(long[] values, long mod, long primitiveRoot, bool invert)
    {
        int n = values.Length;

        // Бит-реверсивная перестановка
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }
            j ^= bit;
            if (i < j)
                (values[i], values[j]) = (values[j], values[i]);
        }

        // Бабочки
        for (int len = 2; len <= n; len <<= 1)
        {
            long wlen = ModPow(primitiveRoot, (mod - 1) / len, mod);
            if (invert)
                wlen = ModPow(wlen, mod - 2, mod);

            for (int i = 0; i < n; i += len)
            {
                long w = 1;
                int half = len >> 1;
                for (int j = 0; j < half; j++)
                {
                    long u = values[i + j];
                    long v = (values[i + j + half] * w) % mod;
                    values[i + j] = (u + v) % mod;
                    values[i + j + half] = (u - v + mod) % mod;
                    w = (w * wlen) % mod;
                }
            }
        }

        if (invert)
        {
            long invN = ModPow(n, mod - 2, mod);
            for (int i = 0; i < n; i++)
                values[i] = (values[i] * invN) % mod;
        }
    }

    // Быстрое возведение в степень по модулю
    private static long ModPow(long baseValue, long exp, long mod)
    {
        long result = 1;
        long b = baseValue % mod;
        while (exp > 0)
        {
            if ((exp & 1) == 1)
                result = (result * b) % mod;
            b = (b * b) % mod;
            exp >>= 1;
        }
        return result;
    }

    // Китайская теорема об остатках (CRT)
    private static long CRT(long r1, long m1, long r2, long m2, long r3, long m3)
    {
        BigInteger bigM = (BigInteger)m1 * m2 * m3;
        BigInteger M1 = bigM / m1;
        BigInteger M2 = bigM / m2;
        BigInteger M3 = bigM / m3;

        long inv1 = ModInverse((long)(M1 % m1), m1);
        long inv2 = ModInverse((long)(M2 % m2), m2);
        long inv3 = ModInverse((long)(M3 % m3), m3);

        BigInteger result = (BigInteger)r1 * M1 * inv1 +
                            (BigInteger)r2 * M2 * inv2 +
                            (BigInteger)r3 * M3 * inv3;
        result %= bigM;
        return (long)result;
    }

    // Обратный элемент по модулю (расширенный алгоритм Евклида)
    private static long ModInverse(long a, long mod)
    {
        long m0 = mod, y = 0, x = 1;
        if (mod == 1) return 0;
        while (a > 1)
        {
            long q = a / mod;
            long t = mod;
            mod = a % mod;
            a = t;
            t = y;
            y = x - q * y;
            x = t;
        }
        if (x < 0) x += m0;
        return x;
    }

    // Нормализация десятичных разрядов

    private static string NormalizeDecimalDigits(long[] digits)
    {
        long carry = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            long current = digits[i] + carry;
            carry = current / 10;
            long remainder = current % 10;
            if (remainder < 0) { remainder += 10; carry--; }
            digits[i] = remainder;
        }

        while (carry > 0)
        {
            Array.Resize(ref digits, digits.Length + 1);
            digits[^1] = carry % 10;
            carry /= 10;
        }

        int last = digits.Length - 1;
        while (last > 0 && digits[last] == 0) last--;

        char[] chars = new char[last + 1];
        for (int i = 0; i <= last; i++)
            chars[i] = (char)('0' + digits[last - i]);

        return new string(chars);
    }
}