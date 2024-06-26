# ConstructiveReals

This repository contains a C# implementation of aribitrary precision arithmetic. It follows some ideas presented in
"Boehm, H.J.: The constructive reals as a Java library. J. Logic and Algebraic Programming 64, 1 (2005), 3–11".

## Background and Purpose

On computers IEEE-754 floating point numbers are often used to represent real numbers. This works acceptably well for many problems that do not accumulate unacceptable errors during calculation. They also tend to fail on problems that every stundent may enter into a calculator when playing with "big" exponents.

The Purpose of constructive reals is to represent numbers up to a much higher precision (just limited by the available memory of the machine) than is possible with fixed sized floating point numbers. Using enough precision it is thus possible to calculate without numerical error.

## Using the demo

You can run the program using `dotnet` command.

```powershell
$ dotnet run --project .\ConstructiveReals\
```

You can enter mathematical expressions for evaluation:

```
$ pi
  3.1415926535897932384626433832795028841971693993751058209749445923
$ sin(0.5*pi)
  1.0000000000000000000000000000000000000000000000000000000000000000
$ atan(sin(1E-100))
  0.0000000000000000000000000000000000000000000000000000000000000000
$ atan(sin(1E-64))
  0.0000000000000000000000000000000000000000000000000000000000000001
```

You can change the displayed precision of results using `set precision`:

```
$ set precision 128
    precison = 128
$ atan(sin(1E-100))
  0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000
$ pi
  3.14159265358979323846264338327950288419716939937510582097494459230781640628620899862803482534211706798214808651328230664709384461
```

You can also change the sensitivity of divisions-by-zero detection using `set division limit`:

```
$ 1.0/1.0E-10000
  DivideByZeroException: Attempted to divide by zero.
  set division limit -128000
      division limit = -128000
$ 1.0/1.0E-10000
  …large value…
```

## Accepted input

The expressions are parsed with a recursive descent [LL1 parser](./ConstructiveReals/ExpressionParsing.cs). It supports the following grammar:

```
SimpleExpression:
    ['+'|'-'] Term { ('+'|'-') Term } .
Term:
    Factor { ('*'|'/') Factor } .
Factor:
    (Number | ConstantOrFunction | '(' SimpleExpression ')' ) ['^' Factor] .
ConstantOrFunction:
    Identifier ['(' [SimpleExpression {','} SimpleExpression] ')'] .
Number:
    ('0'|..|'9') { '0'|..|'9' } ['.' { '0'|..|'9' }] [('e'|'E') ['+'|'-'] ('0'|..|'9') { '0'|..|'9' }] .
Identifier:
    ('a'|..|'z'|'A'|..|'Z') { ('a'|..|'z'|'A'|..|'Z') } .
```

Supported constants are `pi`, `e` and functions are `abs`, `sqrt`, `exp`, `pow`, `ln`, `sin`, `asin`, `cos`, `acos`,  `tan`, `atan`. All functions accept one argument, except for `pow` which uses two.

## How it works

The general idea is implemented in the `class` [ConstructiveReal](./ConstructiveReals/ConstructiveReal.cs) with the `Evaluate` function. It is supposed to return an integer approximation of a real value `x` up to a given precision. Thus it returns an integer satisfying `2^(n-1) < abs(x) < 2^(n+1)`. With `n` getting smaller, this approximaion grows in absolute value (it is thus represented by the `BigInteger` class) but the calculation error becomes smaller than `2^(n-1)`.

To calculate the various values and functions, some optional argument reduction techniques are employed. Then mostly taylor-series or newton-iteration are used to evaluate the approximation up to the required precision.

I found some handy argument reduction formulas in "Henrik Vestermark: The Math behind arbitrary precision for integer and floating point arithmetic", but for many parts Wikipedia will also bring lots of required insight.

I didn't take too much care to verify that domains of functions are checked; partly this is because for some checks the constructive real needs to be "fully evaluated"; otherwise I just didn't care. You may thus be able to get unexpected return values, e.g. `asin(1+1E-500)` might possibly return an invalid.

Some of the functions are able to refine the approximation that has been calculated for a given `n´` to a higher precision `n`; this works particularly for functions implemented with the newton-iteration, like `sqrt`. Other need to be completely reevaluated for a higher precision.
