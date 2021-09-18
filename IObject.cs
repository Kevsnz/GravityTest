using System.Drawing;

namespace GravityTest
{
	using HPV2 = HighPrecisionVector2;

	interface IObject
	{
		HPV2 Position { get; set; }
		double Mass { get; }
		HPV2 Velocity { get; set; }
		HPV2 Acceleration { get; }
		double Radius { get; }
		bool IsDrawingTrail { get; set; }
		double GM { get; }
		bool Processed { get; set; }

		void Draw ( Graphics g, ViewState viewState );
		void DrawTrail ( Graphics g, ViewState viewState );
		void DrawSelection ( Graphics g, ViewState viewState );

		void AddAcceleration ( HPV2 acc, IObject obj, double dist );
		void ResetAccelerations ();
		void ApplyGravityForce ( IObject obj );

		void IntegrateVelocity ( double dt );
		void IntegratePosition ( double dt );

		bool IsColliding ( IObject obj );
		double CalcEnergy ();
		int CountOfInfluences { get; }

		OrbitalInfo CalcOrbitalInfo ( IObject refBody );

		void AfterDeserialization ();

		void ChangeMass ( double mass, bool ajustRadius );
		void ChangeRadius ( double radius );

		double GetBestDeltaT ();
		double NextStepTime { get; set; }

		bool IsDead { get; set; }
	}
}
