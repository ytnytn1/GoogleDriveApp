using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using IkitMita.Mvvm.ViewModels;
using Model;
using Service;
using Services;
using MessageBox = System.Windows.MessageBox;

namespace ViewModel
{
    public class MainViewModel: ViewModelBase
    {
        private GoogleDriveService _googleDriveService;
        private MyFile _previuousFolder;
        private ICommand _loadedCommand;        
        private ICommand _selectItemsCommand;
        private List<MyFile> _files;
        private List<MyFile> _allFiles;
        private ICommand _mouseDblClickCommand;
        private ICommand _goUpCommand;
        private string _rootId;
        private ICommand _downLoadCommand;
        private ICommand _closeCommand;
        private List<MyFile> _selectedItems;
        private List<FileWatcher> _fileWatchers = new List<FileWatcher>();
        private DelegateCommand _syncCommand;
        private ICommand _cancelCommand;
        private string _message;
        private long _progress;
        private long _size;
        private bool _isDownloaded;

        public MainViewModel()
        {
            //_selectItemsCommand = new DelegateCommand(=> _selectedItems);   
            _selectedItems = new List<MyFile>();        
        }

        private async Task<List<MyFile>> GetFiles()
        {
            using (StartOperation())
            {
                var fileList = await _googleDriveService.GetFileListAsync();
                // files = fileList.Items.ToList().Select(i => i.Id, x => x.Title);
                return fileList;
            }  
        }

        private Task<string> GetInfo()
        {
            using (StartOperation())
            {
                return _googleDriveService.GetRootId();
            }           
        }

        public List<MyFile> Files
        {
            get { return _files; }
            set
            {
                if (Equals(value, _files)) return;
                _files = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectItemsCommand => _selectItemsCommand ??
                                              (_selectItemsCommand = new DelegateCommand<object>(SelectItems));

        private void SelectItems(object items)
        {
            _selectedItems = new List<MyFile>();
            foreach (var item in (IEnumerable)items)
            {
                _selectedItems.Add((MyFile)item);
            }
            Debug.WriteLine(_selectedItems.Count);
        }

        public ICommand MouseDblClickCommand => _mouseDblClickCommand ??
                                                (_mouseDblClickCommand = new DelegateCommand(ItemDblClick));

        private void ItemDblClick()
        {
            var item = _selectedItems.FirstOrDefault();
            if (_previuousFolder?.Id != item?.Id)
            {
                _previuousFolder = item;
            }
            if (item.IsFolder)
            {
                var x = _allFiles.FirstOrDefault(f => f.Id == item.Id);//0B2aq3l-wPo8YWFBITy00V0JuLWc
                var items = new List<MyFile>(_allFiles.Where(f => f.ParentId == x.Id).ToList());
                Files = items;
            }
        }

        public ICommand SyncCommand => _syncCommand ??
                                          (_syncCommand = new DelegateCommand(async () => await Sync()));

        private async Task Sync()
        {
            using (StartOperation())
            {
                 await _googleDriveService.Synchronize();
            }
                               
        }

        public ICommand GoUpCommand => _goUpCommand ??
                                       (_goUpCommand = new DelegateCommand(GoUp));

        private void GoUp()
        {
            string parentId = null;
            if (Files.Count != 0)
            {
                parentId = Files?.FirstOrDefault()?.ParentId;
               var parentOfParent = _allFiles.FirstOrDefault(f => f.Id == parentId)?.ParentId;
                if (parentOfParent != null)
               {
                   Files = _allFiles.Where(f => f.ParentId == parentOfParent).ToList();
               }
            }
            else
            {
                parentId = _previuousFolder?.ParentId;
                var parentOfParent = _allFiles.FirstOrDefault(f => f.Id == parentId)?.ParentId;
                if (parentOfParent != null)
                {
                    Files = _allFiles.Where(f => f.ParentId == parentId).ToList();
                }
            }                                       
        }

        public ICommand LoadedCommand => _loadedCommand ??
                                         (_loadedCommand = new DelegateCommand(LoadService));

        public long Progress
        {
            get { return _progress; }
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public long Size
        {
            get { return _size; }
            set
            {
                _size = value;
                OnPropertyChanged();
            }
        }

        public bool IsDownloaded
        {
            get { return _isDownloaded; }
            set
            {
                _isDownloaded = value; 
                OnPropertyChanged();
            }
        }

        private async void LoadService()
        {        
            using (StartOperation())
            {
                try
                {
                    _googleDriveService = new GoogleDriveService();
                    _googleDriveService.ProgressChanged += OnProgressChanged;
                    _allFiles = await GetFiles();
                    _rootId = await GetInfo();
                    await Sync();
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }                        
            Files = _allFiles?.Where(f => f.ParentId == _rootId).ToList();
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            Progress = args.BytesDownloaded;
        }

        public ICommand DownLoadCommand => _downLoadCommand ?? (_downLoadCommand = new DelegateCommand<string>(Download));

        private async void Download(string path = null)
        {
            if(path == null)
            {
                FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
                folderBrowser.ShowDialog();
                path = folderBrowser.SelectedPath;
            }
            if (path != string.Empty)
            {
                using (StartOperation())
                {
                     
                    foreach (var selectedItem in _selectedItems.ToList())
                    {
                        Size = 0;
                        IsDownloaded = true;
                        if (selectedItem.Size != null) Size = selectedItem.Size.Value;
                        StatusOfDownload result = StatusOfDownload.NotStated;
                        if (!selectedItem.IsFolder)
                        {
                            result = await _googleDriveService.DownloadFile(selectedItem, path);
                            
                        }
                        else
                        {
                            path += "\\" + selectedItem.Name;
                            Directory.CreateDirectory(path);
                            var files = _allFiles.Where(f => f.ParentId == selectedItem.Id).ToList();
                            _selectedItems = files;
                            Download(path);
                        }
                        switch (result)
                        {
                            case StatusOfDownload.DownLoadFailed:
                                IsDownloaded = false;
                                break;
                            case StatusOfDownload.DownloadAborted:
                                IsDownloaded = false;
                                break;
                            case StatusOfDownload.DownloadNotStarted:
                                IsDownloaded = false;
                                break;
                            case StatusOfDownload.DownloadCompleted:
                                IsDownloaded = false;
                                break;
                        }
                    }                   
                }
            }
        }

        private CancellationTokenSource CancellationToken { get; set; }

        private bool IsSubDirectoryOf(string candidate, List<string> others)
        {
            var isChild = false;
            try
            {
                foreach (var other in others)
                {
                    var candidateInfo = new DirectoryInfo(candidate);
                    var otherInfo = new DirectoryInfo(other);

                    while (candidateInfo.Parent != null)
                    {
                        if (candidateInfo.Parent.FullName == otherInfo.FullName || candidate == other)
                        {
                            isChild = true;
                            break;
                        }
                        else candidateInfo = candidateInfo.Parent;
                    }
                }
            }
            catch (Exception error)
            {
                //var message = String.Format("Unable to check directories {0} and {1}: {2}", candidate, other, error);
                //Trace.WriteLine(message);
            }
            return isChild;
        }

        public ICommand CloseCommand => _closeCommand ??
                                        (_closeCommand = new DelegateCommand(Closing));

        private void Closing()
        {         
             _googleDriveService.Closing();         
        }

        public ICommand CancelCommand => _cancelCommand ??
                                         (_cancelCommand = new DelegateCommand(Cancel));

        private void Cancel()
        {      
            _googleDriveService.CancellationTokenSource?.Cancel();
        }
    }
}
