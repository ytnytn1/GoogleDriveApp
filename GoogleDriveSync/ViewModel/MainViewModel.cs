using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using IkitMita.Mvvm.ViewModels;
using Model;
using Service;
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

        public MainViewModel()
        {
            //_selectItemsCommand = new DelegateCommand(=> _selectedItems);   
            _selectedItems = new List<MyFile>();        
        }

        private async Task<List<MyFile>> GetFiles()
        {
            try
            {
                using (StartOperation())
                {   var fileList = await _googleDriveService.GetFileListAsync();                   
                   // files = fileList.Items.ToList().Select(i => i.Id, x => x.Title);
                    return fileList;
                }                            
            }
            catch (HttpRequestException requestException)
            {
                MessageBox.Show(requestException.Message);
                return null;
            }            
        }

        private Task<string> GetInfo()
        {
            try
            {
                using (StartOperation())
                {
                    return _googleDriveService.GetRootId();
                }
            }
            catch (HttpRequestException requestException)
            {
                MessageBox.Show(requestException.Message);
                return null;
            }
        }

        private void SelectItems(object items)
        {
            _selectedItems = new List<MyFile>();
            foreach (var item in (IEnumerable)items)
            {
                _selectedItems.Add((MyFile)item);
            }
            Debug.WriteLine(_selectedItems.Count);
        }

        public void OnMouseDoubleClick()
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

        public ICommand SelectItemsCommand => _selectItemsCommand ??
                                              (_selectItemsCommand = new DelegateCommand<object>(SelectItems));

        public ICommand MouseDblClickCommand => _mouseDblClickCommand ??
                                                (_mouseDblClickCommand = new DelegateCommand(OnMouseDoubleClick));

        public ICommand SyncCommand => _syncCommand ??
                                          (_syncCommand = new DelegateCommand(Sync));


        private async void Sync()
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
                                         (_loadedCommand = new DelegateCommand(OnLoad));

        private async void OnLoad()
        {        
            using (StartOperation())
            {
                try
                {
                    _googleDriveService = new GoogleDriveService();
                    _allFiles = await GetFiles();
                    _rootId =  await GetInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
               
            }                        
            Files = _allFiles?.Where(f => f.ParentId == _rootId).ToList();
        }

        public ICommand DownLoadCommand => _downLoadCommand ??
                                           (_downLoadCommand = new DelegateCommand<object>(Download));

        private async void Download(object o)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            folderBrowser.ShowDialog();
            var path = folderBrowser.SelectedPath;
            try
            {
                using (StartOperation())
                {
                     foreach (var selectedItem in _selectedItems)
                    {
                        var result = await _googleDriveService.DownloadFile(selectedItem, path);
                        //_fileWatchers.Add(new FileWatcher(path));
                        if (result != StatusOfDownload.DownloadCompleted)
                        {
                            MessageBox.Show($"Фаил {selectedItem.Name} не был загружен");
                        }
                    }
                }
            }
            catch (Exception ex)
            {                
                MessageBox.Show(ex.Message);
            }                     
        }

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
                                        (_closeCommand = new DelegateCommand<object>(Closing));

        private void Closing(object obj)
        {
            _googleDriveService.Dispose();
        }
    }
}
