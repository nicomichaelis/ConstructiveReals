using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ConstructiveReals;

public enum TokenType
{
    Integer,
    Float,
    Identifier,
    Unknown,
    OpenParen,
    CloseParen,
    Plus,
    Minus,
    Div,
    Mul,
    Pow,
    Comma,
    EndOfInput,
}

public record Token(int Offset, string Value, TokenType Type);

public class Scanner
{
    private string Input { get; }
    private IEnumerator<Token> Enumerator { get; }

    private static readonly IReadOnlyDictionary<int, TokenType> _SingleCharTokens = new Dictionary<int, TokenType>()
    {
        ['('] = TokenType.OpenParen,
        [')'] = TokenType.CloseParen,
        ['+'] = TokenType.Plus,
        ['-'] = TokenType.Minus,
        ['/'] = TokenType.Div,
        ['*'] = TokenType.Mul,
        ['^'] = TokenType.Pow,
        [','] = TokenType.Comma,
        ['√'] = TokenType.Identifier,
    };

    public Scanner(string input)
    {
        Input = input;
        Enumerator = EnumerateTokens().GetEnumerator();
        Enumerator.MoveNext();
        Current = Enumerator.Current;
    }

    public Token Current { get; private set; }

    public void Next()
    {
        if (Current != null && Current.Type == TokenType.EndOfInput)
        {
            return;
        }
        Enumerator.MoveNext();
        Current = Enumerator.Current;
    }

    private IEnumerable<Token> EnumerateTokens()
    {
        int offset = -1;
        StringBuilder builder = new StringBuilder();
        using (var reader = new StringReader(Input))
        {
            int ch = ' ';
            SkipWhitespace(ref ch, ref offset, reader);
            while (ch >= 0)
            {
                int startoffset = offset;
                if (ch >= '0' & ch <= '9')
                {
                    yield return ReadInteger(ref ch, ref offset, reader, builder);
                }
                else if (ch >= 'a' & ch <= 'z' || ch >= 'A' & ch <= 'Z')
                {
                    yield return ReadIdentifier(ref ch, ref offset, reader, builder);
                }
                else if (_SingleCharTokens.TryGetValue(ch, out var singleType))
                {
                    yield return new Token(startoffset, ((char)ch).ToString(), singleType);
                    Read(ref ch, ref offset, reader);
                }
                else
                {
                    yield return new Token(startoffset, ((char)ch).ToString(), TokenType.Unknown);
                    Read(ref ch, ref offset, reader);
                }
                SkipWhitespace(ref ch, ref offset, reader);
                builder.Clear();
            }
        }
        yield return new Token(offset, "", TokenType.EndOfInput);
    }

    private Token ReadIdentifier(ref int ch, ref int offset, StringReader reader, StringBuilder builder)
    {
        int startoffset = offset;
        while (ch >= 'a' & ch <= 'z' || ch >= 'A' & ch <= 'Z' || ch == '_' || ch >= '0' & ch <= '9')
        {
            builder.Append((char)ch);
            Read(ref ch, ref offset, reader);
        }
        return new Token(startoffset, builder.ToString(), TokenType.Identifier);
    }

    private Token ReadInteger(ref int ch, ref int offset, StringReader reader, StringBuilder builder)
    {
        int startoffset = offset;
        TokenType type = TokenType.Integer;
        ReadInteger(ref ch, ref offset, reader, builder);
        if (ch == '.')
        {
            type = TokenType.Float;
            builder.Append((char)ch);
            Read(ref ch, ref offset, reader);
            ReadInteger(ref ch, ref offset, reader, builder);
        }
        if (ch == 'E' || ch == 'e')
        {
            type = TokenType.Float;
            builder.Append((char)ch);
            Read(ref ch, ref offset, reader);
            if (ch == '+' || ch == '-')
            {
                builder.Append((char)ch);
                Read(ref ch, ref offset, reader);
            }
            if (!(ch >= '0' & ch <= '9')) type = TokenType.Unknown;
            ReadInteger(ref ch, ref offset, reader, builder);
        }
        return new Token(startoffset, builder.ToString(), type);

        void ReadInteger(ref int ch, ref int offset, StringReader reader, StringBuilder builder)
        {
            while (ch >= '0' & ch <= '9')
            {
                builder.Append((char)ch);
                Read(ref ch, ref offset, reader);
            }
        }
    }

    private void Read(ref int ch, ref int offset, StringReader reader)
    {
        ch = reader.Read();
        if (ch >= 0) offset++;
    }

