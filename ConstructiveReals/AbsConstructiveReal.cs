using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class AbsConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _op;

    public AbsConstructiveReal(ConstructiveReal op)
    {
        _op = op;
    }

    public override async Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        var res = await _op.Evaluate(precision, es).ConfigureAwait(false);
        return res with { Value = BigInteger.Abs(res.Value) };
    }

    public override string ToString()
    {
        return $"abs({_op})";
    }
}
