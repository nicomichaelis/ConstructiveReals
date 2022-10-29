using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class LnConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;
    private ConstructiveReal? _reduced;
    private object _lock = new object();

    private class LnReducedConstructiveReal : ValueCachingConstructiveReal
    {
        private ConstructiveReal _op;

        public LnReducedConstructiveReal(ConstructiveReal x)
        {
            _op = x;
        }

        const int DOUBLE_PRECISION = 40;  // precision we assume that can be safely taken from double artithmetic. double mantissa is 52 bits.

        protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
        {
            BigInteger currentApproximationValue;
            int currentPrecision;

            if (!Cache.TryGetCurrentCache(out var currentApproximation, out currentPrecision) || currentApproximation!.Value.IsZero)
            {
                (currentApproximationValue, currentPrecision) = await GetFloatApproximation(es).ConfigureAwait(false);
            }
            else
            {
                currentApproximationValue = currentApproximation.Value;
            }

            int targetPrecision = Math.Max(Math.Min(Math.Min((precision - 32) & -2, -32), currentPrecision), MinIntThatDoesNotOverflowWhenMultipliedWith8);
            bool errorAccepted;
            do
            {
                es.Cancel.ThrowIfCancellationRequested();

                // use newton method to iterate required precision.
                // newton method is iteration over k via
                // (II)    x_{k+1} = x_k - \frac{f(x_k)}{f'(x_k)}
                // for calculating ln we have
                // (III)     f(z) = exp(z) - op ,
                // (IV)      f'(z) = exp(z) .

                // to be sure we assume some digits are not precise, thus we drop them
                // the count of digits currently there
                int nextPrecision = Math.Max(Math.Max(2 * currentPrecision, targetPrecision), MinIntThatDoesNotOverflowWhenMultipliedWith8);

                var opApprox = (await _op.Evaluate(nextPrecision, es).ConfigureAwait(false)).Value << -nextPrecision;
                if (opApprox.Sign < 0) throw new ArithmeticException("LN operand is negative"); // this should have been noticed during initial estimation

                var crExp = new ExpConstructiveReal(new ShiftedConstructiveReal(new IntegerConstructiveReal(currentApproximationValue), currentPrecision));
                var expApprox = (await crExp.Evaluate(nextPrecision, es).ConfigureAwait(false)).Value;

                var lastApprox = currentApproximationValue;
                // x_k + 1 = x_k - 1 + op / exp(z)
                currentApproximationValue = (currentApproximationValue << (-nextPrecision + currentPrecision)) - (BigInteger.One << -nextPrecision) + opApprox / expApprox;

                errorAccepted = (currentPrecision == nextPrecision) && (BigInteger.Abs(currentApproximationValue - lastApprox) < 1 << 30);
                currentPrecision = nextPrecision;
            } while (!errorAccepted);

            var result = ShiftRounded(currentApproximationValue, currentPrecision - precision);
            return new Approximation(result, precision);
        }

        private async Task<(BigInteger, int)> GetFloatApproximation(ConstructiveRealEvaluationSettings es)
        {
            var opDouble = await _op.DoubleValue(es).ConfigureAwait(false);
            if (opDouble < 0) throw new ArithmeticException("LN operand is negative"); //oops

            double ln = Math.Log(opDouble);
            double doubleLnOpApproximation = ln * (1L << (DOUBLE_PRECISION + 1));
            BigInteger lnApproximation = new BigInteger(doubleLnOpApproximation);
            // remove "unprecise" bits, shift to final precision
            return (ShiftRounded(lnApproximation, -1), -DOUBLE_PRECISION);
        }

        public override string ToString()
        {
            return $"__ln__({_op})";
        }
    }


    public LnConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    private async Task<ConstructiveReal> ReduceOp(ConstructiveReal op, ConstructiveRealEvaluationSettings es)
    {
        const int testPrecision = -5;

        int msd = await op.FindMostSignificantDigitPosition(testPrecision, es).ConfigureAwait(false);
        if (msd > 13)
        {
            var opSqr = op.Sqrt();
            return (await ReduceOp(opSqr, es)).Shift(1);
        }

        BigInteger currentApprox = (await op.Evaluate(testPrecision, es).ConfigureAwait(false)).Value;
        if (currentApprox.Sign < 0) throw new ArithmeticException("Ln operand is negative");
        if (currentApprox < 4) // 4/32
        {
            var opInv = op.Inverse();
            return (await ReduceOp(opInv, es)).Negate();
        }
        if (currentApprox > (4096 << 5))
        {
            var opSqr = op.Sqrt();
            return (await ReduceOp(opSqr, es)).Shift(1);
        }
        else
        {
            return new LnReducedConstructiveReal(op);
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
        return $"Ln({_op})";
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.FindMostSignificantDigitPosition(precision, es);
    }
}