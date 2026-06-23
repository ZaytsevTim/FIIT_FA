using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    // Три простых модуля специального вида: M = c * 2^k + 1
    // Каждый модуль поддерживает FFT до размера 2^k
    // 998244353 = 119 * 2^23 + 1  →  поддерживает FFT до 2^23 точек
    // 1004535809 = 479 * 2^21 + 1 →  поддерживает FFT до 2^21 точек
    // 469762049 = 7 * 2^26 + 1    →  поддерживает FFT до 2^26 точек
    private static readonly long[] NttModuli =
    {
        998244353,
        1004535809,
        469762049
    };

    // Первообразные корни для каждого из трёх модулей
    // Число 3 является первообразным корнем для всех трёх модулей
    private static readonly long[] PrimitiveRoots = { 3, 3, 3 };

    // Главный метод умножения двух больших чисел
    public BetterBigInteger Multiply(BetterBigInteger first, BetterBigInteger second)
    {
        // Проверка на null
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        // Если хотя бы один множитель — ноль, результат тоже ноль
        if (first.Sign == 0 || second.Sign == 0)
            return BetterBigInteger.FromDigits([], false);

        // Определяем знак результата: минус, если знаки разные (XOR)
        bool resultIsNegative = first.IsNegative ^ second.IsNegative;

        // Берём модули чисел и переводим в десятичные строки
        string firstAbs = first.Abs().ToString(10);
        string secondAbs = second.Abs().ToString(10);

        // Превращаем строки в массивы цифр (младший разряд — по индексу 0)
        int[] firstDigits = ToReversedDigits(firstAbs);
        int[] secondDigits = ToReversedDigits(secondAbs);

        // Вычисляем свёртку через NTT с тремя модулями и CRT
        // NTT переводит многочлен в новую систему координат (значения многочлена в корнях из еденицы), где умножение поэлементное O(N) а не O(N^2), обратным НТТ переводим обратно
        long[] convolution = NttConvolveWithCrt(firstDigits, secondDigits);

        // Нормализуем: переносим десятки и собираем в строку
        string product = NormalizeDecimalDigits(convolution);

        // Приписываем минус, если результат отрицательный и не ноль
        if (resultIsNegative && product != "0")
            product = "-" + product;

        // Создаём новое большое число из строки
        return new BetterBigInteger(product, 10);
    }

    // Переворачивает строку и превращает символы в цифры
    // "1234" в [4, 3, 2, 1]  (индекс 0 = младший разряд)
    private static int[] ToReversedDigits(string value)
    {
        int[] digits = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
            digits[i] = value[value.Length - 1 - i] - '0';
        return digits;
    }

    // Вычисляет свёртку двух массивов через NTT + китайскую теорему об остатках
    private static long[] NttConvolveWithCrt(int[] left, int[] right)
    {
        // Вычисляем размер FFT: ближайшая степень двойки >= (длина left + длина right)
        int fftSize = 1;
        int need = left.Length + right.Length;
        while (fftSize < need)
            fftSize <<= 1;

        // Массив для хранения свёрток по каждому модулю
        // convolutions[m][i] — i-й коэффициент свёртки по модулю NttModuli[m]
        long[][] convolutions = new long[NttModuli.Length][];

        // Выполняем NTT для каждого из трёх модулей независимо
        for (int modIndex = 0; modIndex < NttModuli.Length; modIndex++)
        {
            long mod = NttModuli[modIndex];
            long[] a = new long[fftSize]; // Коэффициенты первого многочлена
            long[] b = new long[fftSize]; // Коэффициенты второго многочлена

            // Копируем цифры в массивы, беря остаток по модулю
            for (int i = 0; i < left.Length; i++)
                a[i] = left[i] % mod;
            for (int i = 0; i < right.Length; i++)
                b[i] = right[i] % mod;

            // Прямое NTT: переводим из коэффициентов в значения в точках
            Ntt(a, mod, PrimitiveRoots[modIndex], false);
            Ntt(b, mod, PrimitiveRoots[modIndex], false);

            // Поэлементное умножение в частотной области
            for (int i = 0; i < fftSize; i++)
                a[i] = a[i] * b[i] % mod;

            // Обратное NTT: возвращаемся к коэффициентам свёртки
            Ntt(a, mod, PrimitiveRoots[modIndex], true);

            // Сохраняем свёртку по текущему модулю
            convolutions[modIndex] = a;
        }

        // Восстанавливаем точные коэффициенты через китайскую теорему об остатках
        long[] result = new long[need];
        for (int i = 0; i < need; i++)
        {
            result[i] = Crt(
                convolutions[0][i], NttModuli[0],
                convolutions[1][i], NttModuli[1],
                convolutions[2][i], NttModuli[2]);
        }

        return result;
    }

    // Number Theoretic Transform — FFT в кольце вычетов по модулю mod
    // primitiveRoot — первообразный корень для этого модуля
    // invert = false -прямое NTT, invert = true - обратное NTT
    private static void Ntt(long[] values, long mod, long primitiveRoot, bool invert)
    {
        int n = values.Length;

        // Этап 1: бит-реверсивная перестановка
        // Переставляем элементы так, чтобы индекс i заменился на свой бит-реверс
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

        // Этап 2: основной цикл «бабочек»
        // blockSize — размер текущего сливаемого блока: 2, 4, 8, ..., n
        for (int blockSize = 2; blockSize <= n; blockSize <<= 1)
        {
            // Вычисляем первообразный корень степени blockSize
            // wlen = primitiveRoot^((mod-1)/blockSize) mod mod
            long wlen = ModPow(primitiveRoot, (mod - 1) / blockSize, mod);

            // Для обратного NTT используем обратный элемент к корню
            if (invert)
                wlen = ModPow(wlen, mod - 2, mod);

            // Проходим по всем блокам размера blockSize
            // Блок - группа корней одной степени (мы проходим по всем степеням)
            for (int blockStart = 0; blockStart < n; blockStart += blockSize)
            {
                long w = 1;                     // Текущий корень (начинаем с w^0 = 1) (корень из единицы это число w^n = 1)
                int halfBlockSize = blockSize >> 1; // Половина блока

                // Одна бабочка даёт значения для двух симметричных корней
                // Бабочка разбивает задачу поиска корней степени n на две задачи поиска корней степени n/2
                for (int j = 0; j < halfBlockSize; j++)
                {
                    long u = values[blockStart + j];                      // Левая половина (четные, благодаря бит реверсу!)
                    long v = values[blockStart + j + halfBlockSize] * w % mod; // Правая * корень (нечетные)

                    // w^k: складываем
                    values[blockStart + j] = (u + v) % mod; //получили значение многочлена (свертки) в корне w^k
                    // w^k⁺половина: просто вычитаем (потому что ω^(half) = -1)
                    values[blockStart + j + halfBlockSize] = (u - v + mod) % mod;

                    // Переходим к следующему корню: ω^(k+1) = ω^k * ω
                    w = w * wlen % mod;
                }
            }
        }

        // Этап 3: нормировка для обратного NTT
        // После двух преобразований значения умножаются на n — делим на n
        if (invert)
        {
            // Обратный элемент к n по модулю mod (по теореме Ферма: n^(mod-2) = n^-1)
            long invN = ModPow(n, mod - 2, mod);
            for (int i = 0; i < n; i++)
                values[i] = values[i] * invN % mod;
        }
    }

    // Быстрое возведение в степень по модулю (бинарное возведение)
    // Вычисляет (baseValue^exp) % mod за O(log exp)
    private static long ModPow(long baseValue, long exp, long mod)
    {
        long result = 1;
        long b = baseValue % mod;
        while (exp > 0)
        {
            if ((exp & 1) == 1)        // Если текущий бит единица
                result = result * b % mod;
            b = b * b % mod;           // Квадрат основания
            exp >>= 1;                 // Сдвиг вправо = деление на 2
        }
        return result;
    }

    // Китайская теорема об остатках (CRT) для трёх модулей
    // Восстанавливает число X по его остаткам r1, r2, r3
    // X = r1 (mod m1), X = r2 (mod m2), X = r3 (mod m3)
    // Результат: единственное X < m1*m2*m3, удовлетворяющее всем трём сравнениям
    private static long Crt(long r1, long m1, long r2, long m2, long r3, long m3)
    {
        // Общий модуль — произведение всех трёх
        BigInteger bigM = (BigInteger)m1 * m2 * m3;

        // M_i = общий модуль / m_i
        BigInteger M1 = bigM / m1;
        BigInteger M2 = bigM / m2;
        BigInteger M3 = bigM / m3;

        // Обратные элементы: inv_i = M_i^-1 mod m_i
        long inv1 = ModInverse((long)(M1 % m1), m1);
        long inv2 = ModInverse((long)(M2 % m2), m2);
        long inv3 = ModInverse((long)(M3 % m3), m3);

        // Формула CRT: X = сумма (r_i * M_i * inv_i) mod общий_модуль
        BigInteger result = (BigInteger)r1 * M1 * inv1 +
                            (BigInteger)r2 * M2 * inv2 +
                            (BigInteger)r3 * M3 * inv3;
        result %= bigM;
        return (long)result;
    }

    // Расширенный алгоритм Евклида для нахождения обратного элемента по модулю
    //находит икс и игрек такие что ax + by = НОД(а б), если НОЖ = 1 то х - обратный эл-т a^-1 mod b
    // Находит x такое, что a*x = 1 (mod mod)
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
    // Превращает массив коэффициентов свёртки (где могут быть числа > 9) в строку
    // Выполняет перенос десятков справа налево
    private static string NormalizeDecimalDigits(long[] digits)
    {
        long carry = 0;

        // Проходим от младшего разряда к старшему и выполняем переносы
        for (int i = 0; i < digits.Length; i++)
        {
            long current = digits[i] + carry;   // Текущее значение + перенос
            carry = current / 10;               // Сколько уходит в следующий разряд
            long remainder = current % 10;      // Что остаётся в текущем разряде

            // Корректируем отрицательный остаток (если был отрицательный перенос)
            if (remainder < 0)
            {
                remainder += 10;
                carry--;
            }

            digits[i] = remainder;
        }

        // Если после всех разрядов остался перенос, добавляем новые разряды
        while (carry > 0)
        {
            Array.Resize(ref digits, digits.Length + 1);
            digits[^1] = carry % 10;
            carry /= 10;
        }

        // Убираем ведущие нули (но оставляем хотя бы один разряд)
        int last = digits.Length - 1;
        while (last > 0 && digits[last] == 0)
            last--;

        // Собираем строку: старший разряд — первый символ
        char[] chars = new char[last + 1];
        for (int i = 0; i <= last; i++)
            chars[i] = (char)('0' + digits[last - i]);

        return new string(chars);
    }
}