using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConstructiveReals;

public class Program
{
    const string COMMAND_SET_PRECISION = "set precision";
    const string COMMAND_SET_TIMEOUT = "set timeout";
    const string COMMAND_SET_DIVISION = "set division limit";
    public static async Task<int> Main(string[] args)
    {
        int precision = 64;
        int timeout = 5000;
        int divisionLimit = -1024 * 64;
        string? input;
        var factory = new ConstructiveRealExpressionFactory();
        var parse = new Parser<ConstructiveReal>(factory);
        while ((input = Console.ReadLine()) != null)
        {
            if (input.StartsWith(COMMAND_SET_PRECISION, true, System.Globalization.CultureInfo.InvariantCulture))
            {
                precision = Math.Max(0, int.Parse(input.Substring(COMMAND_SET_PRECISION.Length).Trim(), System.Globalization.CultureInfo.InvariantCulture));
                Console.WriteLine($"    precison = {precision}");
            }
            else if (input.StartsWith(COMMAND_SET_TIMEOUT, true, System.Globalization.CultureInfo.InvariantCulture))
            {
                timeout = Math.Max(-1, int.Parse(input.Substring(COMMAND_SET_TIMEOUT.Length).Trim(), System.Globalization.CultureInfo.InvariantCulture));
                Console.WriteLine($"    timeout = {timeout}");
            }
            else if (input.StartsWith(COMMAND_SET_DIVISION, true, System.Globalization.CultureInfo.InvariantCulture))
            {
                divisionLimit = Math.Min(-1024, int.Parse(input.Substring(COMMAND_SET_DIVISION.Length).Trim(), System.Globalization.CultureInfo.InvariantCulture));
                Console.WriteLine($"    division limit = {divisionLimit}");
            }
            else
            {
                using (var cts = new CancellationTokenSource())
                {
                    cts.CancelAfter(timeout);
                    ConstructiveRealEvaluationSettings es = new ConstructiveRealEvaluationSettings(cts.Token, false, factory, divisionLimit);
                    try
                    {
                        var cr = parse.ParseExpression(input);
                        string evalResult = await cr.ToString(precision, es);
                        Console.WriteLine(evalResult);
                    }
                    catch (Exception e)
                    {
                        HandleException(e);
                    }
                }
            }
        }
        return 0;
    }

    private static void HandleException(Exception e)
    {
        if (e is ArithmeticException)
        {
            Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}");
        }
        else if (e is OperationCanceledException)
        {
            Console.Error.WriteLine("Timeout..");
        }
        else if (e is SyntaxException)
        {
            Console.Error.WriteLine($"Syntax rror: {e.GetType().Name}: {e.Message}");
        }
        else if (e is AggregateException agg)
        {
            foreach (var inner in agg.Flatten().InnerExceptions) HandleException(inner);
        }
        else
            Console.Error.WriteLine(e);
    }
}