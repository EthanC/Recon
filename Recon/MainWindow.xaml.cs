using AdonisUI.Controls;
using CascLib.NET;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Recon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public ObservableCollection<CASCFile> Files { get; } = new ObservableCollection<CASCFile>();

        CascStorage Storage { get; set; }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();

            ((CollectionView)CollectionViewSource.GetDefaultView(FileList.ItemsSource)).Filter = FileSearch_Filter;

            SetUIIdleUnloaded();
        }

        private void SetUIWorking(string message = "Working", int progress = 100)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MenuFileOpen.IsEnabled = false;
                //MenuFileFilter.IsEnabled = false;
                MenuFileExport.IsEnabled = false;
                MenuFileRegister.IsEnabled = false;
                MenuFileClose.IsEnabled = false;

                FileSearch.IsEnabled = false;
                FileList.IsEnabled = false;

                StatusProgress.Value = progress;
                StatusText.Text = message;
            }));
        }

        private void SetUIIdleUnloaded(string message = "Idle", int progress = 0)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MenuFileOpen.IsEnabled = true;
                //MenuFileFilter.IsEnabled = false;
                MenuFileExport.IsEnabled = false;
                MenuFileRegister.IsEnabled = false;
                MenuFileClose.IsEnabled = false;

                CASCPath.Text = null;
                FileSearch.IsEnabled = false;
                FileList.IsEnabled = false;

                StatusProgress.Value = progress;
                StatusText.Text = message;
            }));
        }

        private void SetUIIdleLoaded(string message = "Idle", int progress = 0)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                MenuFileOpen.IsEnabled = false;
                //MenuFileFilter.IsEnabled = true;
                MenuFileExport.IsEnabled = true;
                MenuFileRegister.IsEnabled = true;
                MenuFileClose.IsEnabled = true;

                FileSearch.IsEnabled = true;
                FileList.IsEnabled = true;

                StatusProgress.Value = progress;
                StatusText.Text = message;
            }));
        }

        private void SetUICASCPath(string subtitle = null)
        {
            CASCPath.Text = subtitle;
        }

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog
            {
                Description = "Open CASC Folder",
                UseDescriptionForTitle = true
            };

            SetUIWorking("Selecting CASC Folder");

            if (dialog.ShowDialog() == true)
            {
                string result = dialog.SelectedPath;

                SetUIWorking($"Opening CASC {result}");

                try
                {
                    Storage = new CascStorage(result);

                    foreach (CascFileInfo file in Storage.Files)
                    {
                        int idx = file.FileName.LastIndexOf(".");
                        string fileType = "File";

                        if (idx != -1)
                        {
                            fileType = file.FileName[(idx + 1)..].ToUpper();
                        }

                        Files.Add(new CASCFile(file.FileName, fileType, GetBytesReadable(file.FileSize), file.FileSize, file.IsLocal));
                    }

                    SetUICASCPath(result);
                    SetUIIdleLoaded();
                }
                catch (Exception ex)
                {
                    MenuFileClose_Click(null, null);
                    SetUIIdleUnloaded($"Failed to open CASC, {ex.Message}");
                }
            }
            else
            {
                SetUIIdleUnloaded();
            }
        }

        private void MenuFileFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterWindow filter = new FilterWindow();
            filter.ShowDialog();
        }

        private void MenuFileExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportCASCFiles(FileList.Items.Cast<CASCFile>().ToList());
        }

        private void MenuFileExportSelected_Click(object sender, RoutedEventArgs e)
        {
            ExportCASCFiles(FileList.SelectedItems.Cast<CASCFile>().ToList());
        }

        private void MenuFileRegisterAll_Click(object sender, RoutedEventArgs e)
        {
            List<CASCFile> files = FileList.Items.Cast<CASCFile>().ToList();
            Dictionary<string, long> register = new Dictionary<string, long>();

            new Thread(() =>
            {
                foreach (CASCFile file in files)
                {
                    register.Add(file.FilePath, file.FileSizeBytes);
                }
            }).Start();

            ExportRegisterFile(register, "CASC.json");
        }

        private void MenuFileClose_Click(object sender, RoutedEventArgs e)
        {
            SetUIWorking("Closing CASC");

            Files.Clear();
            Storage?.Dispose();
            GC.Collect();

            SetUIIdleUnloaded();
        }

        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            MenuFileClose_Click(null, null);
            Application.Current.Shutdown();
        }

        private void MenuHelpUpdate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/EthanC/Recon/releases") { UseShellExecute = true });
        }

        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/EthanC/Recon") { UseShellExecute = true });
        }

        private void FileSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(FileList.ItemsSource).Refresh();
        }

        private bool FileSearch_Filter(object file)
        {
            if (string.IsNullOrEmpty(FileSearch.Text))
            {
                return true;
            }
            else
            {
                return (file as CASCFile).FilePath.Contains(FileSearch.Text, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void FileList_Resize(object sender, SizeChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            GridView gView = listView.View as GridView;

            double workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            double columnPath = 0.75;
            double columnType = 0.125;
            double columnSize = 0.125;

            gView.Columns[0].Width = workingWidth * columnPath;
            gView.Columns[1].Width = workingWidth * columnType;
            gView.Columns[2].Width = workingWidth * columnSize;
        }

        private void FileList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            object selected = ((FrameworkElement)e.OriginalSource).DataContext;

            if (selected is not CASCFile file)
            {
                return;
            }

            List<CASCFile> files = new List<CASCFile>
            {
                file
            };

            ExportCASCFiles(files);
        }

        private void FileList_RightClick(object sender, MouseEventArgs e)
        {
            // TODO
        }

        private void ExportCASCFiles(List<CASCFile> files)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog
            {
                Description = "Choose Export Folder",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true)
            {
                new Thread(() =>
                {
                    byte[] buffer = new byte[0x800000];

                    foreach (CASCFile file in files)
                    {
                        if (file.FileSizeBytes <= 0)
                        {
                            continue;
                        }
                        //else if (file.FileLocal == false)
                        //{
                        //    continue;
                        //}

                        string filePath = Path.Combine(dialog.SelectedPath, file.FilePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        try
                        {
                            using CascFileStream input = Storage.OpenFile(file.FilePath);
                            using FileStream output = File.Create(filePath);

                            while (true)
                            {
                                int bytesRead = input.Read(buffer, 0, 0x800000);

                                output.Write(buffer, 0, bytesRead);

                                if (bytesRead < 0x800000)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            SetUIWorking(e.Message);
                        }
                    }
                }).Start();
            }
        }

        private void ExportRegisterFile(Dictionary<string, long> register, string defaultFileName)
        {
            VistaSaveFileDialog dialog = new VistaSaveFileDialog()
            {
                FileName = defaultFileName,
                Filter = "JSON (*.json)|*.json|All Files (*.*)|*"
            };

            SetUIWorking("Selecting CASC file register folder");

            if (dialog.ShowDialog() is true)
            {
                SetUIWorking("Saving CASC file register");

                using StreamWriter registerFile = File.CreateText(dialog.FileName);
                registerFile.Write(JsonConvert.SerializeObject(register, Formatting.Indented));
            }

            SetUIIdleLoaded();
        }

        public static string GetBytesReadable(long i)
        {
            long absoluteI = i < 0 ? -i : i;

            string suffix;
            double readable;

            if (absoluteI >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = i >> 50;
            }
            else if (absoluteI >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = i >> 40;
            }
            else if (absoluteI >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = i >> 30;
            }
            else if (absoluteI >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = i >> 20;
            }
            else if (absoluteI >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = i >> 10;
            }
            else if (absoluteI >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }

            readable /= 1024;

            return readable.ToString("0 ") + suffix;
        }

        public class CASCFile : INotifyPropertyChanged
        {
            private string IFilePath { get; set; }
            public string FilePath
            {
                get
                {
                    return IFilePath;
                }
                set
                {
                    IFilePath = value;
                    OnPropertyChanged("FilePath");
                }
            }

            private string IFileType { get; set; }
            public string FileType
            {
                get
                {
                    return IFileType;
                }
                set
                {
                    IFileType = value;
                    OnPropertyChanged("FileType");
                }
            }

            private string IFileSize { get; set; }
            public string FileSize
            {
                get
                {
                    return IFileSize;
                }
                set
                {
                    IFileSize = value;
                    OnPropertyChanged("FileSize");
                }
            }

            private long IFileSizeBytes { get; set; }
            public long FileSizeBytes
            {
                get
                {
                    return IFileSizeBytes;
                }
                set
                {
                    IFileSizeBytes = value;
                    OnPropertyChanged("FileSizeBytes");
                }
            }

            private bool IFileLocal { get; set; }
            public bool FileLocal
            {
                get
                {
                    return IFileLocal;
                }
                set
                {
                    IFileLocal = value;
                    OnPropertyChanged("FileLocal");
                }
            }

            public CASCFile(string filePath, string fileType, string fileSize, long fileSizeBytes, bool fileLocal)
            {
                FilePath = filePath;
                FileType = fileType;
                FileSize = fileSize;
                FileSizeBytes = fileSizeBytes;
                FileLocal = FileLocal;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
