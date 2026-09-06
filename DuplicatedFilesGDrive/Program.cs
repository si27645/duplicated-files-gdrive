using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoogleDriveDuplicateFinder
{
    class Program
    {
        static string[] Scopes =  { DriveService.Scope.Drive };// { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Google Drive Duplicate Finder";

        static void Main(string[] args)
        {
            UserCredential credential;
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Drive API service
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Console.WriteLine("Fetching all files (this may take a while)...");

            List<Google.Apis.Drive.v3.Data.File> allFiles = new List<Google.Apis.Drive.v3.Data.File>();
            string pageToken = null;
            const long SIZE_LIMIT = 1L * 1024L * 1024L; // 1 MB in bytes

            do
            {
                var request = service.Files.List();
                request.Fields = "nextPageToken, files(id, name, size, md5Checksum,parents)";
                request.PageSize = 1000; // Max allowed by Drive API
                request.PageToken = pageToken;
                // Exclude Google Docs formats, which don't have size or checksum
                request.Q = "mimeType != 'application/vnd.google-apps.folder' " +
                            "and not mimeType contains 'application/vnd.google-apps.'";
                var response = request.Execute();

                if (response.Files != null)
                    allFiles.AddRange(response.Files);

                pageToken = response.NextPageToken;
                Console.WriteLine($"Fetched {allFiles.Count} files so far...");
            }
            while (pageToken != null);

            Console.WriteLine($"Total files fetched: {allFiles.Count}");

            var filesWithChecksum = allFiles.Where(f => f.Size.HasValue && f.Size > SIZE_LIMIT && f.Md5Checksum != null).ToList();

            var duplicates = filesWithChecksum
                .GroupBy(f => f.Md5Checksum)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count == 0)
            {
                Console.WriteLine("No duplicates found.");
            }
            else
            {
                List<Google.Apis.Drive.v3.Data.File> filesToTrash = new List<Google.Apis.Drive.v3.Data.File>();
                Console.WriteLine("Duplicate files found:");
                foreach (var group in duplicates)
                {

                    Console.WriteLine($"\nMD5: {group.Key}");
                    int i = 0;
                    foreach (var file in group)
                    {
                        if(i++!=0)
                        filesToTrash.Add(file);
                        Console.WriteLine($"  • {file.Name} (ID: {file.Id}, Size: {file.Size} bytes)");
                    }
                }
                MoveToRecycleBin(service, filesToTrash);
            }

            Console.WriteLine("\nDone.");
        }


        static void MoveToRecycleBin(DriveService service, List<Google.Apis.Drive.v3.Data.File> filesToTrash)
        {
            int trashedCount = 0;
            Console.WriteLine("\n--- Trashing Duplicates ---");

            foreach (var fileToTrash in filesToTrash)
            {
                try
                {
                    // Set the trashed property to true to move the file to the Recycle Bin
                    var fileUpdate = new Google.Apis.Drive.v3.Data.File { Trashed = true };

                    var updateRequest = service.Files.Update(fileUpdate, fileToTrash.Id);
                    updateRequest.Fields = "id, trashed"; // Only request necessary fields back
                    updateRequest.Execute();

                    Console.WriteLine($"   ✅ Trashed: {fileToTrash.Name} (ID: {fileToTrash.Id})");
                    trashedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   🛑 Error trashing file {fileToTrash.Name} ({fileToTrash.Id}): {ex.Message}");
                }
            }
            Console.WriteLine($"\nSummary: Successfully moved {trashedCount} files to the Recycle Bin.");
        }


    }
}