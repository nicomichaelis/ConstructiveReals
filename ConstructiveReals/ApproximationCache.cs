namespace ConstructiveReals;

public class ApproximationCache<T>
{
    private object _lock = new object();
    int _availablePrecision;
    T? _currentApproximation;
    bool _approximationValid = false;

    public void StoreApproximation(int precision, T newApproximation)
    {
        lock (_lock)
        {
            if (!_approximationValid)
            {
                _availablePrecision = precision;
                _currentApproximation = newApproximation;
                _approximationValid = true;
            }
            else if (precision < _availablePrecision)
            {
                _availablePrecision = precision;
                _currentApproximation = newApproximation;
            }
        }
    }

    public bool TryGetCurrentCache(out T? approximation, out int precision)
    {
        lock (_lock)
        {
            if (_approximationValid)
            {
                approximation = _currentApproximation;
                precision = _availablePrecision;
                return true;
            }
            else
            {
                approximation = default(T);
                precision = int.MaxValue;
                return false;
            }
        }
    }
}

public sealed class ApproximationCache : ApproximationCache<Approximation>
{
    public override string ToString()
    {
        if (TryGetCurrentCache(out var currentApprox, out var availablePrecision))
        {
            return $"{ConstructiveReal.ApproximationToDebugString(currentApprox!.Value, availablePrecision)} @{availablePrecision}";
        }
        else
        {
            return "invalid";
        }
    }
}
