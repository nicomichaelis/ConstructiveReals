using System.Threading.Tasks;

namespace ConstructiveReals;

public class PowConstructiveReal : ConstructiveReal
{
    private ConstructiveReal _base;
    private ConstructiveReal _exponent;
    private ConstructiveReal _exp;

    public PowConstructiveReal(ConstructiveReal x, ConstructiveReal y)
    {
        _base = x;
        _exponent = y;
        _exp = _exponent.Multiply(_base.Ln()).Exp();
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        return _exp.Evaluate(precision, es);
    }

    public override string ToString()
    {
        return $"({_base})^({_exponent})";
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return _exp.FindMostSignificantDigitPosition(precision, es);
    }
}
