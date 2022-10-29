using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class NegateConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;

    internal ConstructiveReal Op => _op;

    public NegateConstructiveReal(ConstructiveReal op)
    {
        _op = op;
    }
    public override async Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        var res = await _op.Evaluate(precision, es).ConfigureAwait(false);

        return new Approximation(-res.Value, res.Precision);
    }

    public override string ToString()
    {
        return $"-({_op})";
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return _op.FindMostSignificantDigitPosition(precision, es);
    }
}
