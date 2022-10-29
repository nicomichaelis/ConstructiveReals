using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

internal class InvConstructiveReal : ValueCachingConstructiveReal
{
    private ConstructiveReal _op;

    internal ConstructiveReal Op => _op;

    public InvConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    const int DOUBLE_PRECISION = 30;  // precision we assume that can be safely taken from double artithmetic. double mantissa is 52 bits.
    const int DOUBLE_OPERAND_PRECISION = 50; // the operand precision we feed into double arithmetic.

    // Evaluates the inverse of a constructive real op.
    protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
    {
        VerifyPrecision(precision);
        es.Cancel.ThrowIfCancellationRequested();
        if (es.UseMultithreading) await Task.Yield();

        BigInteger result;

        BigInteger currentApproximation;
        int currentPrecision;

        int opmsd = await _op.FindMostSignificantDigitPosition(es.DivisionExplosion, es).ConfigureAwait(false);
        if (opmsd == int.MinValue || opmsd < es.DivisionExplosion) throw new DivideByZeroException();
        int currentApproximationDigits;
        Approximation cached;
        if (!Cache.TryGetCurrentCache(out cached!, out currentPrecision) || cached.Value.IsZero)
        {
            (currentApproximation, currentPrecision) = await GetFloatApproximation(es, opmsd);
        }
        else
        {
            currentApproximation = cached.Value;
        }
        currentApproximationDigits = -opmsd - currentPrecision;
        int requiredPreciseDigits = -opmsd - precision + 32;
        requiredPreciseDigits = Math.Max(requiredPreciseDigits, 31);

        bool errorAccepted;
        do
        {
            es.Cancel.ThrowIfCancellationRequested();

            // use newton method to iterate required precision.
            // newton method is iteration over k via
            // (II)    x_{k+1} = x_k - \frac{f(x_k)}{f'(x_k)}
            // for calculating square roots we have
            // (III)     f(z) = 1/z - op ,
            // (IV)      f'(z) = -z**-2 .
            // putting (III) and (IV) int (II) we get
            // (V)    x_{k+1} = x_k + x_k^2 *(1/x_k - op}
            //                = 2 * x_k - x_k^2 * op

            var opDigits = Math.Min(currentApproximationDigits * 2, requiredPreciseDigits);
            var opPrecision = opmsd - opDigits;
            var opApprox = await _op.Evaluate(opPrecision, es).ConfigureAwait(false);

            int opXapproxSqPrecision = (opPrecision + 2 * currentPrecision);
            BigInteger lastApproximation = currentApproximation;
            BigInteger opXappoxSq = opApprox.Value * currentApproximation * currentApproximation;

            BigInteger doubleCurrentApproxLifted = currentApproximation << -(opXapproxSqPrecision - currentPrecision - 1);
            currentApproximation = doubleCurrentApproxLifted - opXappoxSq;

            currentApproximationDigits = Math.Min(2 * currentApproximationDigits, requiredPreciseDigits);
            int nextPrecision = -opmsd - currentApproximationDigits;
            currentApproximation = ShiftRounded(currentApproximation, (opXapproxSqPrecision - nextPrecision));
            errorAccepted = (currentPrecision == nextPrecision) && (BigInteger.Abs(currentApproximation - lastApproximation) < 1 << 30);
            currentPrecision = nextPrecision;
        } while (!errorAccepted);

        Cache.StoreApproximation(currentPrecision, new Approximation(currentApproximation, currentPrecision));
        result = ShiftRounded(currentApproximation, currentPrecision - precision);

        return new Approximation(result, currentPrecision - precision);
    }

    protected internal override async Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        int opmsd = await _op.FindMostSignificantDigitPosition(es.DivisionExplosion, es).ConfigureAwait(false);
        if (opmsd == int.MinValue || opmsd < es.DivisionExplosion) throw new DivideByZeroException();

        return -opmsd;
    }

    private async Task<(BigInteger approximation, int actualPrecision)> GetFloatApproximation(ConstructiveRealEvaluationSettings es, int opmsd)
    {
        int opPrecison = (opmsd - DOUBLE_OPERAND_PRECISION);
        Approximation opApproximation = await _op.Evaluate(opPrecison, es).ConfigureAwait(false);

        // calculate the inverse such that the integral part builds the approximation of 1/ op
        double inv = ((1.0 * (1L << (DOUBLE_OPERAND_PRECISION - 1)) / (double)opApproximation.Value));
        double doubleInvApproximation = inv * ((1L << DOUBLE_PRECISION));

        BigInteger invApprox = new BigInteger(doubleInvApproximation);
        int currentPrecision = -opmsd + 1 - DOUBLE_PRECISION;
        return (invApprox, currentPrecision);
    }

    public override string ToString()
    {
        return $"1/({_op})";
    }
}

internal class DivisionByZeroConstructiveReal : ConstructiveReal
{
    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        throw new DivideByZeroException();
    }

    public override string ToString()
    {
        return $"⨳";
    }
}