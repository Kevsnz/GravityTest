using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace GravityTest
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		internal static void Main ()
		{
			AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_OnFirstChanceException;
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault ( false );

			Application.Run ( new Form1 () );
		}

		static void CurrentDomain_OnFirstChanceException ( object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e )
		{
			Trace.WriteLine ( e.Exception );
		}
	}
}
