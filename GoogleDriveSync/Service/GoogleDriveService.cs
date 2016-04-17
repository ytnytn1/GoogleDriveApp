using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using IkitMita;
using Model;
using Newtonsoft.Json;
using Services;
using Services.extensions;
using File = Google.Apis.Drive.v2.Data.File;

namespace Service
{
    public class GoogleDriveService : IService
    {
        private const int Kb = 0x400;
        private const int DownloadChunkSize = 512 * Kb;
        private readonly string[] _scopes = {DriveService.Scope.Drive};
        private Dictionary<string, string> _fileDictionary = new Dictionary<string, string>();
        private readonly string ApplicationName = "Drive API .NET Quickstart";
        private readonly DriveService _service;
        private List<string> _pathList;
        private List<MyFile> _allFiles;
        private List<MyFile> _downloadedFiles;
        private readonly List<MyFile> _tempList = new List<MyFile>();

        public GoogleDriveService()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(
                    Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    _scopes,
                    "user",
                    System.Threading.CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
            DeserializeDownloadedFiles();
            DeserializePathList();
            //_fileDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryJson);
        }

        private void DeserializePathList()
        {
            var fileName = ".\\JsonData\\PathList.Json";
            var fi = new FileInfo(fileName);
            if (!fi.Exists && fi.Directory.Exists)
            {
                using (FileStream stream = System.IO.File.Create(fileName))
                {
                    stream.Close();
                }
            }
            else if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
                using (FileStream stream = System.IO.File.Create(fileName))
                {
                    stream.Close();
                }
            }
            string pathListJson = System.IO.File.ReadAllText(fileName);
            _pathList = JsonConvert.DeserializeObject<List<string>>(pathListJson)
                               ?? new List<string>();
        }

