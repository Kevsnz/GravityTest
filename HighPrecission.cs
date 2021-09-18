using System;
using System.Drawing;

namespace GravityTest
{
	using HPV2 = HighPrecisionVector2;

	#region HighPrecisionVector2

	[Serializable]
	public struct HighPrecisionVector2
	{
		public double X;
		public double Y;

		public HighPrecisionVector2 ( double x, double y )
		{
			X = x;
			Y = y;
		}

		public static HighPrecisionVector2 operator + ( HighPrecisionVector2 v1, HighPrecisionVector2 v2 ) { return new HighPrecisionVector2 ( v1.X + v2.X, v1.Y + v2.Y ); }
		public static HighPrecisionVector2 operator - ( HighPrecisionVector2 v1, HighPrecisionVector2 v2 ) { return new HighPrecisionVector2 ( v1.X - v2.X, v1.Y - v2.Y ); }
		public static HighPrecisionVector2 operator - ( HighPrecisionVector2 v ) { return new HighPrecisionVector2 ( -v.X, -v.Y ); }
		public static HighPrecisionVector2 operator * ( HighPrecisionVector2 v1, double c ) { return new HighPrecisionVector2 ( v1.X * c, v1.Y * c ); }
		public static HighPrecisionVector2 operator / ( HighPrecisionVector2 v1, double c ) { return new HighPrecisionVector2 ( v1.X / c, v1.Y / c ); }

		public static double Dot ( HighPrecisionVector2 v1, HighPrecisionVector2 v2 ) { return v1.X * v2.X + v1.Y * v2.Y; }
		public static double Cross ( HighPrecisionVector2 v1, HighPrecisionVector2 v2 ) { return v1.X * v2.Y - v1.Y * v2.X; }
		public static HPV2 Cross ( HPV2 vector, double s ) { return new HighPrecisionVector2 ( s * vector.Y, -s * vector.X ); }
		public static HPV2 Cross ( double s, HPV2 vector ) { return new HighPrecisionVector2 ( -s * vector.Y, s * vector.X ); }

		public double LengthSq
		{
			get { return X * X + Y * Y; }
		}

		public double Length
		{
			get { return Math.Sqrt ( X * X + Y * Y ); }
		}

		public bool IsZero { get { return ( X <= double.Epsilon && X >= -double.Epsilon ) || ( Y <= double.Epsilon && Y >= -double.Epsilon ); } }

		public double Normalize ()
		{
			double length = Length;
			X /= length;
			Y /= length;
			return length;
		}

		public static explicit operator PointF ( HighPrecisionVector2 v ) { return new PointF ( ( float ) v.X, ( float ) v.Y ); }

		public override string ToString () { return "X: " + X.ToString ( "0.000" ) + ", Y: " + Y.ToString ( "0.000" ); }
		public PointF ToPointF () { return new PointF ( ( float ) X, ( float ) Y ); }
	}

	#endregion

	#region HighPrecisionVector3

	public struct HighPrecisionVector3
	{
		public double X;
		public double Y;
		public double Z;

		public HighPrecisionVector3 ( double x, double y, double z )
		{
			X = x;
			Y = y;
			Z = z;
		}

		public static HighPrecisionVector3 operator + ( HighPrecisionVector3 v1, HighPrecisionVector3 v2 ) { return new HighPrecisionVector3 ( v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z ); }
		public static HighPrecisionVector3 operator - ( HighPrecisionVector3 v1, HighPrecisionVector3 v2 ) { return new HighPrecisionVector3 ( v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z ); }
		public static HighPrecisionVector3 operator - ( HighPrecisionVector3 v ) { return new HighPrecisionVector3 ( -v.X, -v.Y, -v.Z ); }
		public static HighPrecisionVector3 operator * ( HighPrecisionVector3 v, double c ) { return new HighPrecisionVector3 ( v.X * c, v.Y * c, v.Z * c ); }
		public static HighPrecisionVector3 operator / ( HighPrecisionVector3 v, double c ) { return new HighPrecisionVector3 ( v.X / c, v.Y / c, v.Z * c ); }

		public double LengthSq
		{
			get { return X * X + Y * Y + Z * Z; }
		}

		public double Length
		{
			get { return Math.Sqrt ( LengthSq ); }
		}

		public void Normalize ()
		{
			double length = Length;
			X /= length;
			Y /= length;
			Z /= length;
		}

		public override string ToString () { return "X: " + X + ", Y: " + Y + ", Z: " + Z; }
	}

	#endregion

	#region HighPrecisionMatrix2

	public class HighPrecisionMatrix2
	{
		private double[] _elements;

		public double[] Elements
		{
			get { return _elements; }
		}

		public HighPrecisionMatrix2 ()
		{
			_elements = new[] { 1.0, 0.0, 0.0, 1.0, 0.0, 0.0 };
		}

		public HighPrecisionMatrix2 ( double m1, double m2, double m3, double m4, double m5, double m6 )
		{
			_elements = new[] { m1, m2, m3, m4, m5, m6 };
		}

		public static HighPrecisionMatrix2 Rotate ( double angle )
		{
			double sin = Math.Sin ( angle );
			double cos = Math.Cos ( angle );
			return new HighPrecisionMatrix2 ( cos, -sin, sin, cos, 0, 0 );
		}

		public HPV2 Transform ( HPV2 v )
		{
			var r = new HighPrecisionVector2 ();
			r.X = v.X * _elements[ 0 ] + v.Y * _elements[ 1 ];
			r.Y = v.X * _elements[ 2 ] + v.Y * _elements[ 3 ];
			return r;
		}

		public void Transform ( HPV2[] vs )
		{
			for ( int i = 0; i < vs.Length; i++ )
			{
				var r = new HighPrecisionVector2 ();
				r.X = vs[ i ].X * _elements[ 0 ] + vs[ i ].Y * _elements[ 1 ];
				r.Y = vs[ i ].X * _elements[ 2 ] + vs[ i ].Y * _elements[ 3 ];
				vs[ i ] = r;
			}
		}
	}

	#endregion
}
