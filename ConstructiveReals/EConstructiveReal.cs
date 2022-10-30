using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class EConstructiveReal : ValueCachingConstructiveReal
{
    protected override Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
    {
        if (precision > 4) return Task.FromResult(new Approximation(BigInteger.Zero, precision));

        int valuePrecision = Math.Min(-64, precision * 2);
        BigInteger ek = ShiftNoRounding(1, -valuePrecision);
        BigInteger uk = ShiftNoRounding(1, -valuePrecision);

        // method of continued fractions
        long k = 1;
        while (uk.Sign != 0)
        {
            uk = uk / k;
            ek = ek + uk;
            k = k + 1;
            if ((k & 15L) == 0L) es.Cancel.ThrowIfCancellationRequested();
        }
        return Task.FromResult(new Approximation(ShiftRounded(ek, valuePrecision - precision), precision));
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(1);
    }

    public override string ToString()
    {
        return $"e";
    }
}
