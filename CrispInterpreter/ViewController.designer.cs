// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace CrispInterpreter
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		UIKit.UITextView CodeOutput { get; set; }

		[Outlet]
		UIKit.UITextView CodeView { get; set; }

		[Outlet]
		UIKit.UIButton LoadFileButton { get; set; }

		[Outlet]
		UIKit.UIButton NewFileButton { get; set; }

		[Outlet]
		UIKit.UIButton RunButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CodeOutput != null) {
				CodeOutput.Dispose ();
				CodeOutput = null;
			}

			if (CodeView != null) {
				CodeView.Dispose ();
				CodeView = null;
			}

			if (NewFileButton != null) {
				NewFileButton.Dispose ();
				NewFileButton = null;
			}

			if (RunButton != null) {
				RunButton.Dispose ();
				RunButton = null;
			}

			if (LoadFileButton != null) {
				LoadFileButton.Dispose ();
				LoadFileButton = null;
			}
		}
	}
}
