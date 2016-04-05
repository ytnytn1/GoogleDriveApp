using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using File = Google.Apis.Drive.v2.Data.File;

namespace Services
{
    public class GoogleDriveService
    {
        private readonly string[] _scopes = { DriveService.Scope.Drive };

        private readonly string ApplicationName = "Drive API .NET Quickstart";

        private readonly DriveService _service;

        private const int KB = 0x400;

        private const int DownloadChunkSize = 10024 * KB;

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

        public Task<About> GetInformation()
        {           
            var about = _service.About.Get().ExecuteAsync();          
            return about;
        }

        public Task<FileList> GetFileListAsync()
        {
            FilesResource.ListRequest request = _service.Files.List();
            request.Q = "'me' in owners";
            return  request.ExecuteAsync();
        }

        public async Task<DownloadStatus> DownloadFile(File fileResource, string saveTo)
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
                        return DownloadStatus.Completed;
                    }
                    return DownloadStatus.Failed;
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
            return DownloadStatus.NotStarted;
        }
    }
}