    private void SkipWhitespace(ref int ch, ref int offset, StringReader reader)
    {
        while (ch == ' ') Read(ref ch, ref offset, reader);
    }
}

public interface IExpressionFactory<T>
{
    T Negate(T op);
    T Add(T op1, T op2);
    T Sub(T op1, T op2);
    T Mul(T op1, T op2);
    T Div(T op1, T op2);
    T Pow(T op1, T op2);
    T Integer(string value);
    T Float(string value);
    T Pi();
    T E();
    T Function(string value, List<T> parms);
}

public class Parser<T>
{
    public Parser(IExpressionFactory<T> factory)
    {
        Factory = factory;
    }

    public IExpressionFactory<T> Factory { get; }

    public T ParseExpression(string input)
    {
        Scanner scan = new Scanner(input);
        var res = SimpleExpression(scan);
        Expect(scan, TokenType.EndOfInput);
        return res;
    }

    private T SimpleExpression(Scanner scan)
    {
        bool negate = (scan.Current.Type == TokenType.Minus);
        if (negate || scan.Current.Type == TokenType.Plus) scan.Next();
        T expr = Term(scan);
        if (negate) expr = Factory.Negate(expr);
        Token? op;
        while ((op = ExpectOptional(scan, TokenType.Plus, TokenType.Minus)) != null)
        {
            T term = Term(scan);
            expr = op.Type switch
            {
                TokenType.Plus => Factory.Add(expr, term),
                TokenType.Minus => Factory.Sub(expr, term),
                _ => throw new SyntaxException("Not supported")
            };
        }
        return expr;
    }

    private T Term(Scanner scan)
    {
        T term = Factor(scan);
        Token? op;
        while ((op = ExpectOptional(scan, TokenType.Mul, TokenType.Div)) != null)
        {
            T factor = Factor(scan);
            term = op.Type switch
            {
                TokenType.Mul => Factory.Mul(term, factor),
                TokenType.Div => Factory.Div(term, factor),
                _ => throw new SyntaxException("Not supported")
            };
        }
        return term;
    }

    private T Factor(Scanner scan)
    {
        Token t = scan.Current;
        Expect(scan, TokenType.Integer, TokenType.Float, TokenType.Identifier, TokenType.OpenParen);
        T factor;
        if (t.Type == TokenType.Integer)
        {
            factor = Factory.Integer(t.Value);
        }
        else if (t.Type == TokenType.Float)
        {
            factor = Factory.Float(t.Value);
        }
        else if (t.Type == TokenType.Identifier)
        {
            factor = ConstantOrFunction(scan, t);
        }
        else if (t.Type == TokenType.OpenParen)
        {
            factor = SimpleExpression(scan);
            Expect(scan, TokenType.CloseParen);
        }
        else throw new SyntaxException("Not supported");

        if (ExpectOptional(scan, TokenType.Pow) != null)
        {
            T pow = Factor(scan);
            factor = Factory.Pow(factor, pow);
        }
        return factor;
    }

    private T ConstantOrFunction(Scanner scan, Token t)
    {
        T factor;
        if (t.Value.Equals("pi", StringComparison.OrdinalIgnoreCase))
        {
            factor = Factory.Pi();
        }
        else if (t.Value.Equals("e", StringComparison.OrdinalIgnoreCase))
        {
            factor = Factory.E();
        }
        else
        {
            List<T> parms = new List<T>();
            Expect(scan, TokenType.OpenParen);
            if (scan.Current.Type != TokenType.CloseParen)
            {
                parms.Add(SimpleExpression(scan));
                while (ExpectOptional(scan, TokenType.Comma) != null)
                {
                    parms.Add(SimpleExpression(scan));
                }
            }
            Expect(scan, TokenType.CloseParen);
            factor = Factory.Function(t.Value, parms);
        }

        return factor;
    }

    private void Expect(Scanner scanner, TokenType type)
    {
        if (scanner.Current.Type != type) throw new SyntaxException($"{type} expected at {scanner.Current.Offset}:'{scanner.Current.Value}'");
        scanner.Next();
    }

    private Token Expect(Scanner scanner, params TokenType[] types)
    {
        var cur = scanner.Current;
        var curt = cur.Type;
        if (!types.Any(z => z == curt)) throw new SyntaxException($"{string.Join(", ", types)} expected at {scanner.Current.Offset}:'{scanner.Current.Value}'");
        scanner.Next();
        return cur;
    }

    private Token? ExpectOptional(Scanner scanner, params TokenType[] types)
    {
        var cur = scanner.Current;
        var curt = cur.Type;
        if (!types.Any(z => z == curt)) return null;
        scanner.Next();
        return cur;
    }
}

