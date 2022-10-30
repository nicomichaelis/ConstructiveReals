using System.Threading.Tasks;

namespace ConstructiveReals;

public class TanConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;
    private ConstructiveReal? _reduced;
    private object _lock = new();

    public TanConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    private void Reduce(ConstructiveRealEvaluationSettings es)
    {
        lock (_lock)
        {
            if (_reduced == null)
            {
                var sin = new SinConstructiveReal(_op);
                var denom = new SqrtConstructiveReal(new IntegerConstructiveReal(1).Add(new MultiplicationConstructiveReal(sin, sin).Negate()));
                _reduced = sin.Multiply(denom.Inverse());
            }
        }
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.Evaluate(precision, es);
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        Reduce(es);
        return _reduced!.FindMostSignificantDigitPosition(precision, es);
    }

    public override string ToString()
    {
        return $"Tan({_op})";
    }
}
