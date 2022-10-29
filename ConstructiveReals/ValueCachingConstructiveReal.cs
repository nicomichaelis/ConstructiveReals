using System.Threading.Tasks;

namespace ConstructiveReals
{
    public abstract class ValueCachingConstructiveReal : ConstructiveReal
    {
        protected abstract Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es);

        protected ApproximationCache Cache { get; } = new ApproximationCache();
        private object _msdLock = new object();
        private int _msd = int.MinValue;

        sealed override public async Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
        {
            es.Cancel.ThrowIfCancellationRequested();
            VerifyPrecision(precision);

            if (Cache.TryGetCurrentCache(out var current_approximation, out var available_precision) && precision >= available_precision)
            {
                return new Approximation(ShiftRounded(current_approximation!.Value, available_precision - precision), precision);
            }

            if (es.UseMultithreading) await Task.Yield();
            var result = await EvaluateInternal(precision, es).ConfigureAwait(false);
            Cache.StoreApproximation(result.Precision, result);

            return result;
        }


        protected internal sealed override async Task<int> EvaluateMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
        {
            lock (_msdLock)
            {
                if (_msd > int.MinValue) return _msd;

                if (Cache.TryGetCurrentCache(out var current_approximation, out var available_precision) && !current_approximation!.Value.IsZero)
                {
                    _msd = current_approximation.Msd;
                    return _msd;
                }
            }

            var res = await base.EvaluateMostSignificantDigitPosition(precision, es).ConfigureAwait(false);
            lock (_msdLock)
            {
                if (_msd == int.MinValue) _msd = res;
            }
            return res;
        }
    }
}
