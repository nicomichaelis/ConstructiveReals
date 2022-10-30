using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

internal class SqrtConstructiveReal : ValueCachingConstructiveReal
{
    private ConstructiveReal _op;

    public SqrtConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    const int DOUBLE_PRECISION = 40;  // precision we assume that can be safely taken from double artithmetic. double mantissa is 52 bits.
    const int DOUBLE_OPERAND_PRECISION = DOUBLE_PRECISION * 2; // the operand precision we feed into double arithmetic.

    ApproximationCache<(BigInteger approx, int valueNomPrec, int msd)> _sqrtApproxCache = new();

    // Evaluates the sqrt(op) of a constructive real op.
    // For small operand approximations we can use double arithmetic to get an initial guess of the sqrt.
    // Upon that we build up precision using newton iterations.
    // Notice that
    // (I)    sqrt(a*(2**p)) = sqrt(a)*(2**(p/2))
    // where we see p as a precision of a CR and a an approximation of the mantissa as returned from Evaluate of the constructive real op.
    protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
    {
        int opmsd;
        int resultMsd;
        int maxOpPrecision = GetMaxOpPrecision(precision);

        BigInteger currentApproximation;
        int currentNomPrec;
        if (!_sqrtApproxCache.TryGetCurrentCache(out var cacheData, out var currentPreciseDigits))
        {
            opmsd = await _op.FindMostSignificantDigitPosition(maxOpPrecision, es).ConfigureAwait(false);
            if (opmsd == int.MinValue) return new Approximation(BigInteger.Zero, precision);

            resultMsd = opmsd / 2; // notice from (I) that the precision/msd of the sqrt is half of the precision/msd of the operand, thus:
            if (opmsd < maxOpPrecision) return new Approximation(BigInteger.Zero, precision);

            (currentApproximation, currentNomPrec) = await GetFloatApproximation(es, opmsd, resultMsd);
            currentPreciseDigits = DOUBLE_PRECISION;
            //DebugApproximation(currentApproximation, currentNomPrec, "float approx");
            _sqrtApproxCache.StoreApproximation(currentPreciseDigits, (currentApproximation, currentNomPrec, opmsd));
        }
        else
        {
            opmsd = cacheData.msd;
            resultMsd = opmsd / 2; // notice from (I) that the precision/msd of the sqrt is half of the precision/msd of the operand, thus:
            if (opmsd < maxOpPrecision) return new Approximation(BigInteger.Zero, precision);
            currentApproximation = cacheData.approx;
            currentNomPrec = cacheData.valueNomPrec;
        }

        int requiredPreciseDigits = resultMsd - precision + 64;
        requiredPreciseDigits = Math.Max(requiredPreciseDigits, currentPreciseDigits);
        bool errorAccepted = false;

        while (!errorAccepted)
        {
            es.Cancel.ThrowIfCancellationRequested();

            // use newton method to iterate required precision.
            // newton method is iteration over k via
            // (II)    x_{k+1} = x_k - \frac{f(x_k)}{f'(x_k)}
            // for calculating square roots we have
            // (III)     f(z) = z^2 - op ,
            // (IV)      f'(z) = 2*z .
            // putting (III) and (IV) int (II) we get
            // (V)    x_{k+1} = x_k - \frac{(x_k)^2 - op}{2*x_k}
            //                = \frac{2*x_k^2 - x_k^2 + op}{2*x_k}
            //                = \frac{x_k^2 + op}{2*x_k} .

            // the next iteration gives us twice as many digits (doubled precision), but lets drop some
            int nextDigits = Math.Min(2 * currentPreciseDigits, requiredPreciseDigits);
            // thus the next precision is
            int nextNomPrecision = ((int)Math.Max(resultMsd - 32L - nextDigits, MinIntThatDoesNotOverflowWhenMultipliedWith8)) & -2;

            // for adding op to x_k^2 in equation (V) we want them to have
            // the nominal same precision. we double the nominal precision
            // of x_k^2 by squaring it thus we need doubled precision for op
            (var opApprox, _) = await _op.Evaluate(nextNomPrecision, es).ConfigureAwait(false);
            //DebugApproximation(opApprox, nextNomPrecision, "op");
            if (opApprox.Sign < 0) throw new ArithmeticException("SQRT operand is negative"); // this should have been noticed during initial estimation

            // equation (V) here we go
            var lastApprox = currentApproximation;
            currentApproximation = ShiftNoRounding(currentApproximation, currentNomPrec - nextNomPrecision / 2);
            BigInteger numerator = (currentApproximation * currentApproximation) + opApprox;
            BigInteger doubledApprox = ((numerator << (-nextNomPrecision / 2)) / currentApproximation);
            // apply rounding when halving
            currentApproximation = (doubledApprox + 1) >> 1;

            errorAccepted = (currentNomPrec == nextNomPrecision) && (BigInteger.Abs(currentApproximation - lastApprox) < 1 << 30);
            currentNomPrec = nextNomPrecision;
            currentPreciseDigits = nextDigits;
        }
        _sqrtApproxCache.StoreApproximation(currentPreciseDigits, (currentApproximation, currentNomPrec, opmsd));

        return new Approximation(ShiftRounded(currentApproximation, currentNomPrec - precision), precision);
    }

    private int GetMaxOpPrecision(int precision)
    {
        return 2 * precision - 8;
    }

    private async Task<(BigInteger approx, int precseBits)> GetFloatApproximation(ConstructiveRealEvaluationSettings es, int opmsd, int resultMsd)
    {
        // we can get get an initial value for the newton interation using floating point
        int currentPrecision = resultMsd - DOUBLE_PRECISION;

        // the precision for op; zero last bit such that opPrecision is divisibile by 2 (rounds towards negative infinity)
        int opPrecison = (opmsd - DOUBLE_OPERAND_PRECISION) & -2;

        (BigInteger opApproximation, _) = await _op.Evaluate(opPrecison, es);
        if (opApproximation.Sign < 0) throw new ArithmeticException("SQRT operand is negative"); //oops

        // calculate the sqrt such that the integral part builds the approximation of sqrt(op)
        double doubleSqrtApproximation = Math.Sqrt((double)(opApproximation << DOUBLE_OPERAND_PRECISION));
        // calculate the nominal precision for the integral part of doubleSqrtApproximation.
        // the precision is halved because of eq. (I)
        // it follows the 2**n rule but contains "unprecise" bits that we do not want to use.
        int sqrtApproxPrecision = (opPrecison - DOUBLE_OPERAND_PRECISION) / 2;
        BigInteger sqrtApproximation = new BigInteger(doubleSqrtApproximation);
        // remove "unprecise" bits, shift to final precision
        return (ShiftRounded(sqrtApproximation, sqrtApproxPrecision - currentPrecision), currentPrecision);
    }

    protected internal override async Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        int maxOpPrecision = GetMaxOpPrecision(precision);

        if (_sqrtApproxCache.TryGetCurrentCache(out var cacheData, out var preciseBits))
        {
            return cacheData.msd / 2;
        }
        else
        {
            var opmsd = await _op.FindMostSignificantDigitPosition(maxOpPrecision, es).ConfigureAwait(false);

            return opmsd == int.MinValue ? int.MinValue : opmsd / 2; // notice from (I) that the precision/msd of the sqrt is half of the precision/msd of the operand, thus:
        }
    }

    public override string ToString()
    {
        return $"√({_op})";
    }
}
