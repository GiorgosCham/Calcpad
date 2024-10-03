﻿using System;

namespace Calcpad.Core
{
    internal readonly struct Value : IEquatable<Value>, IComparable<Value>, IValue
    {
        internal const double LogicalZero = 1e-12;
        internal readonly double Re;
        internal readonly double Im;
        internal readonly Unit Units;
        internal readonly bool IsUnit;
        internal static readonly Value Zero = new(0d);
        internal static readonly Value One = new(1d);
        internal static readonly Value NaN = new(double.NaN);
        internal static readonly Value PositiveInfinity = new(double.PositiveInfinity);
        internal static readonly Value NegativeInfinity = new(double.NegativeInfinity);
        internal static readonly Value ComplexInfinity = new(double.PositiveInfinity, double.PositiveInfinity, null);

        internal Value(double re, double im, Unit units)
        {
            Re = re;
            Im = im;
            Units = units;
        }

        internal Value(in Complex number, Unit units) : this(number.Re, number.Im, units) { }

        internal Value(double number)
        {
            Re = number;
        }

        internal Value(double number, Unit units)
        {
            Re = number;
            Units = units;
        }

        internal Value(in Complex number)
        {
            Re = number.Re;
            Im = number.Im;
        }

        internal Value(Unit units)
        {
            Re = 1d;
            Units = units;
            IsUnit = true;
        }

        internal Value(double re, double im, Unit units, bool isUnit) : this(re, im, units)
        {
            IsUnit = isUnit;
        }

        internal Value(in Complex number, Unit units, bool isUnit) : this(number.Re, number.Im, units)
        {
            IsUnit = isUnit;
        }

        public override int GetHashCode() => HashCode.Combine(Re, Im, Units);

        public override bool Equals(object obj)
        {
            if (obj is Value v)
                return Equals(v);

            return false;
        }

        public bool Equals(Value other)
        {
            if (Units is null)
                return other.Units is null &&
                    Re.Equals(other.Re) &&
                    Im.Equals(other.Im);

            if (other.Units is null)
                return false;

            return Re.Equals(other.Re) &&
                Im.Equals(other.Im) &&
                Units.Equals(other.Units);
        }

        internal bool AlmostEquals(in Value other)
        {
            if (ReferenceEquals(Units, other.Units))
                return Re.AlmostEquals(other.Re) && Im.AlmostEquals(other.Im);

            if (!Units.IsConsistent(other.Units))
                return false;

            var d = Units.ConvertTo(other.Units);
            return Re.AlmostEquals(other.Re * d) &&
                Im.AlmostEquals(other.Im * d);
        }

        //For complex numbers the real parts are ordered first and 
        //then the imaginary parts if real are euals (lexicographic ordering)   
        //Although it is not strictly correct mathematically, 
        //it is useful for practical sorting in many cases. 

        public int CompareTo(Value other)
        {
            var d = Unit.Convert(Units, other.Units, ',');
            var result = Re.CompareTo(other.Re * d);
            return result == 0 ? Im.CompareTo(other.Im * d) : result;
        }

        public override string ToString()
        {
            var s = Units is null ? string.Empty : " " + Units.Text;
            return $"{Re}{Im: + 0i; - 0i; #}{s}";
        }

        internal bool IsReal => Complex.GetType(Re, Im) == Complex.Types.Real;
        internal bool IsComplex => Complex.GetType(Re, Im) == Complex.Types.Complex;

        internal Complex Complex => new(Re, Im);

        internal Value Conjugate => new(Re, -Im, Units);

        internal double Abs() => Complex.Abs(Re, Im);
        internal double SquaredAbs() => Complex.SquaredAbs(Re, Im);
        internal bool IsComposite() => Unit.IsComposite(Re, Units);

        //Operators are defined for real numbers only for performance reasons
        //Complex operators are evalauted through ComplexCalculator functions
        public static Value operator -(Value a) => new(-a.Re, a.Units, a.IsUnit);

        public static Value operator +(Value a, Value b) =>

            new(
                a.Re + b.Re * Unit.Convert(a.Units, b.Units, '+'),
                a.Units
            );

        public static Value operator -(Value a, Value b) =>
            new(
                a.Re - b.Re * Unit.Convert(a.Units, b.Units, '-'),
                a.Units
            );

        public static Value operator *(Value a, Value b)
        {
            if (a.Units is null)
            {
                if (b.Units is not null && b.Units.IsDimensionless && !b.IsUnit)
                    return new(a.Re * b.Re * b.Units.GetDimensionlessFactor(), null);

                return new(a.Re * b.Re, b.Units);
            }
            var uc = Unit.Multiply(a.Units, b.Units, out var d);
            return new(a.Re * b.Re * d, uc);
        }

        public static Value Multiply(in Value a, in Value b)
        {
            if (a.Units is null)
            {
                if (b.Units is not null && b.Units.IsDimensionless && !b.IsUnit)
                    return new(a.Re * b.Re * b.Units.GetDimensionlessFactor(), null);

                return new(a.Re * b.Re, b.Units);
            }
            var uc = Unit.Multiply(a.Units, b.Units, out var d, b.IsUnit);
            var isUnit = a.IsUnit && b.IsUnit && uc is not null;
            return new(a.Re * b.Re * d, uc, isUnit);
        }

        public static Value operator /(Value a, Value b)
        {
            var uc = Unit.Divide(a.Units, b.Units, out var d);
            return new(a.Re / b.Re * d, uc);
        }

        public static Value Divide(in Value a, in Value b)
        {
            var uc = Unit.Divide(a.Units, b.Units, out var d, b.IsUnit);
            var isUnit = a.IsUnit && b.IsUnit && uc is not null;
            return new(a.Re / b.Re * d, uc, isUnit);
        }

        public static Value operator *(Value a, double b) =>
            new(a.Re * b, a.Units);

        public static Value operator %(Value a, Value b)
        {
            if (b.Units is not null)
                Throw.CannotEvaluateRemainderException(Unit.GetText(a.Units), Unit.GetText(b.Units));

            return new(a.Re % b.Re, a.Units);
        }

        public static Value IntDiv(in Value a, in Value b)
        {
            var uc = Unit.Divide(a.Units, b.Units, out var d);
            bool isUnit = a.IsUnit && b.IsUnit && uc is not null;
            var c = b.Re == 0d ?
                double.NaN :
                Math.Truncate(a.Re / b.Re * d);
            return new(c, uc, isUnit);
        }

        public static Value operator ==(Value a, Value b) =>
            a.Re.AlmostEquals(b.Re * Unit.Convert(a.Units, b.Units, '≡')) ? One : Zero;

        public static Value operator !=(Value a, Value b) =>
            a.Re.AlmostEquals(b.Re * Unit.Convert(a.Units, b.Units, '≠')) ? Zero : One;

        public static Value operator <(Value a, Value b)
        {
            var c = a.Re;
            var d = b.Re * Unit.Convert(a.Units, b.Units, '<');
            return c < d && !c.AlmostEquals(d) ? One : Zero;
        }

        public static Value operator >(Value a, Value b)
        {
            var c = a.Re;
            var d = b.Re * Unit.Convert(a.Units, b.Units, '>');
            return c > d && !c.AlmostEquals(d) ? One : Zero;
        }

        public static Value operator <=(Value a, Value b)
        {
            var c = a.Re;
            var d = b.Re * Unit.Convert(a.Units, b.Units, '≤');
            return c <= d || c.AlmostEquals(d) ? One : Zero;
        }

        public static Value operator >=(Value a, Value b)
        {
            var c = a.Re;
            var d = b.Re * Unit.Convert(a.Units, b.Units, '≥');
            return c >= d || c.AlmostEquals(d) ? One : Zero;
        }

        public static Value operator &(Value a, Value b) =>
            Math.Abs(a.Re) < LogicalZero || Math.Abs(b.Re) < LogicalZero ? Zero : One;

        public static Value operator |(Value a, Value b) =>
            Math.Abs(a.Re) >= LogicalZero || Math.Abs(b.Re) >= LogicalZero ? One : Zero;

        public static Value operator ^(Value a, Value b) =>
            (Math.Abs(a.Re) >= LogicalZero) != (Math.Abs(b.Re) >= LogicalZero) ? One : Zero;

        //internal Value EvaluatePercent()
        //{
        //    if (Units is not null && Units.IsDimensionless)
        //        return new Value(Re, Im, null) * Unit.GetDimensionlessFactor(Units.Text[0]);
        //
        //    return this;
        //}
    }
}