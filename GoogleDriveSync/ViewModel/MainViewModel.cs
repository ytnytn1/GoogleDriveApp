using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Google;
using Google.Apis.Download;
using Google.Apis.Drive.v2.Data;
using IkitMita.Mvvm.ViewModels;
using Microsoft.Win32;
using Services;
using ViewModel.View.ViewModel;
using MessageBox = System.Windows.MessageBox;

namespace ViewModel
{
    public class MainViewModel: ViewModelBase
    {
        private GoogleDriveService _googleDriveService;
        private File _previuousFolder;
        private ICommand _loadedCommand;
        private List<File> _selectedItems;
        private List<File> _allFiles;
        private ICommand _selectItemsCommand;
        private List<File> _files;
        private ICommand _mouseDblClickCommand;
        private ICommand _refreshCommand;
        private ICommand _goUpCommand;
        private string _rootId;
        private readonly string _folderMimetype = "application/vnd.google-apps.folder";
        private ICommand _downLoadCommand;

        public MainViewModel()
        {
            //_selectItemsCommand = new DelegateCommand(=> _selectedItems);           
        }

        private async Task<List<File>> GetFiles()
        {
            try
            {
                using (StartOperation())
                {   var fileList = await _googleDriveService.GetFileListAsync();
                    return fileList.Items.ToList();
                }                            
            }
            catch (HttpRequestException requestException)
            {
                MessageBox.Show(requestException.Message);
                return null;
            }            
        }

        private async Task<About> GetInfo()
        {
            try
            {
                using (StartOperation())
                {
                    return await _googleDriveService.GetInformation();
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
            _selectedItems = new List<File>();
            foreach (var item in (IEnumerable)items)
            {
                _selectedItems.Add((File)item);
            }
            Debug.WriteLine(_selectedItems.Count);
        }

        public void OnMouseDoubleClick(object obj)
        {
            var item = _selectedItems.FirstOrDefault();
            if (_previuousFolder?.Id != item?.Id)
            {
                _previuousFolder = item;
            }
            if (item.MimeType == _folderMimetype)
            {
                var x = _allFiles.FirstOrDefault(f => f.Id == item.Id);//0B2aq3l-wPo8YWFBITy00V0JuLWc
                var items = new List<File>(_allFiles.Where(f => f.Parents[0].Id == x.Id).ToList());
                Files = items;
            }           
        }

        public List<File> Files
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
                                              (_selectItemsCommand = new RelayCommand(SelectItems));

        public ICommand MouseDblClickCommand => _mouseDblClickCommand ??
                                                (_mouseDblClickCommand = new RelayCommand(OnMouseDoubleClick));

        public ICommand RefreshCommand => _refreshCommand ??
                                          (_refreshCommand = new RelayCommand(Refresh));


        private async void Refresh(object obj)
        {
            using (StartOperation())
            {
                 _googleDriveService.GetFileListAsync();
            }
                               
        }

        public ICommand GoUpCommand => _goUpCommand ??
                                       (_goUpCommand = new DelegateCommand(GoUp));

        private void GoUp()
        {
            string parentId = null;
            if (Files.Count != 0)
            {
               parentId = Files?.FirstOrDefault()?.Parents.FirstOrDefault()?.Id;
               var parentOfParent = _allFiles.FirstOrDefault(f => f.Id == parentId)?.Parents.FirstOrDefault()?.Id;
                if (parentOfParent != null)
               {
                   Files = _allFiles.Where(f => f.Parents.FirstOrDefault()?.Id == parentOfParent).ToList();
               }
            }
            else
            {
                parentId = _previuousFolder?.Parents.FirstOrDefault()?.Id;
                var parentOfParent = _allFiles.FirstOrDefault(f => f.Id == parentId)?.Parents.FirstOrDefault()?.Id;
                if (parentOfParent != null)
                {
                    Files = _allFiles.Where(f => f.Parents.FirstOrDefault()?.Id == parentId).ToList();
                }
            }                                       
        }

        public ICommand LoadedCommand => _loadedCommand ??
                                         (_loadedCommand = new RelayCommand(OnLoad));

        private async void OnLoad(object obj)
        {
            About about = null;           
            using (StartOperation())
            {
                try
                {
                    _googleDriveService = new GoogleDriveService();
                    _allFiles = await GetFiles();
                    about = await GetInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
               
            }                        
            _rootId = about?.RootFolderId;
            Files = _allFiles?.Where(f => f.Parents.FirstOrDefault()?.Id == _rootId).ToList();
        }

        public ICommand DownLoadCommand => _downLoadCommand ??
            (_downLoadCommand = new RelayCommand(Download, 
                o => _selectedItems != null && _selectedItems.Count > 0));

        private async  void Download(object o)
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
                         var result = await _googleDriveService.DownloadFile(selectedItem, path+"\\"+selectedItem.Title);
                        if (result != DownloadStatus.Completed)
                        {
                            MessageBox.Show($"Фаил {selectedItem.Title} не был загружен");
                        }
                    }
                }
            }
            catch (Exception ex)
            {                
                MessageBox.Show(ex.Message);
            }                     
        }
    }
}
