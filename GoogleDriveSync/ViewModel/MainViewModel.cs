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
using System.Windows;
using System.Windows.Input;
using Google;
using Google.Apis.Drive.v2.Data;
using IkitMita.Mvvm.ViewModels;
using Services;
using ViewModel.View.ViewModel;

namespace ViewModel
{
    public class MainViewModel: ViewModelBase
    {
        private GoogleDriveService _googleDriveService;

        public MainViewModel()
        {
            //_selectItemsCommand = new DelegateCommand(=> SelectedItems);
            _googleDriveService = new GoogleDriveService();
            //_allFiles = _googleDriveService.GetFileListAsync().Result.Items;;
            _allFiles = GetFiles().Result.Items.Where(f => f.OwnerNames[0] == "Иван Грищенко").ToList();
            Files = _allFiles.Where(f => f.Parents.FirstOrDefault()?.Id == "0AGaq3l-wPo8YUk9PVA").ToList();

        }
        private  Task<FileList> GetFiles()
        {
            try
            {
                using (StartOperation())
                {
                    return  _googleDriveService.GetFileListAsync();
                }                            
            }
            catch (HttpRequestException requestException)
            {
                MessageBox.Show(requestException.Message);
                return null;
            }            
        }

        private List<File> _allFiles;
        private ICommand _selectItemsCommand;
        private List<File> _files;
        private ICommand _mouseDblClickCommand;
        private ICommand _refreshCommand;
        private readonly ICommand _goUpCommand;


        private void SelectItems(object items)
        {
            var x = items.GetType();
            SelectedItems = new List<File>();
            foreach (var item in (IEnumerable)items)
            {
                SelectedItems.Add((File)item);
            }
            Debug.WriteLine(SelectedItems.Count);
        }

        public void OnMouseDoubleClick(object obj)
        {
            var item = SelectedItems.FirstOrDefault();
            if (_previuousFolder?.Id != item?.Id)
            {
                _previuousFolder = item;
            }
            if (item.MimeType.Contains("folder"))
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

        public ICommand SelectItemsCommand
        {
            get
            {
                return _selectItemsCommand ??
                       (_selectItemsCommand = new RelayCommand(SelectItems));
            }
        }

        public ICommand MouseDblClickCommand
        {
            get
            {
                return _mouseDblClickCommand ??
                       (_mouseDblClickCommand = new RelayCommand(OnMouseDoubleClick));
            }
        }

        public ICommand RefreshCommand
        {
            get { return _refreshCommand ??
                    (_refreshCommand = new RelayCommand(Refresh)); }
        }
       

        private async void Refresh(object o)
        {
            using (StartOperation())
            {
                await _googleDriveService.GetFileListAsync();
            }                     
        }

        public ICommand GoUpCommand
        {
            get { return _goUpCommand ??
                    (_refreshCommand = new DelegateCommand(GoUp)); }
        }

        private void GoUp()
        {
            var parentId = Files.FirstOrDefault().Parents.FirstOrDefault().Id;                     
            var parentOfParent = _allFiles.FirstOrDefault(f => f.Id == parentId)?.Parents.FirstOrDefault()?.Id;
            if (parentOfParent != null)
            {
                Files = _allFiles.Where(f => f.Parents.FirstOrDefault().Id == parentOfParent).ToList();
            }          
        }

        private File _previuousFolder;

        private List<File> SelectedItems { get; set; }
    }
}