public class ConstructiveRealExpressionFactory : IExpressionFactory<ConstructiveReal>
{
    private EConstructiveReal Eeuler { get; }
    private ConstructiveReal EeulerInv { get; }
    private PiConstructiveReal ConstPi { get; }

    public ConstructiveRealExpressionFactory(EConstructiveReal? e = null, PiConstructiveReal? pi = null)
    {
        Eeuler = e ?? new EConstructiveReal();
        EeulerInv = Eeuler.Inverse();
        ConstPi = pi ?? new PiConstructiveReal();
    }

    public ConstructiveReal Add(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Add(op1, op2);
    }

    public ConstructiveReal Div(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Multiply(op1, ConstructiveRealAlgebra.Inverse(op2));
    }

    public ConstructiveReal E()
    {
        return Eeuler;
    }

    public virtual ConstructiveReal Function(string value, List<ConstructiveReal> parms)
    {
        if (value.Equals("abs", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Abs(parms[0]);
        }
        if (((value.Equals("sqrt", StringComparison.OrdinalIgnoreCase)) || (value == "√")) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Sqrt(parms[0]);
        }
        if (value.Equals("exp", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Exp(parms[0]); ;
        }
        if (value.Equals("sin", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new SinConstructiveReal(parms[0]); ;
        }
        if (value.Equals("asin", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new AsinConstructiveReal(parms[0]); ;
        }
        if (value.Equals("cos", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new CosConstructiveReal(parms[0]); ;
        }
        if (value.Equals("acos", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new AcosConstructiveReal(parms[0]); ;
        }
        if (value.Equals("tan", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new TanConstructiveReal(parms[0]); ;
        }
        if (value.Equals("atan", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return new AtanConstructiveReal(parms[0]); ;
        }
        if (value.Equals("ln", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Ln(parms[0]); ;
        }
        if (value.Equals("pow", StringComparison.OrdinalIgnoreCase) && parms.Count == 2)
        {
            return ConstructiveRealAlgebra.Pow(parms[0], parms[1]);
        }
        throw new SyntaxException($"Not supported: {value}");
    }

    public ConstructiveReal Integer(string value)
    {
        BigInteger bigInteger = BigInteger.Parse(value);
        if (bigInteger.IsZero)
        {
            return new ZeroConstructiveReal();
        }
        return new IntegerConstructiveReal(bigInteger);
    }

    private static System.Text.RegularExpressions.Regex _floatRegex = new(@"^(?<integral>[0-9]+)(\.(?<fractional>[0-9]*))?([eE](?<exp>[+-]?[0-9]+))?$", System.Text.RegularExpressions.RegexOptions.ExplicitCapture);
    public ConstructiveReal Float(string value)
    {
        var m = _floatRegex.Match(value);
        if (!m.Success) throw new SyntaxException($"Float format: {value}");
        var integral = BigInteger.Parse(m.Groups["integral"].Value, System.Globalization.CultureInfo.InvariantCulture);
        long E = 0;
        var frac = m.Groups["fractional"].Value;
        if (frac != "")
        {
            int i = frac.Length;
            E = -i;
            while (i > 0)
            {
                if (i >= 6)
                {
                    integral = integral * 1000000; i = i - 6;
                }
                else if (i >= 3)
                {
                    integral = integral * 1000; i = i - 3;
                }
                else
                {
                    integral = integral * 10; i = i - 1;
                }
            }
            integral = integral + BigInteger.Parse(frac, System.Globalization.CultureInfo.InvariantCulture);
        }
        if (integral.IsZero) return ZeroConstructiveReal.Instance;

        var exp = m.Groups["exp"].Value;
        if (exp != "")
        {
            E = E + long.Parse(exp, System.Globalization.CultureInfo.InvariantCulture);
        }
        if (E == 0) return new IntegerConstructiveReal(integral);
        return new IntegerConstructiveReal(integral).Multiply(new IntegerConstructiveReal(10).Pow(E));
    }

    public ConstructiveReal Mul(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Multiply(op1, op2);
    }

    public ConstructiveReal Negate(ConstructiveReal op)
    {
        return ConstructiveRealAlgebra.Negate(op);
    }

    public ConstructiveReal Pi()
    {
        return ConstPi;
    }

    public ConstructiveReal Pow(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Pow(op1, op2);
    }

    public ConstructiveReal Sub(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Add(op1, ConstructiveRealAlgebra.Negate(op2));
    }

    public ConstructiveReal EInv()
    {
        return EeulerInv;
    }
}