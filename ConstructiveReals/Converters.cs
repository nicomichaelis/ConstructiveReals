using System;

namespace ConstructiveReals;

internal static class Converters
{
    public static long DoubleBitsToLong(double val)
    {
        Span<double> data = stackalloc double[1] { val };
        return System.Runtime.InteropServices.MemoryMarshal.Cast<double, long>(data)[0];
    }
    public static Double LongBitsToDouble(long val)
    {
        Span<long> data = stackalloc long[1] { val };
        return System.Runtime.InteropServices.MemoryMarshal.Cast<long, double>(data)[0];
    }

    internal static double GetDoubleFromParts(int sign, int exp, ulong mantissa)
    {
        Span<ulong> resBits = stackalloc ulong[1];
        Span<double> resDouble = System.Runtime.InteropServices.MemoryMarshal.Cast<ulong, double>(resBits);

        resBits[0] = sign < 0 ? 0x8000000000000000 : 0UL;

        if (mantissa != 0UL)
        {
            // adapt exp and mantissa such that the highest bit 53 of the mantissa is set - bit 53 is the implicit mantissa bit of double
            int shift = nlz(mantissa) - (64 - 53);
            exp = exp - shift;
            exp = exp + (1023 + 52); // add bias to exp and shift the additional 52 mantissa bits

            mantissa = Shift(mantissa, shift);

            if (exp >= 0x7FF)
            {
                resBits[0] |= 0x7FF0000000000000; // ±∞
            }
            else if (exp <= 0)
            {
                // denormal value.
                exp--;
                if (exp >= -52)
                {
                    resBits[0] |= mantissa >> -exp;
                }
                // else: underflow to 0
            }
            else
            {
                // the first mantissa bit is implicit, so mask it
                resBits[0] |= (mantissa & 0x000FFFFFFFFFFFFF) | ((ulong)exp << 52);
            }
        }

        return resDouble[0];
    }

    private static ulong Shift(ulong value, int shift)
    {
        if (shift < 0)
            value = value >> -shift;
        else
            value = value << shift;
        return value;
    }


    // number of leading zeros form Hackers Delight II
    public static int nlz(uint value)
    {
        if (value == 0) return 32;
        int n = 1;
        if ((value >> 16) == 0) { n = n + 16; value = value << 16; }
        if ((value >> 24) == 0) { n = n + 08; value = value << 8; }
        if ((value >> 28) == 0) { n = n + 04; value = value << 4; }
        if ((value >> 30) == 0) { n = n + 02; value = value << 2; }
        n = n - (int)(value >> 31);
        return n;
    }

    public static int nlz(ulong value)
    {
        if ((value & 0xFFFFFFFF00000000) == 0)
            return 32 + nlz((uint)value);
        return nlz((uint)(value >> 32));
    }
}
