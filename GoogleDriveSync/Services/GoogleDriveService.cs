using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using RestSharp;
using File = Google.Apis.Drive.v2.Data.File;

namespace Services
{
    public class GoogleDriveService
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
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = System.IO.Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    _scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            _service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
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

        public Boolean DownloadFile(File fileResource, string saveTo)
        {
            if (!String.IsNullOrEmpty(fileResource.DownloadUrl))
            {
                try
                {
                    var x = _service.HttpClient.GetByteArrayAsync(fileResource.DownloadUrl);
                    byte[] arrBytes = x.Result;
                    BinaryWriter writer = null;
                    string Name = @"C:\temp\yourfile.name";
                    // Create a new stream to write to the file
                    writer = new BinaryWriter(System.IO.File.OpenWrite(saveTo));             
                    writer.Write(arrBytes);
                    writer.Flush();
                    writer.Close();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return false;
                }
            }
            else
            {
                // The file doesn't have any content stored on Drive.
                return false;
            }
        }
    }
}
