using System;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConstructiveReals;

public record ConstructiveRealEvaluationSettings(CancellationToken Cancel, bool UseMultithreading, ConstructiveRealExpressionFactory Factory, int DivisionExplosion = -1024 * 64)
{
}

public record Approximation(BigInteger Value, int Precision)
{
    public int Msd
    {
        get
        {
            var sign = Value.Sign;
            if (sign == 0) return int.MinValue;
            else if (sign > 0)
            {
                return (int)(Precision + Value.GetBitLength() - 1);
            }
            else
            {
                if (Value.Equals(-1)) return Precision;
                return (int)(Precision + Value.GetBitLength() + (Value.IsEven ? 0 : -1));
            }
        }
    }

    public override string ToString()
    {
        return ConstructiveReal.ApproximationToDebugString(Value, Precision);
    }
}

// Represents a constructive real number. A constructive real number is a computable function.
// It is (iteratively) estimated using the Evaluate method.
// Also the position of the most significant digit is provided for estimation and evaluation.
//
// This implementation is based on descriptions [1] and [2].
// References:
// [1] The Journal of Logic and Algebraic Programming 64 (2005) 3–11. The constructive reals as a Java library
// [2] Proceedings of the 1986 ACM conference on LISP and functional programming (LFP '86). Exact Real Arithmetic: A Case Study in Higher Order Programming
// [HD-2] Hackers Delight, Second Edition
public abstract class ConstructiveReal
{
    protected internal const int MaxIntThatDoesNotOverflowWhenMultipliedWith8 = 268435455;
    protected internal const int MinIntThatDoesNotOverflowWhenMultipliedWith8 = -268435456;

    // Computes the ConstructiveReal. Returns the value divided by 2**precision rounded to integer.
    // The error in the result must be < 1.
    // We assume that precision can be multiplied by 8 without overflowing.
    public abstract Task<Approximation> Evaluate(int precision, ConstructiveRealEvaluationSettings es);

