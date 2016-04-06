using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Model;
using File = Google.Apis.Drive.v2.Data.File;

namespace Services
{
    public class GoogleDriveService: IService
    {
        private readonly string[] _scopes = { DriveService.Scope.Drive };

        private readonly string ApplicationName = "Drive API .NET Quickstart";

        private readonly DriveService _service;

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
            var files = fileList.Items.ToList().Select(file => new MyFile
            {
                Name = file.Title,
                Id = file.Id,
                DownloadUrl = file.DownloadUrl,
                ParentId = file.Parents.FirstOrDefault()?.Id,
                Size = file.FileSize, CreationDate = file.CreatedDate,
                IsFolder = file.MimeType == "application/vnd.google-apps.folder"
            }).ToList();
            return files;
        }

        public async Task<StatusOfDownload> DownloadFile(MyFile fileResource, string saveTo)
        {
            if (!String.IsNullOrEmpty(fileResource.DownloadUrl))
            {
                var downloader = new MediaDownloader(_service);
                using (var fileStream = new FileStream(saveTo,
                       FileMode.Create, FileAccess.Write))
                {
                    var progress = await downloader.DownloadAsync(fileResource.DownloadUrl, fileStream);

                    if (progress.Status == DownloadStatus.Completed)
                    {
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
            return StatusOfDownload.DownloadNotStarted;
        }
    }
}
