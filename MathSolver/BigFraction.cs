using System.Numerics;
using System.Runtime.InteropServices.Swift;

struct BigFraction
{
    //hidden in case future optimizations use multiple values sometimes
    private BigInteger numerator;
    private BigInteger denominator;

    public BigInteger Numerator()
    {
        return numerator;
    }
    public BigInteger Denominator()
    {
        return denominator;
    }

    public BigFraction(BigInteger numerator, BigInteger denominator)
    {
        //ensure denominator is positive
        if (denominator.Sign == -1)
        {
            denominator *= -1;
            numerator *= -1;
        }

        //simplify fraction
        //TODO should this be a separate method instead?
        BigInteger gcd = BigInteger.GreatestCommonDivisor(numerator, denominator);

        this.numerator = numerator / gcd;
        this.denominator = denominator / gcd;
    }

    public static BigFraction operator +(BigFraction self, BigFraction operand)
    {
        BigInteger gcd = BigInteger.GreatestCommonDivisor(self.denominator, operand.denominator);
        BigInteger leftMult = self.denominator / gcd;
        BigInteger rightMult = operand.denominator / gcd;
        BigInteger denominator = leftMult * operand.denominator;
        BigInteger left = self.numerator * rightMult;
        BigInteger right = operand.numerator * leftMult;
        return new BigFraction(left + right, denominator);
    }
    public static BigFraction operator -(BigFraction self, BigFraction operand)
    {
        BigInteger gcd = BigInteger.GreatestCommonDivisor(self.denominator, operand.denominator);
        BigInteger leftMult = self.denominator / gcd;
        BigInteger rightMult = operand.denominator / gcd;
        BigInteger denominator = leftMult * operand.denominator;
        BigInteger left = self.numerator * rightMult;
        BigInteger right = operand.numerator * leftMult;
        return new BigFraction(left - right, denominator);
    }
    public static BigFraction operator *(BigFraction self, BigFraction operand)
    {
        return new BigFraction(self.numerator * operand.numerator, self.denominator * operand.denominator);
    }
    public static BigFraction operator /(BigFraction self, BigFraction operand)
    {
        return new BigFraction(self.numerator * operand.denominator, self.denominator * operand.numerator);
    }

    public bool IsZero { get { return numerator == 0; } }
    public bool IsWholeNum { get { return denominator.IsOne; } }

    public override string ToString()
    {
        if (denominator.IsOne) { return numerator.ToString(); }
        return numerator.ToString() + "/" + denominator.ToString();
    }
}