using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
using File = Google.Apis.Drive.v2.Data.File;

namespace Service
{
    public class GoogleDriveService : IService, IDisposable
    {
        private readonly string[] _scopes = {DriveService.Scope.Drive};
        private Dictionary<string, string> _fileDictionary = new Dictionary<string, string>();
        private readonly string ApplicationName = "Drive API .NET Quickstart";
        private readonly DriveService _service;
        private List<FileWatcher> _fileWatchers;
        private List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();
        private List<string> _pathList = new List<string>();
        private List<MyFile> _files;
        private List<MyFile> _downloadedFiles = new List<MyFile>();

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
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            _service = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
            //_fileDictionary = new Dictionary<string, string>();
            _fileWatchers = new List<FileWatcher>();
            //string pathListJson =  System.IO.File.ReadAllText("JsonData/PathList.Json");
            //var dictionaryJson = System.IO.File.ReadAllText("JsonData/FileDictionary.Json");           
            //_pathList = JsonConvert.DeserializeObject<List<string>>(pathListJson);
            //_fileDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionaryJson);
        }

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
            _files = fileList.Items.Select(file => new MyFile
            {
                Name = file.Title,
                Id = file.Id,
                DownloadUrl = file.DownloadUrl,
                ParentId = file.Parents.FirstOrDefault()?.Id,
                Size = file.FileSize,
                CreationDate = file.CreatedDate,
                IsFolder = file.MimeType == "application/vnd.google-apps.folder",
                ModifiedDate = file.ModifiedDate
            }).ToList();
            return _files;
        }

        public async Task<StatusOfDownload> DownloadFile(MyFile fileResource, string saveTo)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(saveTo);
            string fullPath;
            string path;
            fullPath = saveTo + "\\" + fileResource.Name;
            path = saveTo;
            fileResource.StoredPath = fullPath;
            if (!fileResource.DownloadUrl.IsNullOrEmpty() && !fileResource.IsFolder)
            {
                var downloader = new MediaDownloader(_service);
                using (var fileStream = new FileStream(fullPath,
                    FileMode.Create, FileAccess.Write))
                {
                    var progress = await downloader.DownloadAsync(fileResource.DownloadUrl, fileStream);
                    _downloadedFiles.Add(fileResource);
                    if (progress.Status == DownloadStatus.Completed)
                    {
                        if (!IsSubDirectoryOf(path, _pathList))
                        {
                            _pathList.Add(path);
                        }
                        if (!_fileDictionary.ContainsKey(fullPath))
                        {
                            _fileDictionary.Add(fullPath, fileResource.Id);
                        }
                        return StatusOfDownload.DownloadCompleted;
                    }
                    return StatusOfDownload.DownLoadFailed;
                }

                //try
                //{

                //    //var x = await _service.HttpClient.GetByteArrayAsync(fileResource.DownloadUrl);
                //    //byte[] arrBytes = x;
                //    //using (var writer = new BinaryWriter(System.IO.File.OpenWrite(saveTo)))
                //    //{
                //    //    var progress = await downloader.DownloadAsync(fileResource.DownloadUrl, writer);
                //    //    writer.Write(arrBytes);
                //    //    writer.Flush();
                //    //    writer.Close();
                //    //    return Task;
                //    //}
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine("An error occurred: " + e.Message);
                //}
            }
            if(fileResource.IsFolder)
            {
                saveTo += "\\" + fileResource.Name;
                Directory.CreateDirectory(saveTo);
                var files = _files.Where(f => f.ParentId == fileResource.Id).ToList();
                foreach (var file in files)
                {
                    await DownloadFile(file, saveTo);
                }
                return StatusOfDownload.DownloadCompleted;
            }
            return StatusOfDownload.DownloadNotStarted;
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

        private XmlSerializer Xs { get; set; }

        public void Dispose()
        {

            //Xs = new XmlSerializer(typeof(Dictionary<string, string>));
            //using (StreamWriter wr = new StreamWriter("FileDictionary.xml"))
            //{
            //    Xs.Serialize(wr, _fileDictionary);
            //}
            var filedictionarySerializer = JsonConvert.SerializeObject(_fileDictionary);
            var pathListSerializer = JsonConvert.SerializeObject(_pathList);
            using (StreamWriter fileDictionaryWriter = System.IO.File.CreateText("JsonData/FileDictionary.Json"))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(fileDictionaryWriter))
            {
                jsonTextWriter.WriteRaw(filedictionarySerializer);
            }

            using (StreamWriter pathListWriter = System.IO.File.CreateText("JsonData/PathList.Json"))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(pathListWriter))
            {
                jsonTextWriter.WriteRaw(pathListSerializer);
            }

        }

        public async Task Synchronize()
        {
            _files = await GetFileListAsync();
            var folders = BuildWindowFoldersRepresentaion(_files);
            var driveFolders = _files.Where(f => f.IsFolder == true).ToList();
            foreach (var path in _pathList)
            {
                string[] fileDirectories = Directory.GetDirectories(path, "*.*",
                    SearchOption.AllDirectories);
                string [] localFiles = Directory.GetFiles(path, "*.*",
                    SearchOption.AllDirectories);
                //foreach (var localFile in localFiles)
                //{
                //    foreach (var driveFolder in driveFolders)
                //    {
                //        if(localFile.Contains(driveFolder))
                //    }
                //}
                //var folders = filePaths.S(fp => fp == path).ToList();       
                foreach (string t in localFiles)
                {
                    foreach (var folder in folders.Where(folder => !t.Contains(folder)))
                    {

                    }
                }
                foreach (var downloadedFile in _downloadedFiles)
                {
                    FileInfo fi = new FileInfo(downloadedFile.StoredPath);
                   // var firstOrDefault = _files.FirstOrDefault(f => f.Id == downloadedFile.Id);
                    if (fi.LastWriteTime > _files.FirstOrDefault(f => f.Id == downloadedFile.Id)?.ModifiedDate)
                    {
                        await UpdateFile(_service, downloadedFile.Id, downloadedFile.StoredPath, true);
                    }
                }               
            }
        }

        private List<string> BuildWindowFoldersRepresentaion(List<MyFile> files )
        {
            List<string> folders = new List<string>();
           // var rootId = await GetRootId();
            foreach (var file in files)
            {
                folders.Add(GetParents(file.ParentId, file.Name, files));
            }
            return folders;
        }

        private string GetParents(string parentId, string name, List<MyFile> files )
        {
            string x = null;
            foreach (var file in files.Where(f=>f.Id == parentId))
            {
                x = x + GetParents(file.ParentId, file.Name, files) + "//";
            }

            return x + name;
        }
        private async Task UpdateFile(DriveService service, String fileId, String newFilename, bool newRevision)
        {
            try
            {
                // First retrieve the file from the API.
                File file = await service.Files.Get(fileId).ExecuteAsync();

                // File's new content.
                byte[] byteArray = System.IO.File.ReadAllBytes(newFilename);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                // Send the request to the API.
                FilesResource.UpdateMediaUpload request = service.Files.Update(file, fileId, stream, file.MimeType);
                request.NewRevision = newRevision;
                await request.UploadAsync();

                //File updatedFile = request.ResponseBody;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: " + e.Message);
            }
        }
    }
}
