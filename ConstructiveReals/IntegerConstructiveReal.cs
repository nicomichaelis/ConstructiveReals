using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

internal class IntegerConstructiveReal : ConstructiveReal
{
    private BigInteger _value;

    internal BigInteger Value => _value;

    int _opmsd;

    public IntegerConstructiveReal(BigInteger x)
    {
        if (x.Sign == 0)
        {
            _opmsd = int.MinValue;
        }
        else
        {
            _opmsd = new Approximation(x, 0).Msd;
        }
        _value = x;
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        es.Cancel.ThrowIfCancellationRequested();
        return Task.FromResult(new Approximation(ShiftRounded(_value, -precision), precision));
    }

    public override string ToString()
    {
        return _value.ToString();
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(_opmsd);
    }
}
