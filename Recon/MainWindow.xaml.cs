using AdonisUI.Controls;
using CASCLibNET;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Recon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public ObservableCollection<CASCFile> CASCFiles { get; } = new ObservableCollection<CASCFile>();

        CASCStorage Storage { get; set; }

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void MenuOpenCASC_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog folderSelect = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true,
                Title = "Open CASC Folder"
            };

            SetUIWorking(100, "Waiting for CASC folder...");

            if (folderSelect.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    Storage = new CASCStorage(folderSelect.FileName);

                    foreach (CASCFileInfo file in Storage.Files)
                    {
                        int idx = file.FileName.LastIndexOf(".");
                        string fileType = "File";

                        if (idx != -1)
                        {
                            fileType = file.FileName.Substring(idx + 1).ToUpper();
                        }

                        CASCFiles.Add(new CASCFile(file.FileName, fileType, GetBytesReadable(file.FileSize), file.FileSize, file.IsLocal));
                    }
                }
                catch (Exception ex)
                {
                    MenuCloseCASC_Click(null, null);

                    SetUIReadyUnloaded(ex.Message);

                    return;
                }

                Title = $"Recon | {folderSelect.FileName}";
                SetUIReadyLoaded();

                return;
            }

            SetUIReadyUnloaded();
        }

        private void MenuFind_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement CASC File list searching
        }

        private void MenuFilter_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement CASC File list filtering
        }

        private void MenuExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportCASCFiles(FileList.Items.Cast<CASCFile>().ToList());
        }

        private void MenuExportSelected_Click(object sender, RoutedEventArgs e)
        {
            ExportCASCFiles(FileList.SelectedItems.Cast<CASCFile>().ToList());
        }

        private void MenuRegisterAll_Click(object sender, RoutedEventArgs e)
        {
            List<CASCFile> files = FileList.Items.Cast<CASCFile>().ToList();
            Dictionary<string, long> register = new Dictionary<string, long>();

            new Thread(() =>
            {
                foreach (CASCFile file in files)
                {
                    register.Add(file.FileName, file.FileSizeBytes);
                }
            }).Start();

            ExportRegisterFile(register, "CASC");
        }

        private void MenuCloseCASC_Click(object sender, RoutedEventArgs e)
        {
            SetUIWorking(100, "Closing CASC...");

            CASCFiles.Clear();
            Storage?.Dispose();
            GC.Collect();

            Title = "Recon";
            SetUIReadyUnloaded();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            SetUIWorking(100, "Exiting...");

            Storage?.Dispose();
            GC.Collect();

            Application.Current.Shutdown();
        }

        private void MenuCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement an in-app solution to updates utilizing GitHub Releases

            System.Diagnostics.Process.Start("https://github.com/EthanC/Recon/releases/latest/");
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/EthanC/Recon");
        }

        private void FileList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: Export CASC File on double click

            //List<CASCFile> files;
            //Track item = ((FrameworkElement)e.OriginalSource).DataContext as Track;

            //ExportCASCFiles(files);
        }

        private void SetUIWorking(int progress = 100, string status = "Working...")
        {
            MenuOpenCASC.IsEnabled = false;
            MenuFind.IsEnabled = false;
            MenuFilter.IsEnabled = false;
            MenuExport.IsEnabled = false;
            MenuRegister.IsEnabled = false;
            MenuCloseCASC.IsEnabled = false;
            FileList.IsEnabled = false;
            Progress.Value = progress;
            Status.Text = status;
        }

        private void SetUIReadyUnloaded(string status = "Ready")
        {
            MenuOpenCASC.IsEnabled = true;
            MenuFind.IsEnabled = false;
            MenuFilter.IsEnabled = false;
            MenuExport.IsEnabled = false;
            MenuRegister.IsEnabled = false;
            MenuCloseCASC.IsEnabled = false;
            FileList.IsEnabled = false;
            Progress.Value = 0;
            Status.Text = status;
        }

        private void SetUIReadyLoaded(string status = "Ready")
        {
            MenuOpenCASC.IsEnabled = false;
            MenuFind.IsEnabled = true;
            MenuFilter.IsEnabled = true;
            MenuExport.IsEnabled = true;
            MenuRegister.IsEnabled = true;
            MenuCloseCASC.IsEnabled = true;
            FileList.IsEnabled = true;
            Progress.Value = 0;
            Status.Text = status;
        }

        private void SetProgressBar(int progress)
        {
            Progress.Value = progress;
        }

        private void SetStatus(string status)
        {
            Status.Text = status;
        }

        public class CASCFile : INotifyPropertyChanged
        {
            private string _FileName { get; set; }
            public string FileName
            {
                get
                {
                    return _FileName;
                }
                set
                {
                    _FileName = value;
                    OnPropertyChanged("FileName");
                }
            }

            private string _FileType { get; set; }
            public string FileType
            {
                get
                {
                    return _FileType;
                }
                set
                {
                    _FileType = value;
                    OnPropertyChanged("FileType");
                }
            }

            private string _FileSize { get; set; }
            public string FileSize
            {
                get
                {
                    return _FileSize;
                }
                set
                {
                    _FileSize = value;
                    OnPropertyChanged("FileSize");
                }
            }

            private long _FileSizeBytes { get; set; }
            public long FileSizeBytes
            {
                get
                {
                    return _FileSizeBytes;
                }
                set
                {
                    _FileSizeBytes = value;
                    OnPropertyChanged("FileSizeBytes");
                }
            }

            private bool _IsLocal { get; set; }
            public bool IsLocal
            {
                get
                {
                    return _IsLocal;
                }
                set
                {
                    _IsLocal = value;
                    OnPropertyChanged("IsLocal");
                }
            }

            public CASCFile(string fileName, string fileType, string fileSize, long fileSizeBytes, bool isLocal)
            {
                FileName = fileName;
                FileType = fileType;
                FileSize = fileSize;
                FileSizeBytes = fileSizeBytes;
                IsLocal = isLocal;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private GridViewColumnHeader lastHeaderClicked = null;

        private ListSortDirection lastDirection = ListSortDirection.Ascending;

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            ListSortDirection direction;

            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    Binding columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    string sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (lastHeaderClicked != null && lastHeaderClicked != headerClicked)
                    {
                        lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    lastHeaderClicked = headerClicked;
                    lastDirection = direction;
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(FileList.ItemsSource);

            dataView.SortDescriptions.Clear();
            dataView.SortDescriptions.Add(new SortDescription(sortBy, direction));

            dataView.Refresh();
        }

        public string GetBytesReadable(long i)
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

        private void ExportCASCFiles(List<CASCFile> files)
        {
            CommonOpenFileDialog folderSelect = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                EnsurePathExists = true,
                Title = "Choose Export Folder"
            };

            SetUIWorking(100, "Waiting for Export folder...");

            if (folderSelect.ShowDialog() == CommonFileDialogResult.Ok)
            {
                new Thread(() =>
                {
                    byte[] buffer = new byte[0x800000];

                    foreach (CASCFile file in files)
                    {
                        if (file.IsLocal == true)
                        {
                            string filePath = Path.Combine(folderSelect.FileName, file.FileName);
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                            try
                            {
                                using (CASCFileStream input = Storage.OpenFile(file.FileName))
                                using (FileStream output = File.Create(filePath))
                                {
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
                            }
                            catch (Exception ex)
                            {
                                SetUIReadyLoaded(ex.Message);
                            }
                        }
                    }
                }).Start();
            }

            SetUIReadyLoaded();
        }

        private void ExportRegisterFile(Dictionary<string, long> register, string defaultFileName)
        {
            SaveFileDialog saveDialog = new SaveFileDialog()
            {
                FileName = defaultFileName,
                Filter = "JSON (*.json)|*.json|All Files (*.*)|*"
            };

            SetUIWorking(100, "Waiting for Register location...");

            if (saveDialog.ShowDialog() == true)
            {
                using (StreamWriter registerFile = File.CreateText(saveDialog.FileName))
                {
                    registerFile.Write(JsonConvert.SerializeObject(register, Formatting.Indented));
                }
            }

            SetUIReadyLoaded();
        }
    }
}
