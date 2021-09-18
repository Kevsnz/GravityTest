using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GravityTest
{
	class Motec
	{
		private readonly Dictionary<string, List<ValueType>> _channels;

		public List<ValueType> this[ string name ]
		{
			get
			{
				if ( _channels.ContainsKey ( name ) )
					return _channels[ name ];
				return null;
			}
		}

		public Motec ()
		{
			_channels = new Dictionary<string, List<ValueType>> ();
		}

		public Motec ( string[] names )
		{
			_channels = new Dictionary<string, List<ValueType>> ();
			foreach ( string name in names )
				_channels.Add ( name, new List<ValueType> () );
		}

		public void AddChannel ( string name )
		{
			if ( _channels.ContainsKey ( name ) )
				throw new ArgumentException ( "Channel with name '" + name + "' already exists.", "name" );

			_channels.Add ( name, new List<ValueType> () );
		}

		public void AddData ( string name, ValueType data )
		{
			if ( _channels.ContainsKey ( name ) )
				_channels[ name ].Add ( data );
			else
				throw new ArgumentException ( "Unknown channel name", "name" );
		}

		public void AddDataRow ( string[] names, ValueType[] data )
		{
			if ( names.Length != data.Length )
				throw new ArgumentException ( "Names and data arrays have different lengths." );

			for ( int i = 0; i < names.Length; i++ )
			{
				if ( _channels.ContainsKey ( names[ i ] ) )
					_channels[ names[ i ] ].Add ( data[ i ] );
				else
					throw new ArgumentException ( "Unknown channel name '" + names[ i ] + "'", "names" );
			}
		}

		public void Clear ()
		{
			_channels.Clear ();
		}

		public void ClearData ()
		{
			foreach ( var kvp in _channels )
				kvp.Value.Clear ();
		}

		public void RemoveChannel ( string name )
		{
			if ( _channels.ContainsKey ( name ) )
				_channels.Remove ( name );
			else
				throw new ArgumentException ( "Unknown channel name '" + name + "'", "name" );
		}

		public void WriteOnDisk ( string filename )
		{
			using ( TextWriter tw = new StreamWriter ( filename ) )
			{
				int max = _channels.Max ( kvp => kvp.Value.Count );

				foreach ( string name in _channels.Keys )
					tw.Write ( name + "\t" );
				tw.WriteLine ();

				for ( int i = 0; i < max; i++ )
				{
					foreach ( var list in _channels.Values )
						tw.Write ( list[ i ] + "\t" );
					tw.WriteLine ();
				}
			}
		}

		public int MaxData { get { return _channels.Max ( kvp => kvp.Value.Count ); } }
	}
}
