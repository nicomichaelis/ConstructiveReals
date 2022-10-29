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

        while (ch >= '0' & ch <= '9')
        {
            builder.Append((char)ch);
            Read(ref ch, ref offset, reader);
        }
        TokenType type = TokenType.Integer;

        return new Token(startoffset, builder.ToString(), type);
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
                _ => throw new NotSupportedException()
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
                _ => throw new NotSupportedException()
            };
        }
        return term;
    }

    private T Factor(Scanner scan)
    {
        Token t = scan.Current;
        Expect(scan, TokenType.Integer, TokenType.Identifier, TokenType.OpenParen);
        T factor;
        if (t.Type == TokenType.Integer)
        {
            factor = Factory.Integer(t.Value);
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
        else throw new NotImplementedException();

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
        else if (t.Value == "e")
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
        if (scanner.Current.Type != type) throw new Exception($"{type} expected at {scanner.Current.Offset}:'{scanner.Current.Value}'");
        scanner.Next();
    }

    private Token Expect(Scanner scanner, params TokenType[] types)
    {
        var cur = scanner.Current;
        var curt = cur.Type;
        if (!types.Any(z => z == curt)) throw new Exception($"{string.Join(", ", types)} expected at {scanner.Current.Offset}:'{scanner.Current.Value}'");
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
        throw new NotImplementedException();
    }

    public ConstructiveReal Function(string value, List<ConstructiveReal> parms)
    {
        if (value.Equals("abs", StringComparison.OrdinalIgnoreCase) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Abs(parms[0]);
        }
        if (((value.Equals("sqrt", StringComparison.OrdinalIgnoreCase)) || (value == "√")) && parms.Count == 1)
        {
            return ConstructiveRealAlgebra.Sqrt(parms[0]);
        }
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public ConstructiveReal Pow(ConstructiveReal op1, ConstructiveReal op2)
    {
        throw new NotImplementedException();
    }

    public ConstructiveReal Sub(ConstructiveReal op1, ConstructiveReal op2)
    {
        return ConstructiveRealAlgebra.Add(op1, ConstructiveRealAlgebra.Negate(op2));
    }
}