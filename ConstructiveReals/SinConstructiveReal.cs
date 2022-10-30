using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class SinConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;
    private ConstructiveReal? _reduced;
    private object _lock = new object();

    private class SinReducedConstructiveReal : ValueCachingConstructiveReal
    {
        private ConstructiveReal _op;

        public SinReducedConstructiveReal(ConstructiveReal x)
        {
            _op = x;
        }

        protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
        {
            if (precision > 4) return new Approximation(BigInteger.Zero, precision);

            int valuePrecision = Math.Min(-64, 2 * precision);
            BigInteger xk = (await _op.Evaluate(valuePrecision, es)).Value;
            BigInteger xsq = xk * xk >> -valuePrecision;
            BigInteger ek = 0;
            BigInteger uk = ShiftNoRounding(1, -valuePrecision);

            // taylor series
            long k = 1;
            BigInteger z = 1;
            while (z.Sign != 0)
            {
                uk = uk / k;
                if (k % 2 == 1)
                {
                    z = xk * uk >> -valuePrecision;
                    ek = ek + z;
                    xk = (-xk * xsq) >> -valuePrecision;
                }
                k = k + 1;
                if ((k & 15L) == 0L) es.Cancel.ThrowIfCancellationRequested();
            }
            return new Approximation(ShiftRounded(ek, valuePrecision - precision), precision);
        }

        public override string ToString()
        {
            return $"__sin__({_op})";
        }
    }

    public SinConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    private async Task<ConstructiveReal> ReduceOp(ConstructiveReal op, ConstructiveRealEvaluationSettings es)
    {
        const int testPrecision = -3;

        BigInteger approx = (await op.Evaluate(testPrecision, es).ConfigureAwait(false)).Value;
        if (approx >= (3 << -testPrecision) || approx <= (-3 << -testPrecision))
        {
            // subtract multiples of PI
            BigInteger multiplier = approx / ((3 << -testPrecision));
            var piMultiples = es.Factory.Pi().Multiply(multiplier);
            if (multiplier.IsEven)
            {
                var op2 = op.Add(piMultiples.Negate());
                return await ReduceOp(op2, es).ConfigureAwait(false);
            }
            else
            {
                var op2 = op.Add(piMultiples.Negate());
                return (await ReduceOp(op2, es).ConfigureAwait(false)).Negate();
            }
        }
        else if (approx > (1 << -testPrecision - 1) || approx < (-1 << -testPrecision - 1))
        {
            var thirdOp = op.Multiply(((ConstructiveReal)3).Inverse());
            var reduc = await ReduceOp(thirdOp, es);
            return reduc.Multiply(3).Add(reduc.Multiply(reduc).Multiply(reduc).Multiply(-4));
        }
        else
        {
            return new SinReducedConstructiveReal(op);
        }
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.Evaluate(precision, es);
    }

    private void Reduce(ConstructiveRealEvaluationSettings es)
    {
        lock (_lock)
        {
            if (_reduced != null) return;
            _reduced = ReduceOp(_op, es).Result;
        }
    }

    public override string ToString()
    {
        return $"Sin({_op})";
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.FindMostSignificantDigitPosition(precision, es);
    }
}
