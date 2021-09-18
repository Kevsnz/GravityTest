using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GravityTest
{
	using HPV2 = HighPrecisionVector2;

	enum ControlAction
	{
		PanLeft,
		PanRight,
		PanUp,
		PanDown,
		ZoomIn,
		ZoomOut,
		TimeScaleFaster,
		TimeScaleSlower,
		ToggleDrawObjects,
		ToggleDrawTrails,
		TogglePause,
		ToggleSOI,
		ToggleDrawGrid,
		ToggleTrackSelection,
	}

	public class Form1 : Form
	{
		private static readonly Color[] _inflColors =
		{
			Color.FromArgb(0,0,0),
			Color.FromArgb(255,0,0),
			Color.FromArgb(0,255,0),
			Color.FromArgb(0,0,255),
			Color.FromArgb(255,255,0),
			Color.FromArgb(255,0,255),
			Color.FromArgb(0,255,255),
			Color.FromArgb(127,255,0),
			Color.FromArgb(127,0,255),
			Color.FromArgb(255,127,0),
			Color.FromArgb(0,127,255),
			Color.FromArgb(255,0,127),
			Color.FromArgb(0,255,127),
		};

		private const int max_dt_ms = 1;
		private const int center_body_mass = 4000000;
		private readonly Thread _mainPhysicsThread;
		private double _timeScale = 1;
		private double _timeRate = 1;
		private bool _paused;
		private readonly List<IObject> _objects = new List<IObject> ();
		private IObject _trackedObject;
		private readonly List<IObject> _selectedObjects = new List<IObject> ();
		private bool _trackSelectedObjects;
		private bool _stopSimulation;

		private double _simTime;
		private double _lastSimTime;
		private TimeSpan _stepTime;
		private readonly ViewState _viewState;
		private bool _drawObjects = true;
		private readonly Dictionary<ControlAction, Keys> _keyBinds = new Dictionary<ControlAction, Keys> ();
		private readonly Dictionary<Keys, bool> _input = new Dictionary<Keys, bool> ();
		private readonly Stopwatch _inputTimer = new Stopwatch ();
		private long _inputLastTicks;

		//private double _averageInfluences;
		private double _totalEnergy;
		private double _totalAngMomentum;
		private bool _showSOI;
		private bool _drawGrid;
		private HPV2 _centerOfMass;
		private HPV2 _comVelocity;

		private const string format11 = "0.0";
		private const string format12 = "0.00";
		private const string format13 = "0.000";
		private const string format14 = "0.0000";

		private OrbitalInfo _trackedObjectInfo;

		public Form1 ()
		{
			InitializeComponent ();

			this.SetStyle ( ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true );

			_keyBinds.Add ( ControlAction.ZoomIn, Keys.Add );
			_keyBinds.Add ( ControlAction.ZoomOut, Keys.Subtract );
			_keyBinds.Add ( ControlAction.PanLeft, Keys.Left );
			_keyBinds.Add ( ControlAction.PanRight, Keys.Right );
			_keyBinds.Add ( ControlAction.PanUp, Keys.Up );
			_keyBinds.Add ( ControlAction.PanDown, Keys.Down );
			_keyBinds.Add ( ControlAction.TimeScaleFaster, Keys.OemPeriod );
			_keyBinds.Add ( ControlAction.TimeScaleSlower, Keys.Oemcomma );
			_keyBinds.Add ( ControlAction.ToggleDrawObjects, Keys.Space );
			_keyBinds.Add ( ControlAction.ToggleDrawTrails, Keys.T );
			_keyBinds.Add ( ControlAction.TogglePause, Keys.P );
			_keyBinds.Add ( ControlAction.ToggleSOI, Keys.S );
			_keyBinds.Add ( ControlAction.ToggleDrawGrid, Keys.G );
			_keyBinds.Add ( ControlAction.ToggleTrackSelection, Keys.Q );

			foreach ( var v in _keyBinds.Values )
				_input.Add ( v, false );

			_viewState = new ViewState ( this.ClientRectangle.Width, this.ClientRectangle.Height );
			_viewState.ScreenX = -_viewState.Width / 2f;
			_viewState.ScreenY = -_viewState.Height / 2f;

			/*string loadFromFile = null;

			using ( var ofd = new OpenFileDialog () )
			{
				if ( ofd.ShowDialog ( this ) == DialogResult.OK )
					loadFromFile = ofd.FileName;
			}//*/

			/*if ( !string.IsNullOrEmpty ( loadFromFile ) )
			{
				SimState ss = loadState ( loadFromFile );
				_objects = ss.ObjectList;
				_simTime = _lastSimTime = ss.SimTime;
			}
			else*/
			{
				/*_objects.Add ( new Body ( 0, 150, 10, 1, new HighPrecisionVector2 ( 0, 1 ) ) { IsDrawingTrail = true } );
				_objects.Add ( new Body ( -30, 0, 100, 2, new HighPrecisionVector2 ( 0, -1 ) ) { IsDrawingTrail = true } );
				_objects.Add ( new Body ( 30, 0, 600, 3, new HighPrecisionVector2 ( 0, 0 ) ) { IsDrawingTrail = true } );//*/

				_objects.Add ( new Body ( 0, 0, center_body_mass, 15 ) );

				const int objectCount = 300;
				for ( int i = 0; i < objectCount; i++ )
				{
					double dist = Math.Pow ( Utils.Rnd.NextDouble (), 1 ) * 400 + 250;
					double angle = Utils.Rnd.NextDouble () * 2 * Math.PI;

					double posX = dist * Math.Cos ( angle );
					double posY = dist * Math.Sin ( angle );

					double radius = ( Utils.Rnd.NextDouble () ) * 0.2 + 0.15;
					double mass = 4.18879020478639098461685 * Math.Pow ( radius, 3 ) * Utils.AVERAGE_DENSITY * ( Utils.Rnd.NextDouble () + 0.5 ); // that big double is 4/3 PI

					const double vari = 0.1;
					var vel = new HighPrecisionVector2 ( -posY, posX );
					vel.Normalize ();
					vel *= Math.Sqrt ( Utils.GRAVITY_CONSTANT * center_body_mass / dist ) * ( Utils.Rnd.NextDouble () * ( vari * 2 ) + ( 1 - vari ) );
					//vel *= Math.Sqrt ( Utils.GRAVITY_CONSTANT * 3 * objectCount / dist ) * ( Utils.Rnd.NextDouble () * ( vari * 2 ) + Math.Pow ( 1 - vari, 2 ) );

					IObject b = new Body ( posX, posY, mass, radius, vel );

					//if ( i == 3 )
					b.IsDrawingTrail = true;

					_objects.Add ( b );
				}//*/

				/*const int objectCount = 500;
				//_objects.Add ( new Body ( 0, 0, center_body_mass, 15 ) );
				for ( int i = 0; i < objectCount; i++ )
				{
					//double posX = Utils.Rnd.NextDouble () * 1500 - 500;
					//double posY = Utils.Rnd.NextDouble () * 1500 - 500;

					double dist = Math.Pow ( Utils.Rnd.NextDouble (), 0.5 ) * 500;
					double angle = Utils.Rnd.NextDouble () * 2 * Math.PI;

					double posX = dist * Math.Cos ( angle );
					double posY = dist * Math.Sin ( angle );

					double radius = Math.Pow ( Utils.Rnd.NextDouble (), 20 ) * 3 + 0.3;
					double mass = 4.18879020478639098461685 * Math.Pow ( radius, 3 ) * Utils.AVERAGE_DENSITY * ( Utils.Rnd.NextDouble () + 0.5 ); // that big double is 4/3 PI

					const int velRange = 50;
					double velX = Utils.Rnd.NextDouble () * 2 * velRange - velRange;
					double velY = Utils.Rnd.NextDouble () * 2 * velRange - velRange;
					var vel = new HPV2 ( velX, velY );

					IObject obj = new Body ( posX, posY, mass, radius, vel );
					obj.IsDrawingTrail = true;

					_objects.Add ( obj );
				}//*/
			}

			adjustCenterOfMass ();
			foreach ( var o in _objects )
				o.Velocity -= _comVelocity;

			_objects.Sort ( ( o1, o2 ) => o2.Mass.CompareTo ( o1.Mass ) );

			_mainPhysicsThread = new Thread ( runSimulation )
			{
				Name = "Main Physics Thread",
				Priority = ThreadPriority.AboveNormal
			};
			_mainPhysicsThread.Start ();

			_inputTimer.Start ();
		}

		#region Save/Load

		/*private void saveState ( string filename )
		{
			var ss = new SimState ();
			ss.SimTime = _simTime;
			ss.ObjectList = _objects;

			var formatter = new BinaryFormatter ();

			using ( var sr = new FileStream ( filename, FileMode.Create, FileAccess.Write ) )
				formatter.Serialize ( sr, ss );
		}*/

		/*private SimState loadState ( string filename )
		{
			SimState ss;

			try
			{
				var formatter = new BinaryFormatter ();
				using ( var sr = new FileStream ( filename, FileMode.Open, FileAccess.Read ) )
					ss = ( SimState ) formatter.Deserialize ( sr );
				foreach ( var o in ss.ObjectList )
					o.AfterDeserialization ();
			}
			catch ( Exception )
			{
				return null;
			}

			return ss;
		}*/

		#endregion

		private void runSimulation ()
		{
			Thread.Sleep ( 100 );

			var simulationTime = new Stopwatch ();
			var realTime = new Stopwatch ();
			simulationTime.Start ();
			realTime.Start ();
			double lastEnergyTime = 0;
			double simTime = _lastSimTime = simulationTime.ElapsedMilliseconds;
			long lastRealT = simulationTime.ElapsedMilliseconds;
			double trLastSimTime = simTime;
			long trLastRealTime = lastRealT;

			while ( !_stopSimulation )
			{
				realTime.Reset ();
				realTime.Start ();

				var realT = simulationTime.ElapsedMilliseconds;
				long deltaTReal = realT - lastRealT;
				lastRealT = realT;

				if ( !_paused )
					simTime += deltaTReal * _timeScale;

				while ( ( simTime - _lastSimTime ) > max_dt_ms )
				{
					calcPhysics ( max_dt_ms / 1000.0 );

					if ( realTime.ElapsedMilliseconds < 50 )
						_lastSimTime += max_dt_ms;
					else
					{
						simTime = _lastSimTime; // avoid overhead accumulation
						break;
					}
				}

				_stepTime = realTime.Elapsed;

				if ( realT - trLastRealTime > 500 )
				{
					_timeRate = ( simTime - trLastSimTime ) / ( realT - trLastRealTime );
					trLastRealTime = realT;
					trLastSimTime = simTime;
				}

				if ( lastEnergyTime + 100 < _lastSimTime )
				{
					double totalE = 0;
					double angMom = 0;
					for ( int i = 0; i < _objects.Count; i++ )
					{
						IObject o = _objects[ i ];
						totalE -= o.Mass * o.Velocity.LengthSq / 2; // kinetic energy
						for ( int j = i + 1; j < _objects.Count; j++ )
							totalE += o.GM * _objects[ j ].Mass / ( o.Position - _objects[ j ].Position ).Length;

						angMom += HPV2.Cross ( o.Position, o.Velocity * o.Mass );
					}
					_totalEnergy = totalE;
					_totalAngMomentum = angMom;

					/*int c = 0;
					for ( int i = 0; i < _objects.Count; i++ )
						c += _objects[ i ].CountOfInfluences;
					_averageInfluences = ( double ) c / ( _objects.Count - 1 );*/

					foreach ( var o in _objects )
						o.Velocity -= _comVelocity;
					_objects.Sort ( ( o1, o2 ) => o2.Mass.CompareTo ( o1.Mass ) );

					lastEnergyTime = _lastSimTime;
				}

				this.Invalidate ();
				Application.DoEvents ();
				if ( _paused )
					Thread.Sleep ( 1 );
			}

			//saveState ( "Sim " + DateTime.Now.ToString ( "yyyy-MM-dd HH-mm-ss" ) + " - " + _objects.Count + " objects at " + _simTime.ToString ( "0" ) + " ms.bin" );
		}

		private void calcPhysics ( double dt )
		{
			var newObjs = new List<IObject> ();

			ParallelRunner.RunInParallel ( i =>
			{
				/*
				_objects[ i ].ResetAccelerations ();
				foreach ( var o in _objects )
					if ( o != _objects[ i ] )
						_objects[ i ].ApplyGravityForce ( o );
				_objects[ i ].IntegrateVelocity ( dt );
				/*/
				for ( int j = i + 1; j < _objects.Count; j++ )
					Body.ExchangeImpulse ( ( Body ) _objects[ i ], ( Body ) _objects[ j ], dt );//*/

				_objects[ i ].IntegratePosition ( dt );
				if ( _objects[ i ].Position.LengthSq > 1000000.0 * 1000000.0 )
					_objects[ i ].IsDead = true;
			}, _objects.Count );

			ParallelRunner.RunInParallel ( i =>
			{
				if ( _objects[ i ].IsDead )
					return;
				//_objects[ i ].ResetAccelerations ();

				for ( int j = i + 1; j < _objects.Count; j++ )
				{
					if ( _objects[ j ].IsDead || !_objects[ j ].IsColliding ( _objects[ i ] ) )
						continue;

					_objects[ i ].IsDead = _objects[ j ].IsDead = true;
					IObject newObj = Utils.Collide ( _objects[ i ], _objects[ j ] );
					newObjs.Add ( newObj );
					if ( _trackedObject == _objects[ i ] || _trackedObject == _objects[ j ] )
						_trackedObject = newObj;
					if ( _selectedObjects.Contains ( _objects[ i ] ) )
						_selectedObjects.Remove ( _objects[ i ] );
					if ( _selectedObjects.Contains ( _objects[ j ] ) )
						_selectedObjects.Remove ( _objects[ j ] );

					return;
				}
			}, _objects.Count );

			for ( int i = _objects.Count - 1; i >= 0; i-- )
				if ( _objects[ i ] == null || _objects[ i ].IsDead )
					_objects.RemoveAt ( i );
			_objects.AddRange ( newObjs );

			adjustCenterOfMass ();
			if ( _trackedObject != null && _trackedObject.IsDead )
				_trackedObject = null;

			if ( dt > 0 && _trackedObject != null && _trackedObject != _objects[ 0 ] )
				_trackedObjectInfo = _trackedObject.CalcOrbitalInfo ( _objects[ 0 ] );
		}

		private void adjustCenterOfMass ()
		{
			var com = new HighPrecisionVector2 ();
			double totalMass = 0;
			var totalImpulse = new HighPrecisionVector2 ();
			foreach ( IObject o in _objects )
			{
				com += o.Position * o.Mass;
				totalMass += o.Mass;
				totalImpulse += o.Velocity * o.Mass;
			}
			_centerOfMass = com / totalMass;
			_comVelocity = totalImpulse / totalMass;

			/*for ( int i = _objects.Count - 1; i >= 0; i-- )
				_objects[ i ].Position -= _centerOfMass;//*/
		}

		private void handleInput ( double dt )
		{
			if ( _input[ _keyBinds[ ControlAction.ToggleDrawObjects ] ] )
			{
				_drawObjects = !_drawObjects;
				_input[ _keyBinds[ ControlAction.ToggleDrawObjects ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.ToggleDrawGrid ] ] )
			{
				_drawGrid = !_drawGrid;
				_input[ _keyBinds[ ControlAction.ToggleDrawGrid ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.ToggleTrackSelection ] ] )
			{
				_trackSelectedObjects = !_trackSelectedObjects;
				_input[ _keyBinds[ ControlAction.ToggleTrackSelection ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.ToggleDrawTrails ] ] )
			{
				_viewState.DrawTrails = !_viewState.DrawTrails;
				_input[ _keyBinds[ ControlAction.ToggleDrawTrails ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.ZoomIn ] ] )
			{
				_viewState.Scale *= ( float ) Math.Pow ( 2, dt );
				_viewState.ScreenX -= ( float ) ( _viewState.Width / 2 * ( 1 - Math.Pow ( 2, dt ) ) / _viewState.Scale );
				_viewState.ScreenY -= ( float ) ( _viewState.Height / 2 * ( 1 - Math.Pow ( 2, dt ) ) / _viewState.Scale );
			}
			if ( _input[ _keyBinds[ ControlAction.ZoomOut ] ] )
			{
				_viewState.Scale /= ( float ) Math.Pow ( 2, dt );
				_viewState.ScreenX += ( float ) ( _viewState.Width / 2 * ( 1 - Math.Pow ( 2, dt ) ) / _viewState.Scale );
				_viewState.ScreenY += ( float ) ( _viewState.Height / 2 * ( 1 - Math.Pow ( 2, dt ) ) / _viewState.Scale );
			}

			if ( _input[ _keyBinds[ ControlAction.PanLeft ] ] )
			{
				_viewState.ScreenX -= ( float ) ( 500 * dt / _viewState.Scale );
				_trackSelectedObjects = false;
			}
			if ( _input[ _keyBinds[ ControlAction.PanRight ] ] )
			{
				_viewState.ScreenX += ( float ) ( 500 * dt / _viewState.Scale );
				_trackSelectedObjects = false;
			}
			if ( _input[ _keyBinds[ ControlAction.PanUp ] ] )
			{
				_viewState.ScreenY -= ( float ) ( 500 * dt / _viewState.Scale );
				_trackSelectedObjects = false;
			}
			if ( _input[ _keyBinds[ ControlAction.PanDown ] ] )
			{
				_viewState.ScreenY += ( float ) ( 500 * dt / _viewState.Scale );
				_trackSelectedObjects = false;
			}

			if ( _input[ _keyBinds[ ControlAction.TimeScaleFaster ] ] )
			{
				_timeScale *= 2;
				_input[ _keyBinds[ ControlAction.TimeScaleFaster ] ] = false;
			}
			if ( _input[ _keyBinds[ ControlAction.TimeScaleSlower ] ] )
			{
				_timeScale /= 2;
				_input[ _keyBinds[ ControlAction.TimeScaleSlower ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.TogglePause ] ] )
			{
				if ( !_showSOI )
					_paused = !_paused;
				_input[ _keyBinds[ ControlAction.TogglePause ] ] = false;
			}

			if ( _input[ _keyBinds[ ControlAction.ToggleSOI ] ] )
			{
				if ( _paused )
					_showSOI = !_showSOI;
				_input[ _keyBinds[ ControlAction.ToggleSOI ] ] = false;
			}
		}

		private double calcTotalMass ()
		{
			double mass = 0;
			for ( int i = _objects.Count - 1; i >= 0; i-- )
				mass += _objects[ i ].Mass;
			return mass;
		}

		private const string c_str_sim_time = "Simulation time: {0} s (x{1}), {2}";
		private const string c_str_step_time = "Step time: {0}";
		private const string c_str_obj_count = "Object count: {0}";
		//private const string c_str_avg_infl = "Average count of influences: {0}";
		private const string c_str_tot_mass = "Total mass: {0}";
		private const string c_str_total_e = "Total energy: {0}, Momentum: {1}";
		private const string c_str_com = "Center Of Mass: {0}";
		private const string c_str_com_vel = "CoM Velocity: {0}";

		private const string c_str_m = "Mass: {0}";
		private const string c_str_sma = "SMA: {0}";
		private const string c_str_ecc = "Ecc: {0}";
		private const string c_str_ap = "Ap: {0}";
		private const string c_str_pe = "Pe: {0}";
		private const string c_str_ta = "TA: {0}";

		private readonly Font _font = new Font ( "Calibri", 10 );
		private readonly int _fh = 12;
		private readonly SolidBrush _brush = new SolidBrush ( Color.White );
		private readonly Pen _gridPen = new Pen ( Color.FromArgb ( 64, 64, 64 ) );
		private readonly Pen _rectPen = new Pen ( Color.FromArgb ( 192, 192, 192 ) );
		protected override void OnPaint ( PaintEventArgs e )
		{
			base.OnPaint ( e );

			long curTicks = _inputTimer.ElapsedTicks;
			double dt = ( double ) ( curTicks - _inputLastTicks ) / Stopwatch.Frequency;
			handleInput ( dt );
			_inputLastTicks = curTicks;

			if ( _trackSelectedObjects && _trackedObject != null && !_trackedObject.IsDead )
			{
				_viewState.ScreenX = ( float ) ( _trackedObject.Position.X - this.ClientSize.Width / _viewState.Scale / 2f );
				_viewState.ScreenY = ( float ) ( _trackedObject.Position.Y - this.ClientSize.Height / _viewState.Scale / 2f );
			}

			Graphics g = e.Graphics;
			g.Clear ( Color.FromArgb ( 10, 10, 20 ) );

			if ( _drawGrid )
			{
				const int step = 250;
				float minX = _viewState.ScreenX;
				float minY = _viewState.ScreenY;
				float maxX = minX + _viewState.Width / _viewState.Scale;
				float maxY = minY + _viewState.Height / _viewState.Scale;

				for ( var x = ( float ) Math.Ceiling ( minX / step ) * step; x < maxX; x += step )
					g.DrawLine ( _gridPen, ( x - _viewState.ScreenX ) * _viewState.Scale, 0, ( x - _viewState.ScreenX ) * _viewState.Scale, _viewState.Height );

				for ( var y = ( float ) Math.Ceiling ( minY / step ) * step; y < maxY; y += step )
					g.DrawLine ( _gridPen, 0, ( y - _viewState.ScreenY ) * _viewState.Scale, _viewState.Width, ( y - _viewState.ScreenY ) * _viewState.Scale );
			}

			try
			{
				if ( _showSOI )
					drawSOI ( g );

				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

				if ( _drawObjects )
				{
					int i = 0;
					while ( true )
					{
						IObject o;
						if ( i < _objects.Count )
							o = _objects[ i++ ];
						else
							break;
						if ( !o.IsDead )
							o.DrawTrail ( g, _viewState );
					}

					i = 0;
					while ( true )
					{
						IObject o;
						if ( i < _objects.Count )
							o = _objects[ i++ ];
						else
							break;
						if ( !o.IsDead )
							o.Draw ( g, _viewState );
					}

					for ( i = 0; i < _selectedObjects.Count; i++ )
						_selectedObjects[ i ].DrawSelection ( g, _viewState );
				}
			}
			catch ( Exception exception )
			{
				Console.WriteLine ( exception );
			}

			if ( _isMouseDown )
			{
				float x1 = ( _rectX1 - _viewState.ScreenX ) * _viewState.Scale;
				float y1 = ( _rectY1 - _viewState.ScreenY ) * _viewState.Scale;
				float x2 = _mouseX;
				float y2 = _mouseY;

				if ( x1 > x2 )
					x2 = x1 + ( x1 = x2 ) - x1;

				if ( y1 > y2 )
					y2 = y1 + ( y1 = y2 ) - y1;

				if ( x2 - x1 > 2 && y2 - y1 > 2 )
					g.DrawRectangle ( _rectPen, x1, y1, x2 - x1, y2 - y1 );
			}

			{
				float x = ( float ) ( _centerOfMass.X - _viewState.ScreenX ) * _viewState.Scale;
				float y = ( float ) ( _centerOfMass.Y - _viewState.ScreenY ) * _viewState.Scale;
				g.DrawLine ( _rectPen, x - 5, y, x + 5, y );
				g.DrawLine ( _rectPen, x, y - 5, x, y + 5 );
			}

			int h = 5;
			g.DrawString ( string.Format ( c_str_sim_time, ( _lastSimTime / 1000 ).ToString ( format13 ), _timeScale.ToString ( "0.000" ), _paused ? "PAUSED" : "rate: x" + _timeRate.ToString ( "0.000" ) ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_step_time, ( _paused ? "paused" : _stepTime.TotalMilliseconds.ToString ( format13 ) + " ms" ) ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_obj_count, _objects.Count ), _font, _brush, 5, h += _fh );
			//g.DrawString ( string.Format ( c_str_avg_infl, _averageInfluences.ToString ( format11 ) ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_tot_mass, calcTotalMass ().ToString ( format13 ) ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_total_e, _totalEnergy.ToString ( format11 ), _totalAngMomentum.ToString ( "0.0" ) ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_com, _centerOfMass ), _font, _brush, 5, h += _fh );
			g.DrawString ( string.Format ( c_str_com_vel, _comVelocity ), _font, _brush, 5, h + _fh );

			if ( _trackedObject != null )
			{
				h = 5;
				g.DrawString ( string.Format ( c_str_m, _trackedObject.Mass.ToString ( format12 ) ), _font, _brush, _viewState.Width - 100, h += _fh );
				g.DrawString ( string.Format ( c_str_sma, _trackedObjectInfo.SMA.ToString ( format12 ) ), _font, _brush, _viewState.Width - 100, h += _fh );
				g.DrawString ( string.Format ( c_str_ecc, _trackedObjectInfo.Ecc.ToString ( format14 ) ), _font, _brush, _viewState.Width - 100, h += _fh );
				g.DrawString ( string.Format ( c_str_ap, _trackedObjectInfo.Apoapsis.ToString ( format12 ) ), _font, _brush, _viewState.Width - 100, h += _fh );
				g.DrawString ( string.Format ( c_str_pe, _trackedObjectInfo.Periapsis.ToString ( format12 ) ), _font, _brush, _viewState.Width - 100, h += _fh );
				g.DrawString ( string.Format ( c_str_ta, _trackedObjectInfo.TrueAnomaly.ToString ( format12 ) ), _font, _brush, _viewState.Width - 100, h + _fh );
			}

			//Application.DoEvents ();
			//this.Invalidate ();
		}

		private void drawSOI ( Graphics g )
		{
			for ( int scrX = 0; scrX < _viewState.Width; scrX += 5 )
				for ( int scrY = 0; scrY < _viewState.Height; scrY += 5 )
				{
					double x = scrX / _viewState.Scale + _viewState.ScreenX;
					double y = scrY / _viewState.Scale + _viewState.ScreenY;
					var point = new HPV2 ( x, y );

					int influenceIdx = -1;
					double influenceAmount = -99999;

					for ( int i = _objects.Count - 1; i >= 0; i-- )
					{
						if ( _objects[ i ] == null )
							continue;

						double infl = _objects[ i ].GM / Utils.DistanceSq ( _objects[ i ].Position, point );
						if ( infl < influenceAmount )
							continue;
						influenceAmount = infl;
						influenceIdx = i;
					}

					if ( influenceIdx == 0 )
						continue;

					using ( var p = new Pen ( _inflColors[ ( influenceIdx % _inflColors.Length ) ] ) )
						g.DrawRectangle ( p, scrX, scrY, 1, 1 );

					Application.DoEvents ();
				}
		}

		#region Key Down/Up + Resize

		protected override void OnKeyDown ( KeyEventArgs e )
		{
			base.OnKeyDown ( e );

			if ( _input.ContainsKey ( e.KeyCode ) )
				_input[ e.KeyCode ] = true;

			if ( e.KeyCode == Keys.B )
			{
				using ( TextWriter tw = new StreamWriter ( "1.txt", false ) )
					for ( int i = 0; i < _objects.Count; i++ )
					{
						tw.WriteLine ( "Mass " + _objects[ i ].Mass + ", Radius " + _objects[ i ].Radius + ", Velocity " + ( _objects[ i ].Velocity.X + _objects[ i ].Velocity.Y ) + ", Acceleration " + ( _objects[ i ].Acceleration.X + _objects[ i ].Acceleration.Y ) );
					}
			}
		}

		protected override void OnKeyUp ( KeyEventArgs e )
		{
			base.OnKeyUp ( e );

			if ( _input.ContainsKey ( e.KeyCode ) )
				_input[ e.KeyCode ] = false;
		}

		protected override void OnResize ( EventArgs e )
		{
			base.OnResize ( e );

			if ( _viewState != null )
			{
				_viewState.Width = this.ClientRectangle.Width;
				_viewState.Height = this.ClientRectangle.Height;
			}
		}

		#endregion

		#region Mouse Events

		private bool _isMouseDown;
		private int _mouseX, _mouseY;
		private float _rectX1, _rectY1;

		protected override void OnMouseDown ( MouseEventArgs e )
		{
			base.OnMouseDown ( e );

			_isMouseDown = true;

			_rectX1 = e.X / _viewState.Scale + _viewState.ScreenX;
			_rectY1 = e.Y / _viewState.Scale + _viewState.ScreenY;
		}

		protected override void OnMouseUp ( MouseEventArgs e )
		{
			base.OnMouseUp ( e );

			_isMouseDown = false;

			float rectX2 = e.X / _viewState.Scale + _viewState.ScreenX;
			float rectY2 = e.Y / _viewState.Scale + _viewState.ScreenY;

			if ( _rectX1 > rectX2 )
				rectX2 = _rectX1 + ( _rectX1 = rectX2 ) - _rectX1;

			if ( _rectY1 > rectY2 )
				rectY2 = _rectY1 + ( _rectY1 = rectY2 ) - _rectY1;

			_selectedObjects.Clear ();
			_trackedObject = null;

			for ( int i = 0; i < _objects.Count; i++ )
			{
				IObject o = _objects[ i ];
				if ( o == null )
					continue;

				double ox = o.Position.X;
				double oy = o.Position.Y;

				if ( _rectX1 < ox && ox < rectX2 && _rectY1 < oy && oy < rectY2 )
					_selectedObjects.Add ( o );
			}
			if ( _selectedObjects.Count > 0 )
				_trackedObject = _selectedObjects[ 0 ];
		}

		protected override void OnMouseMove ( MouseEventArgs e )
		{
			base.OnMouseMove ( e );

			_mouseX = e.X;
			_mouseY = e.Y;
		}

		#endregion

		#region Windows Form Designer generated code

		/// <summary> Required designer variable. </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> Clean up any resources being used. </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose ( bool disposing )
		{
			_stopSimulation = true;
			_mainPhysicsThread.Join ( 5000 );

			/*foreach ( var are in _handleCache )
				are.Dispose ();*/

			if ( disposing && ( components != null ) )
			{
				components.Dispose ();
			}
			base.Dispose ( disposing );
		}

		/// <summary> Required method for Designer support - do not modify the contents of this method with the code editor. </summary>
		private void InitializeComponent ()
		{
			this.SuspendLayout ();
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF ( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size ( 1200, 900 );
			this.DoubleBuffered = true;
			this.Name = "Form1";
			this.Text = "Gravity Test";
			this.ResumeLayout ( false );
		}

		#endregion
	}
}
