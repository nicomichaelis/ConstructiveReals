using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class PiConstructiveReal : ValueCachingConstructiveReal
{
    protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
    {
        if (precision > 4) return new Approximation(BigInteger.Zero, precision);

        // Brent's approximation for PI - fast multiple-precision evaluation of elementary functions
        int targetPrecision = Math.Min(-64, 2 * precision);

        int valuePrecision = 2 * targetPrecision;
        BigInteger A = ShiftNoRounding(1, -valuePrecision);
        BigInteger X = ShiftNoRounding(1, -valuePrecision);
        BigInteger T = ShiftNoRounding(1, -valuePrecision - 2);
        BigInteger B = (await Sqrt(2, valuePrecision, es, true)).Value;

        BigInteger acceptedError = 1 << -valuePrecision + targetPrecision - 8;
        bool accepted;
        do
        {
            es.Cancel.ThrowIfCancellationRequested();

            BigInteger Y = A;
            A = (A + B) >> 1;
            B = (await Sqrt(B * Y, 2 * valuePrecision, es, false)).Value >> -2 * valuePrecision;
            BigInteger amy = A - Y;
            T = T - ((X * amy * amy) >> -2 * valuePrecision);
            X = X << 1;
            accepted = BigInteger.Abs(A - B) < acceptedError;
        } while (!accepted);

        var preResult = A * A / T;
        Cache.StoreApproximation(targetPrecision, new Approximation(ShiftRounded(preResult, valuePrecision - targetPrecision), targetPrecision));
        return new Approximation(ShiftRounded(preResult, valuePrecision - precision), precision);
    }

    private Task<Approximation> Sqrt(BigInteger v, int precision, ConstructiveRealEvaluationSettings es, bool neg)
    {
        ConstructiveReal sqr = new SqrtConstructiveReal(v);
        if (neg) sqr = sqr.Inverse();
        return sqr.Evaluate(precision, es);
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(1);
    }

    public override string ToString()
    {
        return $"𝜋";
    }
}
