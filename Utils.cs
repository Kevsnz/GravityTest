using System;

namespace GravityTest
{
	static class Utils
	{
		public const double GRAVITY_CONSTANT = 0.0667; // real is 6.67E-11
		public const double AVERAGE_DENSITY = 100;
		public const double INFLUENCE_THRESHOLD = 5000;

		public static readonly Random Rnd = new Random ();

		public static double DistanceSq ( HighPrecisionVector2 a, HighPrecisionVector2 b ) { return ( a.X - b.X ) * ( a.X - b.X ) + ( a.Y - b.Y ) * ( a.Y - b.Y ); }
		public static double Distance ( HighPrecisionVector2 a, HighPrecisionVector2 b ) { return Math.Sqrt ( DistanceSq ( a, b ) ); }

		public static HighPrecisionVector2 Project ( HighPrecisionVector2 vector, HighPrecisionVector2 line )
		{
			var r = Math.Atan2 ( line.Y, line.X );

			HighPrecisionVector2 newV = HighPrecisionMatrix2.Rotate ( -r ).Transform ( vector );

			newV = HighPrecisionMatrix2.Rotate ( r ).Transform ( new HighPrecisionVector2 ( newV.X, 0 ) );

			return newV;
		}

		public static IObject Collide ( IObject o1, IObject o2 )
		{
			double mass = o1.Mass + o2.Mass;
			double radius = Math.Pow ( ( mass / AVERAGE_DENSITY * 1.1 ) / ( 4.0 / 3.0 * Math.PI ), 0.33333333333333 ) / Math.Pow ( mass, 0.05 );

			HighPrecisionVector2 pos = ( o1.Position * o1.Mass + o2.Position * o2.Mass ) / mass;
			HighPrecisionVector2 vel = ( o1.Velocity * o1.Mass + o2.Velocity * o2.Mass ) / mass;

			return new Body ( pos, mass, radius, vel, o1.Mass > o2.Mass ? ( Body ) o1 : ( Body ) o2 ) { IsDrawingTrail = true };
		}

		/*public static IObject[] CollideEx ( IObject o1, IObject o2 )
		{
			Trace.Assert ( o1 != null && o2 != null );

			HighPrecisionVector2 relVel = o1.Velocity - o2.Velocity;
			double totalMass = o1.Mass + o2.Mass;

			if (  ) {}
			double radius = Math.Pow ( ( totalMass / AVERAGE_DENSITY * 1.1 ) / ( 4 / 3 * Math.PI ), 0.33333333333333 ) / Math.Pow ( totalMass, 0.05 );

			HighPrecisionVector2 pos = ( o1.Position * o1.Mass + o2.Position * o2.Mass ) / totalMass;
			HighPrecisionVector2 vel = ( o1.Velocity * o1.Mass + o2.Velocity * o2.Mass ) / totalMass;

			return new IObject[]
			{
				new Body ( pos, totalMass, radius, vel ) { IsDrawingTrail = true }
			};
		}//*/

		public static void ApplyGravityInfluence ( IObject o1, IObject o2 )
		{
			if ( o1 == null || o2 == null )
				throw new ArgumentException ( "One of the objects is null" );

			HighPrecisionVector2 vector = o1.Position - o2.Position;
			double distSq = vector.LengthSq;
			double accValue1 = o1.GM / distSq;
			double accValue2 = o2.GM / distSq;

			if ( ( accValue1 * INFLUENCE_THRESHOLD ) * ( accValue1 * INFLUENCE_THRESHOLD ) < o2.Acceleration.LengthSq )
				if ( ( accValue2 * INFLUENCE_THRESHOLD ) * ( accValue2 * INFLUENCE_THRESHOLD ) < o1.Acceleration.LengthSq )
					return; // skip too little influences*/

			double dist = vector.Normalize ();

			o1.AddAcceleration ( -vector * accValue2, o2, dist );
			o2.AddAcceleration ( vector * accValue1, o1, dist );
		}

		public static double CalcPe ( double a, double e ) { return a * ( 1 - e ); }
		public static double CalcAp ( double a, double e ) { return a * ( 1 + e ); }
	}
}
