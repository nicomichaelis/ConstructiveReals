using System.Threading.Tasks;

namespace ConstructiveReals;

public class PowIntConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op1;
    private long _pow;
    private ConstructiveReal? _reduced;

    internal ConstructiveReal Op => _op1;
    internal long Pow => _pow;
    object _lock = new object();

    public PowIntConstructiveReal(ConstructiveReal op1, long pow)
    {
        _op1 = op1;
        _pow = pow;
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        lock (_lock)
        {
            if (_reduced == null)
            {
                Reduce();
            }
            return _reduced!.Evaluate(precision, es);
        }
    }

    protected internal override Task<int> EvaluateMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        lock (_lock)
        {
            if (_reduced == null)
            {
                Reduce();
            }
            return _reduced!.EvaluateMostSignificantDigitPosition(precision, es);
        }
    }

    private void Reduce()
    {
        long n = _pow;
        ConstructiveReal x = _op1;
        ConstructiveReal y = new IntegerConstructiveReal(1);

        if (n == 0)
        {
            _reduced = y;
        }
        else
        {
            if (n < 0)
            {
                x = x.Inverse();
                n = -n;
            }
            while (n > 1)
            {
                if (n % 2 == 0)
                {
                    x = new MultiplicationConstructiveReal(x, x);
                    n = n / 2;
                }
                else
                {
                    y = new MultiplicationConstructiveReal(x, y);
                    x = new MultiplicationConstructiveReal(x, x);
                    n = (n - 1) >> 1;
                }
            }
            _reduced = new MultiplicationConstructiveReal(x, y);
        }
    }

    public override string ToString()
    {
        return $"({_op1})^{_pow}";
    }
}
