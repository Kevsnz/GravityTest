namespace GravityTest
{
	public class ViewState
	{
		private float _screenX;
		private float _screenY;
		private float _scale;
		private float _width;
		private float _height;
		private bool _drawTrails;

		public float ScreenX { get { return _screenX; } set { _screenX = value; } }
		public float ScreenY { get { return _screenY; } set { _screenY = value; } }
		public float Scale { get { return _scale; } set { _scale = value; } }
		public float Width { get { return _width; } set { _width = value; } }
		public float Height { get { return _height; } set { _height = value; } }
		public bool DrawTrails { get { return _drawTrails; } set { _drawTrails = value; } }

		public ViewState ( float w, float h )
		{
			_screenX = 0;
			_screenY = 0;
			_scale = 1;
			_width = w;
			_height = h;
			_drawTrails = false;
		}
	}
}
