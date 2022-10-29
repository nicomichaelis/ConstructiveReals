using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals
{
    public partial class ExpConstructiveReal : ConstructiveReal
    {
        private ConstructiveReal _op;
        private ConstructiveReal? _reduced;
        private object _lock = new object();

        public ExpConstructiveReal(ConstructiveReal x)
        {
            _op = x;
        }

        private async Task<ConstructiveReal> ReduceOp(ConstructiveReal op, ConstructiveRealEvaluationSettings es)
        {
            const int testPrecision = -10;
            BigInteger currentApprox = (await op.Evaluate(testPrecision, es)).Value;
            if (currentApprox.Sign < 0) return (await ReduceOp(op.Negate(), es)).Inverse();
            if (currentApprox > (2 << 10))
            {
                var opHalf = op.ShiftRight(1);
                var expSqrt = await ReduceOp(opHalf, es);
                var exp = expSqrt.Pow(2);
                return exp;
            }
            else if (currentApprox < (1 << 10))
            {
                var opPlusOne = op.Add(1);
                var expOpPlusOne = ReducedExp(opPlusOne);
                var expOneInv = es.Factory.EInv();
                return expOpPlusOne.Multiply(expOneInv);
            }
            else
            {
                return ReducedExp(op);
            }
        }

        private ConstructiveReal ReducedExp(ConstructiveReal op)
        {
            return new ExpReducedContinuedFractionConstructiveReal(op);
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
            return $"exp({_op})";
        }

        protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
        {
            Reduce(es);
            return _reduced!.FindMostSignificantDigitPosition(precision, es);
        }

        private class ExpReducedContinuedFractionConstructiveReal : ValueCachingConstructiveReal
        {
            private ConstructiveReal _op;

            public ExpReducedContinuedFractionConstructiveReal(ConstructiveReal x)
            {
                _op = x;
            }

            protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
            {
                VerifyPrecision(precision);

                int valuePrecision = Math.Min(-32, precision - 64);
                BigInteger ek = ShiftNoRounding(1, -valuePrecision);
                BigInteger x = (await _op.Evaluate(valuePrecision, es)).Value;
                BigInteger uk = ShiftNoRounding(1, -valuePrecision);

                // method of continued fractions
                long k = 1;
                while (uk.Sign != 0)
                {
                    uk = ShiftNoRounding((uk * x) / k, valuePrecision);
                    ek = ek + uk;
                    k = k + 1;
                    if ((k & 15L) == 0L) es.Cancel.ThrowIfCancellationRequested();
                }
                return new Approximation(ShiftRounded(ek, valuePrecision - precision), precision);
            }

            public override string ToString()
            {
                return $"__exp_cf__({_op})";
            }
        }
    }
}