        private void DeserializeDownloadedFiles()
        {
            var fileName = ".\\JsonData\\DownloadedFiles.Json";
            var fi = new FileInfo(fileName);
            if (!fi.Exists && fi.Directory.Exists)
            {
                using (FileStream stream = System.IO.File.Create(fileName))
                {
                    stream.Close();
                }
            }
            else if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
                using (FileStream stream = System.IO.File.Create(fileName))
                {
                    stream.Close();
                }
            }
            string downloadedFilesJson = System.IO.File.ReadAllText(fileName);
            _downloadedFiles = JsonConvert.DeserializeObject<List<MyFile>>(downloadedFilesJson)
                               ?? new List<MyFile>();
        }

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        private long _progress;

        private long _size;

        public event EventHandler<ProgressChangedEventArgs > ProgressChanged;

        public async Task<string> GetRootId()
        {
            var about = await _service.About.Get().ExecuteAsync();
            return about.RootFolderId;
        }

        public async Task<List<MyFile>> GetFileListAsync()
        {
            FilesResource.ListRequest request = _service.Files.List();
            request.Q = "'me' in owners";
            var fileList = await request.ExecuteAsync();
            _allFiles = fileList.Items.Select(file => new MyFile
            {
                Name = file.Title,
                Id = file.Id,
                DownloadUrl = file.DownloadUrl,
                ParentId = file.Parents.FirstOrDefault()?.Id,
                Size = file.FileSize,
                CreationDate = file.CreatedDate,
                IsFolder = file.MimeType == "application/vnd.google-apps.folder",
                ModifiedDate = file.ModifiedDate,
                Md5Checkum = file.Md5Checksum,
                FullPath = file.Properties?.FirstOrDefault(p=>p.Key == "1")?.Value                
            }).ToList();
            return _allFiles;
        }

        public async Task<StatusOfDownload> DownloadFile(MyFile fileResource, string saveTo)
        {
            CancellationTokenSource = new CancellationTokenSource();
            DirectoryInfo directoryInfo = new DirectoryInfo(saveTo);
            var fullPath = saveTo + "\\" + fileResource.Name;
            var path = saveTo;
            var bakFile = fullPath + "." + "tmp";
            fileResource.FullPath = fullPath;
            fileResource.Path = saveTo;
            _tempList.Add(fileResource);
            if (!fileResource.DownloadUrl.IsNullOrEmpty() || fileResource.IsFolder)
            {
                _tempList.Add(fileResource);
                if (!fileResource.IsFolder)
                {
                    //if (System.IO.File.Exists(fullPath))
                    //{
                    //    System.IO.File.Copy(fullPath, bakFile);
                    //}                   
                    var downloader = new MediaDownloader(_service);
                    downloader.ProgressChanged += DownloaderOnProgressChanged;
                    downloader.ChunkSize = DownloadChunkSize;                    
                    using (var fileStream = new FileStream(bakFile,
                        FileMode.Create, FileAccess.Write))
                    {
                        IDownloadProgress progress;
                        try
                        {                           
                            progress = await downloader.DownloadAsync(fileResource.DownloadUrl, fileStream,
                                CancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            //RemoveDownloaded(fileStream);
                            //foreach (var file in _tempList)
                            //{
                            //    _downloadedFiles.RemoveAll(f => f.FullPath == file.FullPath);
                            //}
                            fileStream.Close();
                            var di = new DirectoryInfo(saveTo);
                            var fileInfos = di.GetFiles();
                            foreach (var fileInfo in fileInfos.Where(fi => fi.Name.EndsWith(".tmp")))
                            {
                                fileInfo.Delete();
                            }
                            return StatusOfDownload.DownloadAborted;
                        }
                        if (progress.Status == DownloadStatus.Completed)
                        {
                            fileStream.Close();
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                                System.IO.File.Move(bakFile, fullPath);
                            }
                            else
                            {
                                System.IO.File.Move(bakFile, fullPath);
                            }                         
                            if (_downloadedFiles.FirstOrDefault(df => df.FullPath == fullPath) == null)
                            {
                                _downloadedFiles.Add(fileResource);
                            }                           
                            if (!IsSubDirectoryOf(path, _pathList))
                            {
                                _pathList.Add(path);
                            }                            
                            return StatusOfDownload.DownloadCompleted;
                        }
                    }
                }
                //if(fileResource.IsFolder)
                //{
                //    saveTo += "\\" + fileResource.Name;
                //    Directory.CreateDirectory(saveTo);                    
                //    var files = _allFiles.Where(f => f.ParentId == fileResource.Id).ToList();
                //    foreach (var file in files)
                //    {
                //        var result = await DownloadFile(file, saveTo);
                //        if (result == StatusOfDownload.DownloadAborted)
                //        {
                //           return StatusOfDownload.DownloadAborted;
                //        }
                //    }
                //    return StatusOfDownload.DownloadCompleted;
                //}                
            }
            _downloadedFiles = _downloadedFiles.Distinct().ToList();
            return StatusOfDownload.DownloadNotStarted;
        }

        private void DownloaderOnProgressChanged(IDownloadProgress downloadProgress)
        {

            _progress = downloadProgress.BytesDownloaded;
            OnProgressChanged(new ProgressChangedEventArgs(_progress));

            //Debug.WriteLine(downloadProgress.BytesDownloaded);
        }

        private void RemoveDownloaded(FileStream fileStream)
        {
            fileStream.Close();
            foreach (var file in _tempList.Where(f => !f.IsFolder))
            {
                _downloadedFiles.RemoveAll(f => f.FullPath == file.FullPath);
                System.IO.File.Delete(file.FullPath);
            }
            foreach (var file in _tempList.Where(f => f.IsFolder))
            {
                _downloadedFiles.RemoveAll(f => f.FullPath == file.FullPath);
                DirectoryInfo directory = new DirectoryInfo(file.FullPath);
                directory.Delete();
            }
            // _tempList.Clear();
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

        private bool IsSubfolder(string parentPath, string childPath)
        {
            var parentUri = new Uri(parentPath);

            var childUri = new DirectoryInfo(childPath).Parent;

            while (childUri != null)
            {
                if (new Uri(childUri.FullName) == parentUri)
                {
                    return true;
                }

                childUri = childUri.Parent;
            }

            return false;
        }

        private async Task<Property> InsertProperty(String fileId, String value)
        {
            Property newProperty = new Property
            {
                Key = "1",
                Value = value,
                Visibility = "public"
            };
            try
            {
                return await _service.Properties.Insert(newProperty, fileId).ExecuteAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occurred: " + e.Message);
            }
            return null;
        }

        private async Task RemoveProperty(string fileId, string key)
        {
            string visibility = "public";
            try
            {
                var request = _service.Properties.Delete(fileId, key);
                request.Visibility = visibility;
                await request.ExecuteAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occurred: " + e);
            }
        }

        public void Closing()
        {
            var downloadedFilesSerializer = JsonConvert.SerializeObject(_downloadedFiles);           
            using (StreamWriter downdloadedFilesWriter = System.IO.File.CreateText("JsonData/DownloadedFiles.Json"))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(downdloadedFilesWriter))
            {
                jsonTextWriter.WriteRaw(downloadedFilesSerializer);
            }
            var pathListSerializer = JsonConvert.SerializeObject(_pathList);
            using (StreamWriter pathListWriter = System.IO.File.CreateText("JsonData/PathList.Json"))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(pathListWriter))
            {
                jsonTextWriter.WriteRaw(pathListSerializer);
            }
        }

        public async Task Synchronize()
        {
            _allFiles = await GetFileListAsync();
            foreach (var df in _downloadedFiles.Where(f => f.FullPath != null).ToList())
            {
                var fi = new FileInfo(df.FullPath);
                if (fi.Exists && _allFiles.Exists(f=> f.Id == df.Id))
                {
                    var md5Sum = GetMd5Sum(fi.FullName);
                    if (md5Sum != df.Md5Checkum)
                    {
                        if (fi.LastWriteTime > df.ModifiedDate)
                        {
                            await UpdateFile(df.Id, fi.FullName, true);
                            df.Md5Checkum = md5Sum;
                        }
                        else if (fi.LastWriteTime < df.ModifiedDate)
                        {
                            await DownloadFile(df, fi.DirectoryName);
                            df.Md5Checkum = _allFiles.FirstOrDefault(f => f.Id == df.Id)?.Md5Checkum;
                        }
                    }                    
                }
                else
                {
                    _downloadedFiles.RemoveAll(f => f.FullPath == fi.FullName);
                }
            }
            //List<string> folders = BuildWindowFoldersRepresentaion(_allFiles);
            //var driveFolders = _allFiles.Where(f => f.IsFolder == true).ToList();
            //foreach (var path in _pathList.ToList())
            //{
            //    var files = Directory.GetFiles(path);
            //    foreach (var file in files)
            //    {
            //        FileInfo fi = new FileInfo(file);
            //        var md5Sum = GetMd5Sum(fi.FullName);
            //        foreach (var myFile in _allFiles)
            //        {
            //            if (md5Sum != myFile.Md5Checkum)
            //            {
            //                if (fi.LastWriteTime > myFile.ModifiedDate)
            //                {
            //                    await UpdateFile(_service, myFile.Id, fi.Name, true);
            //                }
            //                else if (fi.LastWriteTime < myFile.ModifiedDate)
            //                {
            //                    await DownloadFile(myFile, fi.DirectoryName);
            //                }
            //            }
            //        }
            //    }
            //}
            //FileInfo fi = new FileInfo(downloadedFile.FullPath);
            //var md5Sum = GetMd5Sum(fi.FullPath);
            //// var firstOrDefault = _allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id);
            //if (md5Sum != _allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id)?.Md5Checkum)
            //{
            //    if (fi.LastWriteTime > _allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id)?.ModifiedDate)
            //    {
            //        await UpdateFile(_service, downloadedFile.Id, downloadedFile.FullPath, true);
            //        _downloadedFiles.Remove(downloadedFile);
            //        _downloadedFiles.Add(_allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id));
            //    }
            //    else if(fi.LastWriteTime < _allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id)?.ModifiedDate)
            //    {
            //        _downloadedFiles.Remove(downloadedFile);
            //        await DownloadFile(downloadedFile, downloadedFile.Path);
            //    }            

        }

        //private List<string> BuildWindowFoldersRepresentaion(List<MyFile> files )
        //{
        //    List<string> folders = new List<string>();
        //   // var rootId = await GetRootId();
        //    foreach (var file in files)
        //    {
        //        folders.Add(GetParents(file.ParentId, file.Name, files));
        //    }
        //    return folders;
        //}

        //private string GetParents(string parentId, string name, List<MyFile> files )
        //{
        //    string x = null;
        //    foreach (var file in files.Where(f=>f.Id == parentId))
        //    {
        //        x = x + GetParents(file.ParentId, file.Name, files) + "\\";
        //    }

        //    return x + name;
        //}

        private async Task UpdateFile(string fileId, string newFilename, bool newRevision)
        {
            try
            {
                // First retrieve the file from the API.
                File file = await _service.Files.Get(fileId).ExecuteAsync();

                // File's new content.
                byte[] byteArray = System.IO.File.ReadAllBytes(newFilename);
                MemoryStream stream = new System.IO.MemoryStream(byteArray);
                // Send the request to the API.
                var request = _service.Files.Update(file, fileId, stream, file.MimeType);
                request.NewRevision = newRevision;
                await request.UploadAsync();

                //File updatedFile = request.ResponseBody;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }

        private void CheckMd5Sum()
        {
            foreach (var downloadedFile in _downloadedFiles.ToList())
            {
                var md5Sum = GetMd5Sum(downloadedFile.FullPath);
                if (md5Sum != _allFiles.FirstOrDefault(f => f.Id == downloadedFile.Id)?.Md5Checkum)
                {
                    System.IO.File.Delete(downloadedFile.FullPath);
                    _downloadedFiles.Remove(downloadedFile);
                }
            }           
        }
        private string GetMd5Sum(string fileNname)
        {
            using (var md5 = MD5.Create())
            {
                if (System.IO.File.Exists(fileNname))
                {
                    string md5Sum;
                    using (var stream = System.IO.File.OpenRead(fileNname))
                    {
                         md5Sum = md5.ComputeHash(stream).ToHex(false);
                    }
                    return md5Sum;
                }                
                return null;
            }
        }

        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }
}
