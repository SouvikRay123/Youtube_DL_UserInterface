using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Youtube_DL_Helper
{
    public class DownloadHelper
    {
        #region Private Variables

        private List<Action> _threadPool = new List<Action>();
        private ConcurrentBag<StringBuilder> _logs = new ConcurrentBag<StringBuilder>();                
        public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource(); 
        
        public string CurrentDirectory {
            private get; set;
        }

        private short _numberOfParallelDownloads = 1;
        public short NumberOfParallelDownloads
        {
            private get { return this._numberOfParallelDownloads; }
            set { this._numberOfParallelDownloads = value; }
        }
        
        public bool IsMultithreadingDownloadEnabled
        {
            private get; set;
        }
        
        public string ProcessLocation
        {
            private get; set;
        }
        
        public bool HideProcessWindow
        {
            private get; set;
        }

        #endregion

        public DownloadHelper(string currentDirectory, string processLocation, short noOfParallelDownloads, bool isMultiThreaded , bool hideProcessWindow)
        {
            this.CurrentDirectory = currentDirectory;
            this.ProcessLocation = processLocation;
            this.NumberOfParallelDownloads = noOfParallelDownloads;
            this.IsMultithreadingDownloadEnabled = isMultiThreaded;
            this.HideProcessWindow = hideProcessWindow;
        }

        
        public List<Action> GetThreadPool()
        {
            return this._threadPool;
        }

        public void GetCurrentVersion(string processLocation)
        {
            string arguements = "--version";
            PrepareThreadPool(arguements, DownloadHelperConstants.currentVersionDownload);
            StartDownloads(null, _threadPool);
        }

        public string[] GetListOfVideos(string input)
        {
            return input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public void CreateDirectoryIfNotExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void PrepareThreadPool(string arguements, string downloadType)
        {
            try
            {
                Process myProcess = CreateProcess(arguements, downloadType);

                _threadPool.Add(new Action(delegate ()
                {
                    StartProcessAndWriteStatus(myProcess, downloadType);
                }));

                #region Commented Codes

                //if (isMultiThreadEnabled)
                //{
                //    //if (_numberOfParallelDownloads > 0)
                //    //{
                //    //_numberOfParallelDownloads--;
                //    //Parallel.For(0, 10, new ParallelOptions { MaxDegreeOfParallelism = 4 }, count =>
                //    //{
                //    //    StartProcessAndWriteStatus(myProcess, folderName);
                //    //});
                //    _threadPool.Add(new Action(delegate ()
                //    {
                //        StartProcessAndWriteStatus(myProcess, folderName);
                //    }));
                //        //StartThread(new Action(delegate ()
                //        //{
                //        //    StartProcessAndWriteStatus(myProcess, folderName);
                //        //}));
                //    //}
                //    //else
                //    //{
                //    //    while(_numberOfParallelDownloads <= 0)
                //    //        Thread.Sleep(2000);
                //    //    StartThread(new Action(delegate ()
                //    //    {
                //    //        StartProcessAndWriteStatus(myProcess, folderName);
                //    //    }));
                //    //}
                //}
                //else
                //{
                //    StartProcessAndWriteStatus(myProcess, folderName);
                //}

                #endregion
            }
            catch (Exception ex)
            {
                StartDownloads(new Action(delegate ()
                {
                    WriteOutputToFile("Error", ex.Message);
                }), null);
            }
        }

        public void StartDownloads(Action action, List<Action> actions)
        {
            if (action != null)
            {
                Parallel.For(0, 1, new ParallelOptions
                {
                    CancellationToken = CancellationTokenSource.Token,
                    MaxDegreeOfParallelism = 1
                } , task => action.Invoke());
            }
            else
            {
                if (!IsMultithreadingDownloadEnabled)
                    NumberOfParallelDownloads = 1;
                StartParallelTasks(actions);
            }

            string consolidatedLog = GetConsolidatedLogs();

            WriteOutputToFile(DownloadHelperConstants.logFileType, consolidatedLog);
        }

        public string GetConsolidatedLogs()
        {
            string consolidatedLog = "";
            foreach (StringBuilder log in _logs)
            {
                consolidatedLog += log + "\n\n";
            }

            return consolidatedLog;
        }

        private Process CreateProcess(string arguements, string downloadType)
        {
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = ProcessLocation;
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.Arguments = arguements;
            myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            if(HideProcessWindow)
                myProcess.StartInfo.CreateNoWindow = HideProcessWindow;

            return myProcess;
        }

        private void StartParallelTasks(List<Action> actions)
        {
            ParallelOptions po = new ParallelOptions
            {
                CancellationToken = CancellationTokenSource.Token,
                MaxDegreeOfParallelism = NumberOfParallelDownloads
            };

            try
            {
                Parallel.ForEach(actions, po, task =>
                {
                    task.Invoke();
                    //po.CancellationToken.ThrowIfCancellationRequested();
                });
            }
            catch (Exception ex)
            {
                _logs.Add(new StringBuilder("Download Was Cancelled"));
            }
        }

        private void ClearLogs()
        {
            _logs = new ConcurrentBag<StringBuilder>();
        }

        public void StopDownloads()
        {
            CancellationTokenSource.Cancel();            
        }

        public void StartProcessAndWriteStatus(Process myProcess, string downloadType)
        {
            var threadId = Thread.CurrentThread.ManagedThreadId.ToString();

            StringBuilder redirectedOutput = new StringBuilder("[" + System.DateTime.Now.ToString() + "] : Started Downloading in thread : " + threadId + "\n");

            myProcess.Start();

            myProcess.WaitForExit();

            redirectedOutput.Append(myProcess.StandardOutput.ReadToEnd());

            _logs.Add(redirectedOutput);
        }

        public string DownloadUpdate()
        {
            string arguements = "--update";

            Process downloadUpdateProcess = CreateProcess(arguements , DownloadHelperConstants.latestVersionDownload);
            StartProcessAndWriteStatus(downloadUpdateProcess, DownloadHelperConstants.latestVersionDownload);

            string output = "";
            foreach (StringBuilder log in _logs)
                output += log;            

            string[] outputLines = output.Split('\n');
            return outputLines[outputLines.Length - 2];
        }

        public void WriteOutputToFile(string downloadType, string content)
        {
            try
            {
                CreateDirectoryIfNotExists(downloadType);
                string path = downloadType + "\\" +
                    //folderName + "_" + 
                    System.DateTime.Now.ToString() + "_Log.txt";
                path = Regex.Replace(path, "[ :/-]", "_"); // Replace timestamps and "-" in date

                using (var sw = new StreamWriter(path))
                {
                    sw.WriteLine(content);
                }
                ClearLogs();
            }
            catch (Exception ex)
            {

            }
        }

        public string GetOutputDirectoryArguement(string directory, string subFolder)
        {
            string directoryPath = "\"" + directory + @"\" + subFolder + @"\" + Regex.Replace(System.DateTime.Now.ToString(), "[ :/-]", "_");
            if (subFolder == DownloadHelperConstants.playlistDownload)
                return (directoryPath + @"\%(playlist)s\%(title)s.%(ext)s""");

            if (subFolder == DownloadHelperConstants.channelDownload)
                return (directoryPath + @"\%(uploader)s\%(playlist)s\%(title)s.%(ext)s""");

            if (subFolder == DownloadHelperConstants.videoDownload)
                return (directoryPath + @"\%(title)s.%(ext)s""");

            return (directoryPath + @"\%(title)s.%(ext)s""");
        }
    }
}