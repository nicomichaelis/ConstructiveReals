using System.Threading.Tasks;

namespace ConstructiveReals;

public class CosConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;
    private ConstructiveReal? _reduced;
    private object _lock = new();

    public CosConstructiveReal(ConstructiveReal x)
    {
        _op = x;
    }

    private void Reduce(ConstructiveRealEvaluationSettings es)
    {
        lock (_lock)
        {
            if (_reduced == null)
            {
                _reduced = new SinConstructiveReal(es.Factory.Pi().ShiftRight(1).Add(_op.Negate()));
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
        return $"Cos({_op})";
    }
}