    // Computes the most significant digit position n of the CR x, that is
    // 2**(n-1) < abs(x) < 2**(n+1)
    // it returns int.MinValue if n < precision .
    protected internal virtual async Task<int> EvaluateMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        if (es.UseMultithreading) await Task.Yield();
        var res = await Evaluate(precision - 1, es).ConfigureAwait(false);
        int result;
        if (res.Value.IsZero)
        {
            result = int.MinValue; // position yet unknown when evaluating up to precision
        }
        else
            result = res.Msd;
        return result;
    }

    // computes the most significant digit position n of the CR x, that is
    // 2**(n-1) < abs(x) < 2**(n+1)
    // it may return int.MinValue if n < precision
    // it iteratively raises calculation-precision up to precision
    protected internal virtual async Task<int> FindMostSignificantDigitPosition(int precision, ConstructiveRealEvaluationSettings es)
    {
        if (es.UseMultithreading) await Task.Yield();
        for (int testPrecision = short.MaxValue; testPrecision > precision & testPrecision > 64; testPrecision = testPrecision / 2)
        {
            es.Cancel.ThrowIfCancellationRequested();
            int msdp = await EvaluateMostSignificantDigitPosition(testPrecision, es).ConfigureAwait(false);
            if (msdp != int.MinValue) return msdp;
        }
        for (int testPrecision = 0; testPrecision > precision & testPrecision <= 0; testPrecision = testPrecision * 13 / 10 - 16)
        {
            es.Cancel.ThrowIfCancellationRequested();
            int msdp = await EvaluateMostSignificantDigitPosition(testPrecision, es).ConfigureAwait(false);
            if (msdp != int.MinValue) return msdp;
        }
        return await EvaluateMostSignificantDigitPosition(precision, es).ConfigureAwait(false);
    }

    protected internal Task<int> EvaluateMostSignificantDigitPosition(ConstructiveRealEvaluationSettings es)
    {
        return FindMostSignificantDigitPosition(int.MinValue, es);
    }

    // Evaluates the CR x to a double value
    public async Task<double> DoubleValue(ConstructiveRealEvaluationSettings es)
    {
        const int MAX_DOUBLE_PRECISION = 1023 + 52; // max exp range for denormal numbers
        int msdp = await FindMostSignificantDigitPosition(-MAX_DOUBLE_PRECISION - 4, es).ConfigureAwait(false);
        if (-MAX_DOUBLE_PRECISION - 4 > msdp) return 0.0;

        int needed_prec = msdp - 52;
        var scaledValue = await Evaluate(needed_prec, es).ConfigureAwait(false);
        int sign = scaledValue.Value.Sign;

        Double result;
        if (msdp > 1024)
        {
            result = sign < 0 ? Double.NegativeInfinity : Double.PositiveInfinity;
        }
        else
        {
            ulong longmBits = (sign < 0 ? (ulong)-scaledValue.Value : (ulong)scaledValue.Value);
            long exp = needed_prec;
            result = Converters.GetDoubleFromParts(sign, needed_prec, longmBits);
        }
        return result;
    }

    public async Task<String> ToString(int digits, ConstructiveRealEvaluationSettings es, bool hex = false)
    {
        ConstructiveReal shiftedReal = hex
            ? this.ShiftLeft(4 * digits)
            : this.Multiply(new IntegerConstructiveReal(BigInteger.Pow(10, digits)));
        Approximation shiftedApproximation = await shiftedReal.Evaluate(0, es);
        return BuildFractionString(digits, hex, shiftedApproximation.Value);
    }

    [System.Diagnostics.Conditional("DEBUG")]
    protected void DebugApproximation(BigInteger approximation, int precision, string? name = null)
    {
        name = name ?? $"({this})";
        System.Diagnostics.Debug.WriteLine($"IR: {this.GetType().Name:-20} {name:-20} = {ApproximationToDebugString(approximation, precision)} @ {precision}");
    }

    internal static string ApproximationToDebugString(BigInteger approximation, int precision)
    {
        return new ShiftedConstructiveReal(new IntegerConstructiveReal(approximation), precision).CalcString();
    }

    private string CalcString()
    {
        using (var cts = new CancellationTokenSource())
        {
            var es = new ConstructiveRealEvaluationSettings(cts.Token, false, new ConstructiveRealExpressionFactory());
            cts.CancelAfter(250);
            try
            {
                return ToString(15, es).Result.ToString();
            }
            catch (AggregateException)
            {
                return "--";
            }
            catch (OperationCanceledException)
            {
                return "--";
            }
        }
    }

    private string BuildFractionString(int digits, bool hex, BigInteger shiftedApproximation)
    {
        String approximationString = BigInteger.Abs(shiftedApproximation).ToString(hex ? "X" : "D", System.Globalization.CultureInfo.InvariantCulture);
        var approximationSpan = approximationString.AsSpan();

        while (shiftedApproximation.Sign != 0 && approximationSpan[0] == '0') approximationSpan = approximationSpan.Slice(1);

        int len = approximationSpan.Length;

        StringBuilder buider = StringBuilderUtils.AquireBuider();

        if (shiftedApproximation.Sign < 0)
        {
            buider.Append('-');
        }

        if (len <= digits)
        {
            buider.Append("0.");
            buider.Append('0', digits - len);
            buider.Append(approximationSpan);
        }
        else
        {
            buider.Append(approximationSpan.Slice(0, len - digits));
            if (digits > 0)
            {
                buider.Append('.');
                buider.Append(approximationSpan.Slice(len - digits));
            }
        }

        return StringBuilderUtils.GetAndRelease(buider);
    }

    // Shifts k by n.
    protected internal static BigInteger ShiftNoRounding(BigInteger k, int n)
    {
        BigInteger result;
        if (n == 0) result = k;
        else if (n < 0) result = k >> -n;
        else result = k << n;

        return result;
    }

    // Shifts k by n, rounding result
    protected internal static BigInteger ShiftRounded(BigInteger k, int n)
    {
        BigInteger result;

        if (n == -1)
        {
            result = (k + 1) >> 1;
        }
        else if (n < -1)
        {
            BigInteger biasedDoubleRes = (k >> -(n + 1)) + 1;
            result = biasedDoubleRes >> 1;
        }
        else // if (n >= 0)
        {
            result = k << n;
        }
        return result;
    }

    protected internal static void VerifyPrecision(int n)
    {
        if (n < MinIntThatDoesNotOverflowWhenMultipliedWith8 | n > MaxIntThatDoesNotOverflowWhenMultipliedWith8)
        {
            throw new PrecisionOverflowException($"{n}");
        }
    }

    public static implicit operator ConstructiveReal(long value)
    {
        if (value == 0) return ZeroConstructiveReal.Instance;
        return new IntegerConstructiveReal(value);
    }

    public static implicit operator ConstructiveReal(BigInteger value)
    {
        if (value == BigInteger.Zero) return ZeroConstructiveReal.Instance;
        return new IntegerConstructiveReal(value);
    }
}
