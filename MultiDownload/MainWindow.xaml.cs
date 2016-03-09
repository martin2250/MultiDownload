using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;
using System.ComponentModel;

namespace MultiDownload
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string prop)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}

		static Regex rangeVal = new Regex("^(\\d+)\\s*-\\s*(\\d+)$");
		static Regex urlIns = new Regex("{(\\d+)}");

		FolderBrowserDialog fbd = new FolderBrowserDialog() { RootFolder = Environment.SpecialFolder.Desktop, ShowNewFolderButton = true};
		WebClient webc = new WebClient();

		int min = 0, max = 10;

		public string Range
		{
			get { return $"{min}-{max}"; }
			set
			{
				Match range = rangeVal.Match(value);

				if (!range.Success)
				{
					throw new ApplicationException("Invalid Range, Format: [min]-[max]");
				}

				min = int.Parse(range.Groups[1].Value);
				max = int.Parse(range.Groups[2].Value);
			}
		}

		string _path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		public string FilePath
		{
			get
			{
				return _path;
			}
			set
			{
				if (Directory.Exists(value))
				{
					_path = Path.GetFullPath(value);
					OnPropertyChanged("FilePath");
				}
				else
					throw new ApplicationException("Invalid Path");
			}
		}

		bool _canChange = true;
		public bool CanChange { get { return _canChange; } set { _canChange = value; OnPropertyChanged("CanChange"); } }

        public MainWindow()
		{
			InitializeComponent();
			webc.DownloadFileCompleted += Webc_DownloadFileCompleted;
			webc.DownloadProgressChanged += Webc_DownloadProgressChanged;
		}

		private void Webc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			progressBar.Value = progressBar.Maximum - ((ToDownload.Count + 1) * 100) + e.ProgressPercentage;
		}

		private void Webc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if(e.Error != null && !e.Cancelled)
			{
				System.Windows.MessageBox.Show("Error while downloading: " + e.Error.Message);

				CanChange = true;

				progressBar.Value = 0;

				buttonDownload.Content = "Download";
			}
			else if (ToDownload.Any())
			{
				DownloadEntry next = ToDownload.Dequeue();
				webc.DownloadFileAsync(new Uri(next.Url), next.Path);

				progressBar.Value = progressBar.Maximum - ((ToDownload.Count + 1) * 100);
            }
			else
			{
				System.Windows.MessageBox.Show("Done");

				CanChange = true;

				progressBar.Value = 0;

				buttonDownload.Content = "Download";
			}
		}

		private IEnumerable<DownloadEntry> GetDownloadEntries()
		{
			string path = FilePath;

			if (!path.EndsWith("\\"))
				path += "\\";

			for(int i = min; i <= max; i++)
			{
				yield return new DownloadEntry(
					urlIns.Replace(textBoxUrl.Text, (m)=> { return i.ToString($"D{int.Parse(m.Groups[1].Value)}"); }),
					urlIns.Replace(path + textBoxFileName.Text, (m) => { return i.ToString($"D{int.Parse(m.Groups[1].Value)}"); }));
			}
		}

		Queue<DownloadEntry> ToDownload = new Queue<DownloadEntry>();

		private void buttonDownload_Click(object sender, RoutedEventArgs e)
		{
			if (CanChange)
			{
				CanChange = false;

				foreach(var entry in GetDownloadEntries())
					ToDownload.Enqueue(entry);

				progressBar.Maximum = ToDownload.Count * 100;

				DownloadEntry first = ToDownload.Dequeue();

				webc.DownloadFileAsync(new Uri(first.Url), first.Path);

				buttonDownload.Content = "Cancel";
			}
			else
			{
				ToDownload.Clear();
				webc.CancelAsync();
				CanChange = true;

				progressBar.Value = 0;

				buttonDownload.Content = "Download";
			}
		}

		private void buttonDownload_ToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
		{
			List<DownloadEntry> examples = new List<DownloadEntry>(GetDownloadEntries());

			buttonDownload.ToolTip = string.Join("\n", examples.GetRange(0, Math.Min(5, examples.Count)).Select((entry)=> { return $"{entry.Url}\n=>{entry.Path}"; }));

			if (examples.Count > 5)
				buttonDownload.ToolTip += $"\n... {examples.Count - 5} lines hidden";
		}

		private void buttonBrowse_Click(object sender, RoutedEventArgs e)
		{
			fbd.SelectedPath = FilePath;

			fbd.ShowDialog();

			FilePath = fbd.SelectedPath;
        }
	}

	struct DownloadEntry
	{
		public string Url;
		public string Path;

		public DownloadEntry(string url, string path)
		{
			Url = url;
			Path = path;
		}
	}
}
