using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Download;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using ViewModel;
using File = Google.Apis.Drive.v2.Data.File;


namespace ConsoleApplication1
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        private static string[] Scopes =
        {
            DriveService.Scope.Drive
        };

        static string ApplicationName = "Drive API .NET Quickstart";

        static void Main(string[] args)
        {
            MainViewModel = new MainViewModel();
           // UserCredential credential;

           // using (var stream =
           //     new FileStream("client_id.json", FileMode.Open, FileAccess.Read))
           // {
           //     string credPath = System.Environment.GetFolderPath(
           //         System.Environment.SpecialFolder.Personal);
           //     credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");
           //     credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
           //         GoogleClientSecrets.Load(stream).Secrets,
           //         Scopes,
           //         "user",
           //         CancellationToken.None,
           //         new FileDataStore(credPath, true)).Result;
           //     Console.WriteLine("Credential file saved to: " + credPath);
           // }
           //// credential.Token.AccessToken = "https://www.googleapis.com/auth/drive";
           // // Create Drive API service.
           // var service = new DriveService(new BaseClientService.Initializer()
           // {
           //     HttpClientInitializer = credential,
           //     ApplicationName = ApplicationName,

           // });
           // var service1 = new Google.Apis.Drive.v3.DriveService(new BaseClientService.Initializer()
           // {
           //     HttpClientInitializer = credential,
           //     ApplicationName = ApplicationName,

           // });
           // FilesResource.ListRequest listRequest = service.Files.List();
           // FilesResource.ListRequest filesRequest;
           // FilesResource.GetRequest getRequest = null;
           // IList<File> files = listRequest.Execute().Items;
           // Dictionary<File, string> fileDictionary = new Dictionary<File, string>();
           // //fileDictionary.Add(files,files.SingleOrDefault(f => f.OwnerNames[0] == "Иван Грищенко" && f.Parents[0].Id == "0AGaq3l-wPo8YUk9PVA").Id);
           // Console.WriteLine("Files:");
           // var debug = files.FirstOrDefault(f => f.Title == "Debug");
           // List<File> files1 = new List<File>();
           // if (files != null && files.Count > 0)
           // {
           //     ChildList children = new ChildList();
           //     foreach (var file in files.Where(f => f.OwnerNames[0] == "Иван Грищенко"))
           //     {
           //         fileDictionary.Add(file, file.Parents[0].Id);                   
           //        //Console.WriteLine("{0} ({1})",fileDictionary.Keys, fileDictionary.Values);
           //     }
           //     List<string> names = new List<string>();
           //     foreach (var value in fileDictionary.Values)
           //     {
           //         names.Add(files.FirstOrDefault(f => f.Id == value)?.Title);
           //     }
           //     names = names.Distinct().ToList();
           //     var dictionary = fileDictionary.GroupBy(v => v.Value)
           //         .ToDictionary(t => t.Key, t => t.Select(r => r.Key).ToList());
           //     //foreach (var var in dictionary)
           //     //{
           //     //    Console.WriteLine("{0}",files.FirstOrDefault(f => f.Id == var.Key)?.Title);
           //     //}
           //     foreach (var var in dictionary)
           //     {
           //         Console.WriteLine("Папка: {0}", files.FirstOrDefault(f => f.Id == var.Key)?.Title);
           //         foreach (var file in var.Value)
           //         {
           //             Console.WriteLine("Фаил{0} :", file.Title);
           //         }
           //         Console.WriteLine();
           //     }
           //     for (int i = 0; i < fileDictionary.Count; i++)
           //     {
           //         Console.WriteLine("{0} ({1})", fileDictionary.Keys.ElementAt(i).Title, 
           //             fileDictionary.Values.ElementAt(i));
           //         //BuildTree(fileDictionary.Keys.ElementAt(i));
           //     }
                
           //     //foreach (var file in files.Where(f => f.OwnerNames[0] == "Иван Грищенко" &&
           //     //                                      f.Parents[0]?.Id == files.FirstOrDefault
           //     //                                      (ff => ff.Title == "New folder (4)").Id))
           //     //{
           //     //    ChildrenResource.ListRequest request = service.Children.List(file.Id);
           //     //    children = request.Execute();
           //     //    Console.WriteLine("{0} ({1})", file.Title, file.Id);
           //     //    files1.Add(file);
                    
           //     //    //Console.WriteLine("{0}", children.Items[0].Id);
           //     //}
                
           //     //var xx = service.Files.List().Execute().Items.FirstOrDefault(f => f.Title == "metzo.v12.suo");
           //     ////    if (!String.IsNullOrEmpty(xx.DownloadUrl))
           //     ////    {
           //     ////        try
           //     ////        {
           //     ////            var x = service1.HttpClient.GetByteArrayAsync(xx.DownloadUrl);
           //     ////            byte[] arrBytes = x.Result;
           //     ////            System.IO.File.WriteAllBytes(@"C:\Users\user\Documents\GitHubVisualStudio\GoogleDriveApp", arrBytes);
           //     ////        }
           //     ////        catch (Exception e)
           //     ////        {
           //     ////            Console.WriteLine("An error occurred: " + e.Message);
           //     ////        }
           //     ////    }
           //     ////    else
           //     ////    {
           //     ////        // The file doesn't have any content stored on Drive.
           //     ////    }
           //     ////    //FindChildren(children,  service);
           //     ////}
           //     ////else
           //     ////{
           //     ////    Console.WriteLine("No files found.");
           //     ////}
           //     //var downloadRequest = service1.Files.Get(xx.Id);
           //     //var stream = new System.IO.MemoryStream();

           //     //// Add a handler which will be notified on progress changes.
           //     //// It will notify on each chunk download and when the
           //     //// download is completed or failed.
           //     //downloadRequest.MediaDownloader.ProgressChanged +=
           //     //    (IDownloadProgress progress) =>
           //     //    {
           //     //        switch (progress.Status)
           //     //        {
           //     //            case DownloadStatus.Downloading:
           //     //                {
           //     //                    Console.WriteLine(progress.BytesDownloaded);
           //     //                    break;
           //     //                }
           //     //            case DownloadStatus.Completed:
           //     //                {
           //     //                    Console.WriteLine("Download complete.");
           //     //                    break;
           //     //                }
           //     //            case DownloadStatus.Failed:
           //     //                {
           //     //                    Console.WriteLine("Download failed.");
           //     //                    break;
           //     //                }
           //     //        }
           //     //    };
           //     //downloadRequest.Download(stream);
           //     var parentId = "0AGaq3l-wPo8YUk9PVA";
           //     Console.Read();
           // }

           // //private static void FindChildren(ChildList children, DriveService service)
           // //{
           // //    foreach (var child in children.Items)
           // //    {
           // //        FindChildren(service.Files.Get(child.Id).Execute(), service);               
           // //    }
           // //}

        }

        public static MainViewModel MainViewModel { get; set; }

        //private IList<File> GetChildren(IList<File> files)
        //{
        //    foreach (var child in files)
        //    {
        //        GetChildren(child.)
        //    }
        //}
    }
}
