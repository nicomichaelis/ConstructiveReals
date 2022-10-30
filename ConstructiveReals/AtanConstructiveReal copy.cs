using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class AtanConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;
    private ConstructiveReal? _reduced;
    private object _lock = new object();

    private class AtanReducedConstructiveReal : ValueCachingConstructiveReal
    {
        private ConstructiveReal _op;

        public AtanReducedConstructiveReal(ConstructiveReal x)
        {
            _op = x;
        }

        protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
        {
            if (precision > 4) return new Approximation(BigInteger.Zero, precision);

            int valuePrecision = Math.Min(-16, precision - 16);
            BigInteger xk = (await _op.Evaluate(valuePrecision, es)).Value;
            BigInteger xsq = xk * xk >> -valuePrecision;
            BigInteger ek = 0;
            BigInteger one = ShiftNoRounding(1, -valuePrecision);
            BigInteger uk;

            // taylor series
            long k = 1;
            BigInteger z = 1;
            while (z.Sign != 0)
            {
                if (k % 2 == 1)
                {
                    uk = one / k;
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
            return $"__atan__({_op})";
        }
    }

    public AtanConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    private async Task<ConstructiveReal> ReduceOp(ConstructiveReal op, ConstructiveRealEvaluationSettings es)
    {
        const int testPrecision = -5;

        int msd = await op.FindMostSignificantDigitPosition(testPrecision, es).ConfigureAwait(false);
        if (msd >= -1)
        {
            return new ShiftedConstructiveReal(await ReduceOp(op.Multiply(new SqrtConstructiveReal(new IntegerConstructiveReal(1).Add(new MultiplicationConstructiveReal(op, op))).Add(new IntegerConstructiveReal(1)).Inverse()), es), 1);
        }
        else
        {
            return new AtanReducedConstructiveReal(op);
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
        return $"Atan({_op})";
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.FindMostSignificantDigitPosition(precision, es);
    }
}
