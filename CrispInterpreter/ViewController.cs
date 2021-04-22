using Foundation;
using System;
using System.IO;
using System.Collections.Generic;
using UIKit;

using Xamarin.Essentials;

namespace CrispInterpreter
{
    public partial class ViewController : UIViewController
    {
        private int Padding
        {
            get
            {
                return 5;
            }
        }

        public ViewController(IntPtr handle) : base(handle)
        {
            
        }

        private int ButtonPadding
        {
            get
            {
                return 2;
            }
        }
        
        private UIImage ResizeImage(String pathName,in int w, int h)
        {
            UIImage img = UIImage.FromFile(pathName);
            if (w == img.Size.Width && h == img.Size.Height)
            {
                return img;
            }
            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(w, h));
            img.Draw(new CoreGraphics.CGRect(0, 0, w, h));
            UIImage result = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return result;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var screenSize = UIScreen.MainScreen.Bounds;
            var width = (screenSize.Width - (RunButton.Frame.Left * 2)) / 3;

            var borderColor = new CoreGraphics.CGColor(0.6f, 0.89f, 0.313f);
            var backgroundColor = new UIColor(0.21f, 0.21f, 0.227f, 1.0f);

            View.BackgroundColor = backgroundColor;

            CodeView.Text = "(define x 3)\n\n";
            CodeView.Text += "(define-func add-x (y)\n    (+ x y))\n\n";
            CodeView.Text += "(print-line (add-x 3))\n\n";
            CodeView.Text += "(define-func fold-func (acc value)\n    (+ acc value))\n\n";
            CodeView.Text += "(print-line (foldl fold-func 0 (1 2 3 4 5)))\n";
            CodeView.Text += "(print-line (foldr fold-func 0 (1 2 3 4 5)))\n\n";
            CodeView.Text += "(define-func print-list (ls)\n";
            CodeView.Text += "    (if (not (empty? ls))\n";
            CodeView.Text += "        (let ((x (car ls)))\n";
            CodeView.Text += "            (print-line x)\n";
            CodeView.Text += "            (print-list (cdr ls)))\n";
            CodeView.Text += "        (print-line \"done\")))\n\n";
            CodeView.Text += "(print-list (quote (1 2 3 4 5)))\n\n";
            CodeView.Text += "(print-line (map (+ 1) (1 2 3 4 5)))\n";
            CodeView.Text += "(print-line (map (+ 1 2) (1 2 3 4 5)))\n\n";
            CodeView.Text += "(help)\n";


            // ###############################################
            // Run Button
            // ###############################################
            RunButton.BackgroundColor = backgroundColor;

            RunButton.Frame = new CoreGraphics.CGRect(
                RunButton.Frame.X,
                RunButton.Frame.Y,
                width - Padding,
                RunButton.Frame.Height + ButtonPadding * 2);

            UIImage runImage = ResizeImage("Images/runbutton.png", 16, 16);
            RunButton.SetImage(runImage, UIControlState.Normal);
            RunButton.TitleEdgeInsets = new UIEdgeInsets(0, -RunButton.ImageView.Frame.Size.Width, 0, RunButton.ImageView.Frame.Size.Width);
            RunButton.ImageEdgeInsets = new UIEdgeInsets(0, RunButton.TitleLabel.Frame.Size.Width + Padding, 0, -RunButton.TitleLabel.Frame.Size.Width);

            RunButton.Layer.CornerRadius = 10;
            RunButton.Layer.BorderWidth = 2;
            RunButton.Layer.BorderColor = borderColor;

