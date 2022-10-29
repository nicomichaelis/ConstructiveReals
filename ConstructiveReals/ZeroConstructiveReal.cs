using System.Numerics;
using System.Threading.Tasks;

namespace ConstructiveReals;

internal class ZeroConstructiveReal : ConstructiveReal
{
    public static ZeroConstructiveReal Instance { get; } = new ZeroConstructiveReal();

    protected internal override Task<int> EvaluateMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(int.MinValue);
    }

    protected internal override Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(int.MinValue);
    }

    public override Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es)
    {
        return Task.FromResult(new Approximation(BigInteger.Zero, precision));
    }

    public override string ToString()
    {
        return "0";
    }
}
