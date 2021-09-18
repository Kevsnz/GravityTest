using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace GravityTest
{
	using HPV2 = HighPrecisionVector2;

	[Serializable]
	class Body : IObject
	{
		private double _mass;
		private HPV2 _pos;
		private double _radius;
		private HPV2 _acceleration;
		private HPV2 _velocity;
		private readonly double _GM;

		private bool _drawTrail;

		public bool IsDrawingTrail
		{
			get { return _drawTrail; }
			set { _drawTrail = value; }
		}
		public int CountOfInfluences { get { return _countOfInfluences; } }
		public bool Processed { get; set; }
		public bool IsDead { get; set; }

		private readonly List<HPV2> _trail;
		private readonly Color _trailColor;
		private static Color _borderColor = Color.FromArgb ( 64, 96, 96 );
		private Color _coreColor;
		[NonSerialized]
		private Brush _coreBrush;
		private static Pen _dotPen = new Pen ( Color.FromArgb ( 127, 127, 127 ), 1 );
		private static Pen _selectionPen = new Pen ( Color.FromArgb ( 255, 255, 0 ), 1 );
		private HPV2 _lastTrailDir;
		private const int trail_min_step = 1;
		private const int trail_step_count = 100;
		private static readonly double _trailOffsetThreshold = Math.Pow ( Math.Sin ( Math.PI * 2 / trail_step_count ), 2 );
		private int _countOfInfluences;
		private double _centerBodyGM;
		private double _centerBodyDist = 1;
		private SpinLock _locker = new SpinLock ();

		public Body ( double x, double y, double mass, double radius ) : this ( new HighPrecisionVector2 ( x, y ), mass, radius, new HighPrecisionVector2 () ) { }
		public Body ( double x, double y, double mass, double radius, HPV2 initVelocity ) : this ( new HighPrecisionVector2 ( x, y ), mass, radius, initVelocity ) { }
		public Body ( HPV2 pos, double mass, double radius, HPV2 initVelocity ) : this ( pos, mass, radius, initVelocity, null ) { }
		public Body ( HPV2 pos, double mass, double radius, HPV2 initVelocity, Body trailCopy )
		{
			_pos = pos;
			_mass = mass;
			_GM = _mass * Utils.GRAVITY_CONSTANT;
			_radius = radius;
			_velocity = _lastTrailDir = initVelocity;

			if ( trailCopy == null )
			{
				_trail = new List<HighPrecisionVector2> ();
				const int trailColorMin = 24;
				const int trailColorMax = 80;

				int cr = Utils.Rnd.Next ( trailColorMin, trailColorMax );
				int cg = Utils.Rnd.Next ( trailColorMin, trailColorMax );
				int cb = Utils.Rnd.Next ( trailColorMin, trailColorMax );
				_trailColor = Color.FromArgb ( cr, cg, cb );
			}
			else
			{
				_trailColor = trailCopy._trailColor;
				_trail = trailCopy._trail;
				_lastTrailDir = trailCopy._lastTrailDir;
			}

			double ratio = Math.Max ( 0, _mass / Utils.AVERAGE_DENSITY / ( 4.1887902047863909846168 * Math.Pow ( _radius, 3 ) ) - 0.25 ); // that long double is (4/3 PI) constant
			double ratio2 = 0;
			double ratio3 = 0;
			if ( ratio > 1 )
			{
				ratio2 = ratio - 1;
				ratio = 1;
				if ( ratio2 > 1 )
				{
					ratio3 = Math.Min ( ratio2 - 1, 1 );
					ratio2 = 1;
				}
			}
			_coreColor = Color.FromArgb ( ( int ) ( 255 * ratio ), ( int ) ( 255 * ratio2 ), ( int ) ( 255 * ratio3 ) );
			_coreBrush = new SolidBrush ( _coreColor );
		}

		public void AfterDeserialization ()
		{
			double ratio = Math.Max ( 0, _mass / Utils.AVERAGE_DENSITY / ( 4.1887902047863909846168 * Math.Pow ( _radius, 3 ) ) - 0.25 ); // that long double is (4/3 PI) constant
			double ratio2 = 0;
			double ratio3 = 0;
			if ( ratio > 1 )
			{
				ratio2 = ratio - 1;
				ratio = 1;
				if ( ratio2 > 1 )
				{
					ratio3 = Math.Min ( ratio2 - 1, 1 );
					ratio2 = 1;
				}
			}
			_coreColor = Color.FromArgb ( ( int ) ( 255 * ratio ), ( int ) ( 255 * ratio2 ), ( int ) ( 255 * ratio3 ) );
			_coreBrush = new SolidBrush ( _coreColor );
		}

		#region IObject Members

		public void Draw ( Graphics g, ViewState viewState )
		{
			var size = ( float ) _radius; // was x2 for visibility
			float x1 = ( ( float ) _pos.X - size - viewState.ScreenX ) * viewState.Scale;
			float y1 = ( ( float ) _pos.Y - size - viewState.ScreenY ) * viewState.Scale;
			float x2 = ( size + size ) * viewState.Scale;
			float y2 = ( size + size ) * viewState.Scale;

			if ( x1 > viewState.Width || x1 + x2 < 0 || y1 > viewState.Height || y1 + y2 < 0 )
				return;

			if ( x2 < 1.5f || y2 < 1.5f ) // too far - draw white dot
			{
				g.DrawRectangle ( _dotPen, x1, y1, 0.5f, 0.5f );
				return;
			}

			g.FillEllipse ( _coreBrush, x1, y1, x2, y2 );

			using ( var p = new Pen ( _borderColor, Math.Min ( viewState.Scale, 1.5f ) ) )
				g.DrawEllipse ( p, x1, y1, x2, y2 );
		}

		public void DrawTrail ( Graphics g, ViewState viewState )
		{
			if ( !_drawTrail || !viewState.DrawTrails )
				return;

			using ( var p = new Pen ( _trailColor, Math.Min ( viewState.Scale, 1.5f ) ) )
			{
				HPV2 last = _pos;
				lock ( _trail )
					foreach ( HPV2 t in _trail )
					{
						float x1 = ( float ) ( last.X - viewState.ScreenX ) * viewState.Scale;
						float y1 = ( float ) ( last.Y - viewState.ScreenY ) * viewState.Scale;
						float x2 = ( float ) ( t.X - viewState.ScreenX ) * viewState.Scale;
						float y2 = ( float ) ( t.Y - viewState.ScreenY ) * viewState.Scale;

						if ( ( x1 > 0 && x1 < viewState.Width ) || ( x2 > 0 && x2 < viewState.Width ) )
							if ( ( y1 > 0 && y1 < viewState.Height ) || ( y2 > 0 && y2 < viewState.Height ) )
								g.DrawLine ( p, x1, y1, x2, y2 );
						last = t;
					}
			}
		}

		public void DrawSelection ( Graphics g, ViewState viewState )
		{
			var gap = ( float ) _radius * viewState.Scale + 2;
			var far = gap + 8;
			float x = ( ( float ) _pos.X - viewState.ScreenX ) * viewState.Scale;
			float y = ( ( float ) _pos.Y - viewState.ScreenY ) * viewState.Scale;

			if ( ( x > 0 && x < viewState.Width ) && ( y > 0 && y < viewState.Height ) )
			{
				g.DrawLine ( _selectionPen, x - far, y - far, x - gap, y - gap );
				g.DrawLine ( _selectionPen, x + far, y + far, x + gap, y + gap );
				g.DrawLine ( _selectionPen, x - far, y + far, x - gap, y + gap );
				g.DrawLine ( _selectionPen, x + far, y - far, x + gap, y - gap );
			}
		}

		public double NextStepTime { get; set; }
		public double GetBestDeltaT ()
		{
			return 0.001;
			/*const double minDt = 0.0001;
			const double maxDt = 0.01;

			double v1 = _velocity.X + _velocity.Y;
			double v2 = _acceleration.X + _acceleration.Y;

			double v = Math.Min ( Math.Abs ( v1 ), Math.Abs ( v2 ) );

			v = 0.02 - v/15000-0.0075;

			v = Math.Min ( v, maxDt );
			v = Math.Max ( v, minDt );

			return v;//*/
		}

		public void AddAcceleration ( HPV2 acc, IObject obj, double dist )
		{
			_acceleration += acc;
			_countOfInfluences++;

			/*if ( _centerBodyGM < obj.GM )
			{
				_centerBodyGM = obj.GM;
				_centerBodyDist = dist;
			}*/
		}

		public void ResetAccelerations ()
		{
			_acceleration = new HighPrecisionVector2 ();
			_countOfInfluences = 0;
			_centerBodyGM = 0;
		}

		public void ApplyGravityForce ( IObject obj )
		{
			double distSq = Utils.DistanceSq ( _pos, obj.Position );
			double accValue = obj.GM / distSq;

			HPV2 vector = obj.Position - _pos;
			//double dist = vector.Normalize ();
			_acceleration += vector * accValue;

			_countOfInfluences++;
			/*if ( _centerBodyGM < obj.GM )
			{
				_centerBodyGM = obj.GM;
				_centerBodyDist = dist;
			}*/
		}

		public static void ApplyGravityForces ( Body obj1, Body obj2 )
		{
			double distSq = Utils.DistanceSq ( obj1._pos, obj2._pos );
			HPV2 vector = obj2._pos - obj1._pos;
			//double dist = vector.Normalize ();

			//ParallelRunner.SyncDo ( ref obj1._locker, () =>
			{
				obj1._acceleration += vector * obj2.GM / distSq;
				obj1._countOfInfluences++;
				/*if ( obj1._centerBodyGM < obj1.GM )
				{
					obj1._centerBodyGM = obj1.GM;
					obj1._centerBodyDist = dist;
				}*/
			} //);

			//ParallelRunner.SyncDo ( ref obj2._locker, () =>
			{
				obj2._acceleration -= vector * obj1.GM / distSq;
				obj2._countOfInfluences++;

				/*if ( obj2._centerBodyGM < obj2.GM )
				{
					obj2._centerBodyGM = obj2.GM;
					obj2._centerBodyDist = dist;
				}*/
			} //);
		}

		public static void ExchangeImpulse ( Body obj1, Body obj2, double dt )
		{
			HPV2 vector = obj2._pos - obj1._pos;
			double distSq = vector.LengthSq;
			vector.Normalize ();

			/*
			 * p = mv
			 * v = at
			 * dV = a dt
			 * F = ma = G m1 m2 / r^2
			 * dP1 = m1 dV = m1 a dt = F dt = G m1 m2 dt / r^2
			 * dV1 = dP1 / m1 = G m2 dt / r^2
			 */

			double dp = Utils.GRAVITY_CONSTANT * obj1.Mass * obj2.Mass / distSq * dt;

			obj1._velocity += vector * dp / obj1.Mass;
			obj2._velocity -= vector * dp / obj2.Mass;
		}

		public void IntegrateVelocity ( double dt )
		{
			_velocity += _acceleration * dt;
		}

		public void IntegratePosition ( double dt )
		{
			if ( _drawTrail )
				recordTrail ( dt );
			_pos += _velocity * dt;
		}

		private void recordTrail ( double dt )
		{
			if ( _trail.Count > 0 )
			{
				// Yes! That's very close to ideal.
				HPV2 dir = _pos - _trail[ 0 ];
				if ( trail_min_step * trail_min_step > dir.LengthSq )
					return;

				if ( _trail.Count > 1 )
				{
					double dot = HPV2.Dot ( _velocity, _lastTrailDir );

					if ( dot > 0 )
					{
						double offset = dot * dot / _velocity.LengthSq / _lastTrailDir.LengthSq;
						if ( offset > 1 - _trailOffsetThreshold )
							return;
					}
				}
			}

			lock ( _trail )
			{
				_trail.Insert ( 0, _pos );
				while ( _trail.Count > trail_step_count )
					_trail.RemoveAt ( _trail.Count - 1 );
			}
			_lastTrailDir = _velocity;
		}

		public HPV2 Position { get { return _pos; } set { _pos = value; } }
		public double Mass { get { return _mass; } }
		public double GM { get { return _GM; } }
		public double Radius { get { return _radius; } }

		public void ChangeMass ( double mass, bool adjustRadius )
		{
			_mass = mass;
			if ( adjustRadius )
				_radius = Math.Pow ( ( mass / Utils.AVERAGE_DENSITY * 1.1 ) / ( 4.0 / 3.0 * Math.PI ), 0.33333333333333 ) / Math.Pow ( mass, 0.05 );
		}

		public void ChangeRadius ( double radius )
		{
			_radius = radius;
		}

		public OrbitalInfo CalcOrbitalInfo ( IObject refBody )
		{
			var oi = new OrbitalInfo ();
			double dist = ( _pos - refBody.Position ).Length;

			double invSMA = 2 / dist - _velocity.LengthSq / refBody.GM;
			oi.SMA = 1 / invSMA;

			double rvgm = dist * _velocity.LengthSq / refBody.GM;

			HPV2 vect = _pos - refBody.Position;
			double dot = HPV2.Dot ( _velocity, vect );
			double cross = HPV2.Cross ( _velocity, vect );
			double cosSq = dot * dot / _velocity.LengthSq / vect.LengthSq;
			double sinSq = cross * cross / _velocity.LengthSq / vect.LengthSq;
			double resSq = Math.Pow ( rvgm - 1, 2 ) * sinSq + cosSq;
			oi.Ecc = Math.Sqrt ( resSq );

			oi.Apoapsis = Utils.CalcAp ( oi.SMA, oi.Ecc );
			oi.Periapsis = Utils.CalcPe ( oi.SMA, oi.Ecc );

			double tanTrueAnomaly = rvgm * Math.Sqrt ( sinSq * cosSq ) / ( rvgm * sinSq - 1 );
			oi.TrueAnomaly = Math.Atan ( tanTrueAnomaly ) * 180 / Math.PI;

			return oi;
		}

		public HPV2 Velocity
		{
			get { return _velocity; }
			set { _velocity = new HPV2 ( value.X, value.Y ); }
		}

		public HPV2 Acceleration { get { return _acceleration; } }

		public bool IsColliding ( IObject obj )
		{
			if ( IsDead || obj.IsDead )
				return false;

			double collideDist = ( Radius + obj.Radius ) * 0.75;
			double distSq = Utils.DistanceSq ( Position, obj.Position );

			return distSq < collideDist * collideDist;
		}

		public double CalcEnergy ()
		{
			if ( IsDead )
				return 0;
			return _mass * _velocity.LengthSq / 2; // kinetic energy only
			//return -_centerBodyGM * _mass / 2 * ( 2 / _centerBodyDist - _velocity.LengthSq / _centerBodyGM ); // total energy
			//return _mass * ( _velocity.LengthSq / 2 + _centerBodyGM / _centerBodyDist ); // m*v^2 + m*g*h
		}

		#endregion
	}
}
