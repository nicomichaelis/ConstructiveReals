using System.Threading.Tasks;

namespace ConstructiveReals;

public class ShiftedConstructiveReal : ConstructiveReal
{
    ConstructiveReal _op;
    int _bitShiftCount;

    internal int ShiftCount => _bitShiftCount;
    internal ConstructiveReal Op => _op;

    public ShiftedConstructiveReal(ConstructiveReal x, int n)
    {
        _op = x;
        _bitShiftCount = n;
    }

    public override async Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        return await _op.Evaluate(precision - _bitShiftCount, es) with { Precision = precision };
    }

    public override string ToString()
    {
        return $"({_op}) {(_bitShiftCount >= 0 ? "<<" : ">>")} {System.Math.Abs(_bitShiftCount)}";
    }

    protected internal override async Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        var res = await _op.FindMostSignificantDigitPosition(precision - _bitShiftCount, es).ConfigureAwait(false);
        if (res == int.MinValue) return int.MinValue;
        return res + _bitShiftCount;
    }
}
