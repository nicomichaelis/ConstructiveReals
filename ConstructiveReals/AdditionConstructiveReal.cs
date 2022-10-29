using System.Threading.Tasks;

namespace ConstructiveReals;

public class AdditionConstructiveReal : ValueCachingConstructiveReal
{
    private ConstructiveReal _op1;
    private ConstructiveReal _op2;

    public AdditionConstructiveReal(ConstructiveReal op1, ConstructiveReal op2)
    {
        _op1 = op1;
        _op2 = op2;
    }

    protected override async Task<Approximation> EvaluateInternal(int precision, ConstructiveRealEvaluationSettings es)
    {
        if (es.UseMultithreading) await Task.Yield();

        var t1 = _op1.Evaluate(precision - 2, es);
        var t2 = _op2.Evaluate(precision - 2, es);

        await Task.WhenAll(t1, t2).ConfigureAwait(false);
        var r1 = await t1; var r2 = await t2;
        return new Approximation(ShiftRounded(r1.Value + r2.Value, -2), precision);
    }

    public override string ToString()
    {
        return $"({_op1}) + ({_op2})";
    }
}
