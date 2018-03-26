using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Youtube_DL_Helper;

namespace Youtube_DL_UserInterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DownloadHelper _downloadHelper;
        private string _currentDirectory = "";
            
        public MainWindow()
        {
            InitializeComponent();
            _currentDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);            
            
            txtYoutubeDLLocation.Text = _currentDirectory + "\\" + "youtube-dl.exe";
            if (!IsYoutubeDownloaderConsolePresent(txtYoutubeDLLocation.Text))
            {
                MessageBox.Show("Youtube Downloader Executable Not Found !!\nPlease Provide the location in the input box");
            }
        }


        private void radioMultiThreaded_Checked(object sender, RoutedEventArgs e)
        {
            txtNumberParallelDownloads.IsEnabled = true;
        }

        private void radioMultiThreaded_UnChecked(object sender, RoutedEventArgs e)
        {
            txtNumberParallelDownloads.IsEnabled = false;
        }


        private void btnChooseYoutubeDLLocation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = _currentDirectory;
            openFileDialog.Filter = "Exe files (*.exe)|*.exe";
            if (openFileDialog.ShowDialog() == true)
                txtYoutubeDLLocation.Text = openFileDialog.FileName;
        }

        private void btnDownloadUpdates_Click(object sender, RoutedEventArgs e)
        {
            SetDownloadHelperObject();
            lblLatestVersion.Content = _downloadHelper.DownloadUpdate();
            //_downloadHelper = null;
        }

        private void btnDownloadLatest_Click(object sender, RoutedEventArgs e)
        {
            SetDownloadHelperObject();
            _downloadHelper.DownloadUpdate();
            //_downloadHelper = null;
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (!IsYoutubeDownloaderConsolePresent(txtYoutubeDLLocation.Text))
            {
                MessageBox.Show("Youtube Downloader Executable Not Found !!\nPlease Provide the location in the input box");
                return;
            }
            
            if (string.IsNullOrEmpty(txtVideoList.Text))
            {
                MessageBox.Show("Video List is Empty !!");
                return;
            }

            SetDownloadHelperObject();

            string[] videoList = _downloadHelper.GetListOfVideos(txtVideoList.Text.Replace("\t", ""));

            int numberOfVideos = videoList.Length;
            for (int i = 0; i < numberOfVideos; i++)
            {
                string arguements = "", folder = "", videoURL = videoList[i];
                //int index = 0;

                if (string.IsNullOrEmpty(videoURL))
                {
                    continue;
                }
                else if (videoURL.Contains("watch?v="))
                {
                    videoURL = Regex.Replace(videoURL, "&list=.+", ""); // enable downloading only video
                    folder = DownloadHelperConstants.videoDownload;
                    //index = ++_videoIndex;
                }
                else if (videoURL.Contains("list="))
                {
                    folder = DownloadHelperConstants.playlistDownload;
                    //index = ++_listIndex;
                }
                else if (videoURL.Contains("/channel/"))
                {
                    folder = DownloadHelperConstants.channelDownload;
                    //index = ++_channelIndex;
                }

                arguements = @"-o " + _downloadHelper.GetOutputDirectoryArguement(_currentDirectory, folder) + " " + videoURL;
                _downloadHelper.PrepareThreadPool(arguements, folder, true);
            }

            _downloadHelper.StartDownloads(null, _downloadHelper.GetThreadPool());
            //_downloadHelper = null;
        }


        public bool IsYoutubeDownloaderConsolePresent(string location)
        {
            if (File.Exists(location))
            {
                return true;
            }
            return false;
        }

        public void SetDownloadHelperObject()
        {
            bool isMultiThreadEnabled = radioMultiThreaded.IsChecked ?? false;
            short numberOfParallelDownloads = 1;
            Int16.TryParse(txtNumberParallelDownloads.Text, out numberOfParallelDownloads);

            if (_downloadHelper != null)
            {
                _downloadHelper.SetCurrentDirectory(_currentDirectory);
                _downloadHelper.SetProcessLocation(txtYoutubeDLLocation.Text);
                _downloadHelper.SetNumberOfParallelDownloads(numberOfParallelDownloads);
                _downloadHelper.SetIsMultiThreaded(isMultiThreadEnabled);
            }
            else
                _downloadHelper = new DownloadHelper(_currentDirectory , txtYoutubeDLLocation.Text , numberOfParallelDownloads, isMultiThreadEnabled);
        }
    }
}