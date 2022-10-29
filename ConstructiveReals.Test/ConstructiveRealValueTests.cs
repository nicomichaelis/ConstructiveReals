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
    [InlineData("e", "2.7182818284590452353602874713526624977572470936999595749669676277240766303535475945713821785251664274274663919320030599218174136")]
    public void VerifyValueExtendedOperations(string input, string result)
    {
        VerifyValue(input, result);
    }


    private void VerifyValue(string input, string result)
    {
        int periodIndex = result.IndexOf('.');
        int precision = periodIndex < 0 ? 0 : (result.Length - periodIndex - 1);
        var parse = new Parser<ConstructiveReal>(new ConstructiveRealExpressionFactory());
        ConstructiveReal test = parse.ParseExpression(input);
        using (var cts = new CancellationTokenSource())
        {
            if (!System.Diagnostics.Debugger.IsAttached) cts.CancelAfter(60000);
            ConstructiveRealEvaluationSettings es = new ConstructiveRealEvaluationSettings(cts.Token, false);
            var evalResult = test.ToString(precision, es).Result;
            Assert.Equal(result, evalResult);
        }
    }
}