            RunButton.TouchUpInside += (sender, e) => {
                View.EndEditing(true);
                Crisp.Environment env = new Crisp.Environment(null);
                Crisp.Parser p = new Crisp.Parser();
                MemoryStream ms = new MemoryStream();
                    
                try
                {
                    var t = Crisp.Evaluator.Evaluate(ref env, ms, p.Parse(CodeView.Text));
                }
                catch (Crisp.CrispArgumentException exc)
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.WriteLine();
                    sw.WriteLine(String.Format("Argument Exception (Line {0}):", exc.Line));
                    sw.WriteLine(exc.Message);
                    sw.Flush();
                }
                catch (Crisp.CrispParserExpcetion exc)
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.WriteLine();
                    sw.WriteLine(String.Format("Parser Exception (Line {0}):", exc.Line));
                    sw.WriteLine(exc.Message);
                    sw.Flush();
                }
                catch (Crisp.CrispNotExistsException exc)
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.WriteLine();
                    sw.WriteLine(String.Format("Identifer Exception (Line {0})", exc.Line));
                    sw.WriteLine(exc.Message);
                    sw.Flush();
                }
                catch (Crisp.CrispException exc)
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.WriteLine();
                    sw.WriteLine(String.Format("Exception (Line {0}):", exc.Line));
                    sw.WriteLine(exc.Message);
                    sw.Flush();
                }
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                CodeOutput.Text = sr.ReadToEnd();
                sr.Close();
            };

            // ###############################################
            // Load File Button
            // ###############################################
            LoadFileButton.BackgroundColor = backgroundColor;
            LoadFileButton.SetTitleColor(new UIColor(1.0f, 1.0f, 1.0f, 1.0f), UIControlState.Normal);
            LoadFileButton.Frame = new CoreGraphics.CGRect(
                RunButton.Frame.Right + Padding,
                RunButton.Frame.Y,
                width - Padding,
                RunButton.Frame.Height);

            LoadFileButton.TouchUpInside += async (sender, e) =>
            {
                var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<String>>
                {
                    { DevicePlatform.iOS, new[] { "com.crisp.extension" } },
                });
                var options = new PickOptions
                {
                    PickerTitle = "Please select a Crisp file",
                    FileTypes = customFileType
                };
                var result = await FilePicker.PickAsync(options);
                if (result != null)
                {
                    var stream = await result.OpenReadAsync();
                    StreamReader r = new StreamReader(stream);
                    CodeView.Text = r.ReadToEnd();
                }
            };

            LoadFileButton.Layer.CornerRadius = 10;
            LoadFileButton.Layer.BorderWidth = 2;
            LoadFileButton.Layer.BorderColor = borderColor;

            // ###############################################
            // New File Button
            // ###############################################
            NewFileButton.BackgroundColor = backgroundColor;
            NewFileButton.Frame = new CoreGraphics.CGRect(
                NewFileButton.Frame.Right - width + Padding,
                RunButton.Frame.Y,
                width - Padding,
                RunButton.Frame.Height);

            NewFileButton.TouchUpInside += async (sender, e) =>
            {
                if (CodeView.Text.Length > 0)
                {
                    String documents = Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments);
                    String filename = String.Format("{0}_{1}.crp",
                        DateTime.Now.ToShortDateString().Replace('/', '_'),
                        DateTime.Now.ToShortTimeString().Replace(':', '_'));
                    String pathAndFile = Path.Combine(documents, filename);
                    File.WriteAllText(pathAndFile, CodeView.Text);

                    CodeOutput.Text = String.Format("Wrote file '{0}' to documents.", filename);

                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = Title,
                        File = new ShareFile(pathAndFile)
                    });
                }
            };

            NewFileButton.Layer.CornerRadius = 10;
            NewFileButton.Layer.BorderWidth = 2;
            NewFileButton.Layer.BorderColor = borderColor;

            UIImage newImage = ResizeImage("Images/newfilebutton.png", 16, 16);

            NewFileButton.SetImage(newImage, UIControlState.Normal);
            NewFileButton.TitleEdgeInsets = new UIEdgeInsets(0, -NewFileButton.ImageView.Frame.Size.Width, 0, NewFileButton.ImageView.Frame.Size.Width);
            NewFileButton.ImageEdgeInsets = new UIEdgeInsets(0, NewFileButton.TitleLabel.Frame.Size.Width + Padding, 0, -NewFileButton.TitleLabel.Frame.Size.Width);

            // ###############################################
            // Code View
            // ###############################################
            var cvWidth = screenSize.Width - (CodeView.Frame.X * 2);
            var cvHeight = (screenSize.Height - (RunButton.Frame.Top * 2)) * 0.4f;

            CodeView.BackgroundColor = backgroundColor;

            CodeView.Frame = new CoreGraphics.CGRect(
                CodeView.Frame.X,
                RunButton.Frame.Bottom + Padding,
                cvWidth,
                cvHeight);

            CodeView.Layer.CornerRadius = 10;
            CodeView.Layer.BorderWidth = 2;
            CodeView.Layer.BorderColor = new CoreGraphics.CGColor(0.6f, 0.89f, 0.313f);

            // ###############################################
            // Code Output
            // ###############################################
            var outX = CodeView.Frame.X;
            var outY = CodeView.Frame.Bottom + Padding;
            var outWidth = screenSize.Width - (CodeOutput.Frame.X * 2);
            var outHeight = screenSize.Height - CodeView.Frame.Bottom - Padding - RunButton.Frame.Top;

            CodeOutput.BackgroundColor = backgroundColor;

            CodeOutput.Frame = new CoreGraphics.CGRect(outX, outY, outWidth, outHeight);

            CodeOutput.Layer.CornerRadius = 10;
            CodeOutput.Layer.BorderWidth = 2;
            CodeOutput.Layer.BorderColor = borderColor;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}