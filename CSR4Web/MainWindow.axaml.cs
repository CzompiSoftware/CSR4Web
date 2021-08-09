using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace CSR4Web
{
	public partial class MainWindow : Window
	{
		public IAssetLoader Assets { get; }
		public TextBox Domain { get; }
		public TextBox Editor { get; }
		public TextBox DefaultBits { get; }
		public Label TitleLabel { get; }
		public Image StatusImage { get; }

		public MainWindow()
		{
			InitializeComponent();
			Assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

			Domain = this.FindControl<TextBox>("Domain");
			Editor = this.FindControl<TextBox>("Editor");
			DefaultBits = this.FindControl<TextBox>("DefaultBits");
			TitleLabel = this.FindControl<Label>("TitleLabel");
			StatusImage = this.FindControl<Image>("StatusImage");

			TitleLabel.Content = Title;
			Editor.Text = new StreamReader(Assets.Open(new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Resources/ssl.conf"))).ReadToEnd();

#if DEBUG
			this.AttachDevTools();
#endif
		}

		public void OnTitlebarPointerMoved(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);
		private void GenerateCertificateSigningRequest()
		{
			BackgroundWorker bw = new();
			bw.DoWork += delegate
			{
				try
				{
					string path = $"{Path.GetFullPath("certs")}{Path.DirectorySeparatorChar}";
					Directory.CreateDirectory(path);
					File.WriteAllText($"{path}{Domain.Text}-ssl.conf", Editor.Text.Replace("{{Domain}}", Domain.Text, StringComparison.OrdinalIgnoreCase).Replace("{{DefaultBits}}", DefaultBits.Text, StringComparison.OrdinalIgnoreCase));
					// Generate Private key
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine("lib", "openssl", "openssl.exe"),
						Arguments = $"genrsa -out {path}{Domain.Text}-private.key {DefaultBits.Text}",
						CreateNoWindow = true
					}).WaitForExit();
					// Generate CSR
					Process.Start(new ProcessStartInfo
					{
						FileName = Path.Combine("lib", "openssl", "openssl.exe"),
						Arguments = $"req -new -sha256 -out {path}{Domain.Text}-private.csr -key {path}{Domain.Text}-private.key -config {path}{Domain.Text}-ssl.conf",
						//CreateNoWindow = true

					}).WaitForExit();
					Debug.WriteLine($"req -new -sha256 -out {path}{Domain.Text}-private.csr -key {path}{Domain.Text}-private.key -config {path}{Domain.Text}-ssl.conf");
					Dispatcher.UIThread.InvokeAsync(() =>
					{
						StatusImage.IsVisible = true;
						StatusImage.Source = new Bitmap(Assets.Open(new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Resources/success.png")));
					});
				}
				catch (Exception ex)
				{
					Dispatcher.UIThread.InvokeAsync(() =>
					{
						StatusImage.IsVisible = true;
						StatusImage.Source = new Bitmap(Assets.Open(new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Resources/fail.png")));
					});
					Debug.WriteLine(ex);
				}
				Thread.Sleep(3 * 1000);
				Dispatcher.UIThread.InvokeAsync(() =>
				{
					StatusImage.IsVisible = false;
				});
			};
			bw.RunWorkerAsync();
		}
		public void OnGenerateCSRClick(object sender, RoutedEventArgs e)
		{

			GenerateCertificateSigningRequest();
		}
		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
