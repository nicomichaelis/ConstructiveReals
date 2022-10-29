using System;
using System.Threading.Tasks;

namespace ConstructiveReals
{
    internal class MultiplicationConstructiveReal : ValueCachingConstructiveReal
    {
        private ConstructiveReal _op1;
        private ConstructiveReal _op2;
        private object _lock = new object();
        private int[] _msds = new[] { int.MinValue, int.MinValue };
        private int _OpWithKnownMsd = 0; // it doesn't matter as long as both msds are unknwown

        public MultiplicationConstructiveReal(ConstructiveReal x, ConstructiveReal y)
        {
            this._op1 = x;
            this._op2 = y;
        }

        // Evaluates the multiplications of the constructive reals op1 and op2.
        // First notice the property of the multiplication:
        // (I)     (a * 2**n) * (b * 2**m) = a*b * 2**(n + m)
        //
        // Moreover consider the MSDs of op1 and op2:
        // (II.1)   2**(n'-1) < abs(op1) < 2**(n'+1)
        // (II.2)   2**(m'-1) < abs(op2) < 2**(m'+1)
        // Then we know from (I)
        // (III)    abs(op1 * op2) = abs(op1) * abs(op2) < 2**(n' + m' + 2)
        // Remember that Evaluate() computes
        // (IV)     op1 * op2 * 2** -p rounded to integer.
        // Thus we learn from (III) that
        // (V)      abs(op1 * op2 * 2**-p) < 2 ** (n' + m ' + 2 - p)
        // The result of the multiplication will thus be 0 if n' + m' + 2 - p <= -1 because then the result will then be rounded to 0.
        // That means
        // (VI)     n' + m' - p < - 3 => return 0 .
        // Otherwise we need to approximate enough bits of each operand such that we can safely round.
        // The bits of operand op1 are mutiplied by at most 2**(m'+1).
        // Thence we need to evaluate the operands up to precision
        //  (VII.1)   p - m' - 3
        //  (VII.2)   p - n' - 3
        protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
        {
            VerifyPrecision(precision);

            ConstructiveReal[] ops = new[] { _op1, _op2 };
            var (opWithKnownMsd, msds) = await FindMsds(precision, ops, es);
            if (opWithKnownMsd < 0) return new Approximation(0, precision);
            int otherOp = opWithKnownMsd ^ 1;
            int opPrecision = precision - msds[otherOp] - 4;
            int otherOpMaxPrecision = precision - msds[opWithKnownMsd] - 4; // and add a few exta bits (1/2 + 1/4 + 1/8)

            var tApproxA = ops[opWithKnownMsd].Evaluate(opPrecision, es);
            var tApproxB = object.ReferenceEquals(ops[0], ops[1]) ? tApproxA : ops[otherOp].Evaluate(otherOpMaxPrecision, es);

            await Task.WhenAll(tApproxA, tApproxB).ConfigureAwait(false);

            int rescale = opPrecision + otherOpMaxPrecision - precision;

            return new Approximation(ShiftRounded(tApproxA.Result.Value * tApproxB.Result.Value, rescale), precision);
        }

        private async Task<int> DetermineOpWithMsd(ConstructiveReal[] ops, int[] msds, int precision, ConstructiveRealEvaluationSettings es, int previouslyKnownOpWithKnownMsd)
        {
            int result;
            if (msds[previouslyKnownOpWithKnownMsd] == int.MinValue)
            {
                // if msd of ops[previouslyKnownOpWithKnownMsd] is unknwown try finding it
                var t1 = ops[0].FindMostSignificantDigitPosition(precision, es);
                var t2 = ReferenceEquals(ops[0], ops[1]) ? t1 : ops[1].FindMostSignificantDigitPosition(precision, es);
                await Task.WhenAll(t1, t2).ConfigureAwait(false);
                msds[0] = t1.Result;
                msds[1] = t2.Result;

                if (msds[0] > int.MinValue) result = msds[0] > msds[1] ? 1 : 0;
                else if (msds[1] > int.MinValue) result = 1;
                else result = -1;
            }
            else
                result = previouslyKnownOpWithKnownMsd;
            return result;
        }

        protected internal override async Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
        {
            ConstructiveReal[] ops = new[] { _op1, _op2 };
            var msdData = await FindMsds(precision, ops, es);
            return msdData.opWithMsd < 0 ? int.MinValue : (msdData.msds[0] + msdData.msds[1]);
        }

        async Task<(int opWithMsd, int[] msds)> FindMsds(int precision, ConstructiveReal[] ops, ConstructiveRealEvaluationSettings es)
        {
            es.Cancel.ThrowIfCancellationRequested();
            if (es.UseMultithreading) await Task.Yield();

            int[] msds;
            int opWithKnownMsd;

            lock (_lock)
            {
                msds = new[] { _msds[0], _msds[1] };
                opWithKnownMsd = _OpWithKnownMsd; // until the first msd has been found, this might very well be incorrect
            }

            int halfPrecision = (precision >> 1) - 1;
            // try getting the msd of at least one of the ops up to half-precision.
            opWithKnownMsd = await DetermineOpWithMsd(ops, msds, halfPrecision, es, opWithKnownMsd);

            if (opWithKnownMsd < 0) return (-1, msds);
            int otherOp = opWithKnownMsd ^ 1;

            // from (VII) we can now get the precision needed for the other op:
            int otherOpMaxPrecision = precision - msds[opWithKnownMsd] - 4; // and add a few exta bits (1/2 + 1/4 + 1/8)
            msds[otherOp] = msds[otherOp] > int.MinValue ? msds[otherOp] : await ops[otherOp].FindMostSignificantDigitPosition(otherOpMaxPrecision, es).ConfigureAwait(false);

            lock (_lock)
            {
                // store what we learned
                _OpWithKnownMsd = opWithKnownMsd;
                _msds[0] = Math.Max(msds[0], _msds[0]);
                _msds[1] = Math.Max(msds[1], _msds[1]);
            }

            if ((long)msds[0] + (long)msds[1] - precision < - 4) return (-1, msds); // equation (VI); to long because msds[otherOp] could be int.MinValue
            return (opWithKnownMsd, msds);
        }

        public override string ToString()
        {
            return ReferenceEquals(_op1, _op2) ? $"({_op1})^2": $"({_op1})*({_op2})";
        }
    }
}