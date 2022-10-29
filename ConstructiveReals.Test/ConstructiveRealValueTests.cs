using System.Threading;

namespace ConstructiveReals.Test;

public class ConstructiveRealValueTest
{
    [Theory]
    [InlineData("1", "1")]
    [InlineData("1", "1.0")]
    [InlineData("10", "10")]
    [InlineData("10", "10.0")]
    [InlineData("-1", "-1")]
    [InlineData("-1", "-1.0")]
    [InlineData("-10", "-10")]
    [InlineData("-10", "-10.0")]
    [InlineData("1-1", "0")]
    [InlineData("1-1", "0.000000")]
    [InlineData("1*1", "1.000000")]
    [InlineData("2*1", "2.000000")]
    [InlineData("1*2", "2.000000")]
    [InlineData("2*2", "4.000000")]
    [InlineData("10*3", "30.000000")]
    [InlineData("3+4*5", "23.000000")]
    [InlineData("(3+4)*5", "35.000000")]
    [InlineData("Abs(-10)", "10.0000")]
    [InlineData("2 / 2", "1.000000")]
    [InlineData("2 / 3", "1")]
    [InlineData("2 / 3", "0.7")]
    [InlineData("2 / 3", "0.67")]
    [InlineData("2 / 3", "0.667")]
    [InlineData("2 / 3", "0.666666666666666666666666666666666666666666666666666666666666666666666667")]
    [InlineData("1 / 3", "0")]
    [InlineData("1 / 3", "0.3")]
    [InlineData("1 / 3", "0.33")]
    [InlineData("1 / 3", "0.333")]
    [InlineData("1 / 3", "0.3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333")]
    [InlineData("1 / 9", "0")]
    [InlineData("1 / 9", "0.1")]
    [InlineData("1 / 9", "0.11")]
    [InlineData("1 / 9", "0.111")]
    [InlineData("1 / 9", "0.1111")]
    [InlineData("1 / 9", "0.1111111")]
    [InlineData("1 / 9", "0.11111111111")]
    [InlineData("1 / 9", "0.111111111111111")]
    [InlineData("1 / 9", "0.1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111")]
    [InlineData("1 / 1000000", "0")]
    [InlineData("1 / 1000000", "0.0")]
    [InlineData("1 / 1000000", "0.00")]
    [InlineData("1 / 1000000", "0.000")]
    [InlineData("1 / 1000000", "0.0000")]
    [InlineData("1 / 1000000", "0.00000")]
    [InlineData("1 / 1000000", "0.000001")]
    [InlineData("1 / 1000000", "0.0000010")]
    [InlineData("1 / 1000000", "0.00000100")]
    [InlineData("1 / 1000000", "0.000001000")]
    [InlineData("1 / 1000000 ", "0.0000010000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("1 / (-2)", "-0.50000000000000000000000000000000000000000000000000000000000")]
    [InlineData("-(1 / 2)", "-0.50000000000000000000000000000000000000000000000000000000000")]
    [InlineData("-((-1) / (-2))", "-0.50000000000000000000000000000000000000000000000000000000000")]
    public void VerifyValueBasicOperation(string input, string result)
    {
        VerifyValue(input, result);
    }

    [Theory]
    [InlineData("sqrt(0)", "0.0")]
    [InlineData("sqrt(100)", "10.0000")]
    [InlineData("sqrt(2)", "1.41421")]
    [InlineData("sqrt(2)", "1.4142135624")]
    [InlineData("√(2)", "1.414213562373095048801688724209698078569671875376948073176679737990732478462107038850387534327641572735013846230912297024924836055850737212644121497099935831")]
    [InlineData("pow(2, 1 / 2)", "1.414213562373095048801688724209698078569671875376948073176679737990732478462107038850387534327641572735013846230912297024924836055850737212644121497099935831")]
    [InlineData("pow(2, 2)", "4.0000")]
    [InlineData("e", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    [InlineData("pi", "3")]
    [InlineData("pi", "3.1")]
    [InlineData("pi", "3.1415926535897932384626433832795028841971693993751058209749445923")]
    [InlineData("pi", "3.1415926535897932384626433832795028841971693993751058209749445923078164062862089986280348253421170679821480865132823066470938446095505822317253594081284811174502841027019385211055596446229489549303820")]
    [InlineData("exp(1)", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    [InlineData("1 / exp(-1)", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    [InlineData("sqrt(1 / exp(-2))", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    [InlineData("1 / sqrt(exp(-2))", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    [InlineData("exp(-2)", "0.1353352832366126918939994949724844034076315459095758814681588726540733741014876899370981224906570487550772871896335522124493468718928530381588951349967060055912502275586825823048384205758453846800359940834460248128713537501566435339959360850139004952942171")]
    [InlineData("exp(0)", "1.0000000000")]
    [InlineData("exp(1 / 1000000)", "1.0000010000")]
    [InlineData("exp(1 / 1000000)", "1.0000010000005000001666667083333416666680555557539682787698440255734678130761984949528353912034582297")]
    [InlineData("exp(100)", "26881171418161354484126255515800135873611118.7737419224")]
    [InlineData("ln(exp(2))", "2.00000000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("ln(exp(-2))", "-2.000000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("exp(ln(2))", "2.000000")]
    [InlineData("ln(exp(1000))", "1000")]
    [InlineData("exp(ln(1000))", "1000")]
    [InlineData("ln(exp(1000))", "1000.0")]
    [InlineData("exp(ln(1000))", "1000.0")]
    [InlineData("ln(exp(1000))", "1000.00")]
    [InlineData("exp(ln(1000))", "1000.00")]
    [InlineData("ln(exp(1000))", "1000.000")]
    [InlineData("exp(ln(1000))", "1000.000")]
    [InlineData("ln(exp(1000))", "1000.0000")]
    [InlineData("exp(ln(1000))", "1000.0000")]
    [InlineData("ln(exp(1000))", "1000.000000000000000000000000000000000000")]
    [InlineData("exp(ln(1000))", "1000.000000000000000000000000000000000000")]
    [InlineData("ln(1/1000000)", "-13.8155105580")]
    [InlineData("1 / exp(ln(1 / 1000000))", "1000000.00000000")]
    public void VerifyValueExtendedOperations(string input, string result)
    {
        VerifyValue(input, result);
    }

    private void VerifyValue(string input, string result)
    {
        int periodIndex = result.IndexOf('.');
        int precision = periodIndex < 0 ? 0 : (result.Length - periodIndex - 1);
        var factory = new ConstructiveRealExpressionFactory();
        var parse = new Parser<ConstructiveReal>(factory);
        ConstructiveReal test = parse.ParseExpression(input);
        using (var cts = new CancellationTokenSource())
        {
            if (!System.Diagnostics.Debugger.IsAttached) cts.CancelAfter(60000);
            ConstructiveRealEvaluationSettings es = new ConstructiveRealEvaluationSettings(cts.Token, false, factory);
            var evalResult = test.ToString(precision, es).Result;
            Assert.Equal(result, evalResult);
        }
    }
}