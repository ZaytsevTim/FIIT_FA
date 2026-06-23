using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    // Порог: если длина числа (в 32-битных словах) меньше или равна 32,
    // переключаемся на обычное умножение столбиком.
    private const int SchoolbookThreshold = 32;

    // Главный метод умножения двух больших чисел.
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        // Проверка на null.
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        // Знак результата: минус, если знаки разные (XOR).
        bool isNegative = a.IsNegative ^ b.IsNegative;

        // Получаем массивы 32-битных слов для каждого числа.
        // Normalize убирает ведущие нули.
        uint[] left = Normalize(a.ToDigitArray());
        uint[] right = Normalize(b.ToDigitArray());

        // Рекурсивное умножение Карацубы.
        uint[] product = MultiplyKaratsuba(left, right);

        // Собираем результат из массива слов и знака.
        return BetterBigInteger.FromDigits(product, isNegative);
    }

    // Рекурсивный алгоритм Карацубы.
    // Числа представлены как массивы uint[] (по основанию 2^32).
    private static uint[] MultiplyKaratsuba(uint[] left, uint[] right)
    {
        // Убираем ведущие нули на всякий случай.
        left = Normalize(left);
        right = Normalize(right);

        int leftLength = left.Length;
        int rightLength = right.Length;

        // Если одно из чисел пустое, результат тоже пустой.
        if (leftLength == 0 || rightLength == 0)
        {
            return [];
        }

        // n — максимальная длина из двух чисел.
        int n = Math.Max(leftLength, rightLength);

        // Base case: если числа маленькие, умножаем столбиком.
        if (n <= SchoolbookThreshold)
        {
            return SimpleMultiplier.MultiplyMagnitude(left, right);
        }

        // Точка разбиения: делим числа пополам.
        // split — размер младшей части в словах.
        int split = n / 2;

        // Разбиваем левое число на младшую (low) и старшую (high) части.
        // Slice вырезает подмассив от start длиной length.
        uint[] leftLow = Slice(left, 0, Math.Min(split, leftLength));
        uint[] leftHigh = Slice(left, Math.Min(split, leftLength),
                                 leftLength - Math.Min(split, leftLength));

        // Разбиваем правое число так же.
        uint[] rightLow = Slice(right, 0, Math.Min(split, rightLength));
        uint[] rightHigh = Slice(right, Math.Min(split, rightLength),
                                  rightLength - Math.Min(split, rightLength));

        // Три рекурсивных умножения (вместо четырёх).
        // z0 = младшие части: a0 * b0
        uint[] z0 = MultiplyKaratsuba(leftLow, rightLow);

        // z2 = старшие части: a1 * b1
        uint[] z2 = MultiplyKaratsuba(leftHigh, rightHigh);

        // z1 = (a0 + a1) * (b0 + b1)
        uint[] sumLeft = AddMagnitude(leftLow, leftHigh);
        uint[] sumRight = AddMagnitude(rightLow, rightHigh);
        uint[] z1 = MultiplyKaratsuba(sumLeft, sumRight);

        // Хитрость Карацубы:
        // (a0 + a1)*(b0 + b1) = a0*b0 + a0*b1 + a1*b0 + a1*b1
        // Вычитаем z0 и z2, остаётся a0*b1 + a1*b0.
        z1 = SubtractMagnitude(z1, z0);
        z1 = SubtractMagnitude(z1, z2);

        // Собираем результат:
        // answer = z0 + z1 * B^split + z2 * B^(2*split)
        // где B = 2^32 (основание системы счисления).
        // Шифт - аналог умножения на степень основания типа 10^m
        // ShiftWords сдвигает массив на указанное количество слов (умножение на B^shift).
        uint[] partLow = z0;
        uint[] partMid = ShiftWords(z1, split);
        uint[] partHigh = ShiftWords(z2, 2 * split);

        return AddMagnitude(partLow, AddMagnitude(partMid, partHigh));
    }

    // Сложение двух массивов слов.
    // Каждое слово — uint (32 бита), сумма может быть до 64 бит (ulong).
    private static uint[] AddMagnitude(uint[] left, uint[] right)
    {
        int max = Math.Max(left.Length, right.Length);
        uint[] result = new uint[max + 1]; // +1 для возможного переноса
        ulong carry = 0;                   // перенос (0 или 1)

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

            // Младшие 32 бита — цифра результата.
            result[i] = (uint)current;
            // Старшие 32 бита — перенос.
            carry = current >> 32;
        }

        // Последний перенос.
        result[max] = (uint)carry;
        return Normalize(result);
    }

    // Вычитание двух массивов слов: left - right.
    // Предполагается, что left >= right.
    private static uint[] SubtractMagnitude(uint[] left, uint[] right)
    {
        // Проверяем, что левое число не меньше правого.
        if (CompareMagnitude(left, right) < 0)
        {
            throw new InvalidOperationException(
                "Karatsuba intermediate result became negative.");
        }

        uint[] result = new uint[left.Length];
        long borrow = 0; // заём (0 или 1)

        for (int i = 0; i < left.Length; i++)
        {
            // Вычитаем заём и цифру правого числа (если есть).
            long current = (long)left[i] - borrow -
                           (i < right.Length ? right[i] : 0);

            if (current < 0)
            {
                // Занимаем из следующего разряда.
                current += 1L << 32; // добавляем 2^32
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

    // Сравнение двух массивов слов по величине.
    // Возвращает -1 если left < right, 0 если равны, 1 если left > right.
    private static int CompareMagnitude(uint[] left, uint[] right)
    {
        int leftLength = left.Length;
        int rightLength = right.Length;

        // Если длины разные — больше то, которое длиннее.
        if (leftLength != rightLength)
        {
            return leftLength.CompareTo(rightLength);
        }

        // Длины одинаковые — сравниваем от старшего разряда к младшему.
        for (int i = leftLength - 1; i >= 0; i--)
        {
            if (left[i] != right[i])
            {
                return left[i] < right[i] ? -1 : 1;
            }
        }

        return 0; // Числа равны.
    }

    // Сдвиг массива слов влево на wordShift позиций.
    // Это эквивалентно умножению числа на (2^32)^wordShift.
    private static uint[] ShiftWords(uint[] digits, int wordShift)
    {
        // Если массив пустой — возвращаем пустой.
        if (digits.Length == 0)
        {
            return [];
        }

        // Если сдвиг нулевой или отрицательный — возвращаем копию.
        if (wordShift <= 0)
        {
            return [.. digits];
        }

        // Создаём массив побольше и копируем данные со сдвигом.
        uint[] result = new uint[digits.Length + wordShift];
        Array.Copy(digits, 0, result, wordShift, digits.Length);
        return result;
    }

    // Вырезает подмассив из source начиная с индекса start длиной length.
    private static uint[] Slice(uint[] source, int start, int length)
    {
        // Если длина нулевая или отрицательная — возвращаем пустой массив.
        if (length <= 0)
        {
            return [];
        }

        uint[] result = new uint[length];
        Array.Copy(source, start, result, 0, length);
        return Normalize(result);
    }

    // Убирает ведущие нули в массиве слов.
    // Например, [1, 2, 0, 0, 0] -> [1, 2].
    private static uint[] Normalize(uint[] digits)
    {
        int length = digits.Length;

        // Ищем первый ненулевой элемент с конца.
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        // Если все нули — возвращаем пустой массив.
        if (length == 0)
        {
            return [];
        }

        // Если ведущих нулей нет — возвращаем исходный массив.
        if (length == digits.Length)
        {
            return digits;
        }

        // Копируем только значащие разряды.
        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }
}