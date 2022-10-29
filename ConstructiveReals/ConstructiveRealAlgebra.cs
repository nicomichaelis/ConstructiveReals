namespace ConstructiveReals;

public static class ConstructiveRealAlgebra
{
    public static ConstructiveReal ShiftLeft(this ConstructiveReal x, int n)
    {
        return Shift(x, n);
    }

    public static ConstructiveReal ShiftRight(this ConstructiveReal x, int n)
    {
        return Shift(x, -n);
    }

    public static ConstructiveReal Shift(this ConstructiveReal x, int n)
    {
        if (x is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        ConstructiveReal.VerifyPrecision(n);
        if (x is ShiftedConstructiveReal shifted)
        {
            var c = shifted.ShiftCount + n;
            if (c == 0) return shifted.Op;
            return new ShiftedConstructiveReal(shifted.Op, c);
        }
        if (n == 0) return x;
        return new ShiftedConstructiveReal(x, n);
    }

    public static ConstructiveReal Multiply(this ConstructiveReal x, ConstructiveReal y)
    {
        if (x is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        if (y is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        return new MultiplicationConstructiveReal(x, y);
    }

    public static ConstructiveReal Add(this ConstructiveReal x, ConstructiveReal y)
    {
        if (x is ZeroConstructiveReal && y is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        if (x is NegateConstructiveReal n1 && object.ReferenceEquals(n1.Op, y)) return ZeroConstructiveReal.Instance;
        if (y is NegateConstructiveReal n2 && object.ReferenceEquals(n2.Op, x)) return ZeroConstructiveReal.Instance;
        return new AdditionConstructiveReal(x, y);
    }

    public static ConstructiveReal Negate(this ConstructiveReal x)
    {
        if (x is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        if (x is IntegerConstructiveReal integer)
        {
            return new IntegerConstructiveReal(-integer.Value);
        }
        return new NegateConstructiveReal(x);
    }

    public static ConstructiveReal Abs(this ConstructiveReal x)
    {
        if (x is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        return new AbsConstructiveReal(x);
    }

    public static ConstructiveReal Inverse(this ConstructiveReal x)
    {
        if (x is ZeroConstructiveReal) return DivisionByZeroConstructiveReal.Instance;
        if (x is InvConstructiveReal inv) return inv.Op;
        return new InvConstructiveReal(x);
    }

    internal static ConstructiveReal Sqrt(ConstructiveReal x)
    {
        if (x is ZeroConstructiveReal) return ZeroConstructiveReal.Instance;
        return new SqrtConstructiveReal(x);
    }

    public static ConstructiveReal Pow(this ConstructiveReal x, long exp)
    {
        if (x is ZeroConstructiveReal && exp >= 0) return ZeroConstructiveReal.Instance;
        if (x is PowIntConstructiveReal pow)
        {
            return new PowIntConstructiveReal(pow.Op, exp * pow.Pow);
        }
        return new PowIntConstructiveReal(x, exp);
    }
}
