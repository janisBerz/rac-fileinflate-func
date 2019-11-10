using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
//using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Storage; // Namespace for Storage Client Library
using Microsoft.Azure.Storage.File; // Namespace for Azure Files
using System.Text;
using Microsoft.Azure.Storage.Blob;


namespace AzUnzipEverything
{
    public static class UnZipThis
    {
        [FunctionName("unZip")]
        public static async Task<string> UnZip(
            [ActivityTrigger] string name,
            //[HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            // Get app settings.
            var destinationStorage = Environment.GetEnvironmentVariable("DestinationStorageAccount");
            var destinationFileShare = Environment.GetEnvironmentVariable("DestinationFileShare");
            var sourceStorageConnectionString = Environment.GetEnvironmentVariable("SourceStorageAccount");

            //string name = "test.zip";
            var localZipFile = SetLocalPath(name);

            log.LogInformation($"Blob trigger function Processed blob:{name}");

            // Check whether the connection string can be parsed.
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(sourceStorageConnectionString, out storageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob
                // storage here.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("archived");
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(name);
                await cloudBlockBlob.DownloadToFileAsync(localZipFile, FileMode.Create);
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                log.LogInformation(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add an environment variable named 'SourceStorageAccount' with your storage " +
                    "connection string as a value.");
            }

            // Parse the connection string for the storage account.
            CloudStorageAccount cloudFileStorageAccount = CloudStorageAccount.Parse(destinationStorage);

            // Create a CloudFileClient object for credentialed access to Azure Files.
            CloudFileClient fileClient = cloudFileStorageAccount.CreateCloudFileClient();

            // Get a reference to the file share.
            CloudFileShare share = fileClient.GetShareReference(destinationFileShare);

            // Get a reference to the root directory for the share.
            CloudFileDirectory destinationDirectory = share.GetRootDirectoryReference();

            // Create file share if it doesn't exist.
            if (!await share.ExistsAsync())
            {
                await share.CreateAsync();
            }

            // Set slash character that is used to differentiate the zip entry from file.
            char slash = '/';
            //var localZipFile = SetLocalPath(name);

            try
            {
                // Filter out only zips.
                if (name.Split('.').Last().ToLower() == "zip")
                {

                    // write the zip to the disk
                    //await myBlob.DownloadToFileAsync(localZipFile, FileMode.Create);

                    // Opening zip file and specifying encoding for entries in the zip.
                    using (ZipArchive archive = ZipFile.Open(localZipFile, 0, Encoding.GetEncoding("ISO-8859-1")))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            /// How to tell if a “ZipArchiveEntry” is directory? - https://stackoverflow.com/questions/40223451/how-to-tell-if-a-ziparchiveentry-is-directory
                            // Check if th zip archive entry is a folder. FullName property for folders end with a "/" and Name property is empty.
                            if (slash == entry.FullName[entry.FullName.Length - 1] && 0 == entry.Name.Length)
                            {
                                // Create a folder if the zip archive entry is a folder.
                                log.LogInformation($"Now processing folder '{entry.FullName}'");
                                CloudFileDirectory EntryDestinationDirectory = destinationDirectory.GetDirectoryReference(entry.FullName);

                                if (! await EntryDestinationDirectory.ExistsAsync())
                                {
                                    await EntryDestinationDirectory.CreateAsync();
                                }
                            }

                            // Check if the zip archive entry is a file.
                            if (slash != entry.FullName.Length - 1 && 0 != entry.Name.Length)
                            {
                                log.LogInformation($"Now processing file '{entry.FullName}'");

                                //// Create buffer that is used to measure the deflated file size
                                byte[] buf = new byte[1024];
                                int size = 0;

                                // Open the entry to measure the size
                                using (var fileStream = entry.Open())
                                {
                                    int len;
                                    while ((len = fileStream.Read(buf, 0, buf.Length)) > 0)
                                    {
                                        size += len;
                                    }
                                }

                                //var threadCount = 1;

                                //if (size > 83886080)
                                //{
                                //    threadCount = 4;
                                //}
                                //else
                                //{
                                //    threadCount = 1;

                                //}

                                var requestOptions = new FileRequestOptions()
                                {
                                    ParallelOperationThreadCount = 8
                                };

                                // Open the zip entry for further processing
                                using (var fileStream = entry.Open())
                                {
                                    // Open memory stream by specifying the size
                                    using (var fileMemoryStream = new MemoryStream(size + 1024))
                                    //using (var fileMemoryStream = new MemoryStream())
                                    {
                                        
                                        fileStream.CopyTo(fileMemoryStream);
                                        fileMemoryStream.Position = 0;
                                        var destinationFile = destinationDirectory.GetFileReference(entry.FullName);
                                        await destinationFile.UploadFromStreamAsync(fileMemoryStream, null, requestOptions, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error! Something went wrong: {ex.Message}, {ex.StackTrace}");
            }

            //log.LogInformation($"cleaning up temp files {localZipFile}");
            //CleanUp(localZipFile);
            log.LogInformation($"Unzip of '{name}' completed!");
            return "OK!";
        }

        static string SetLocalPath(string fileName)
        {
            var workDir = Environment.GetEnvironmentVariable("TMP");
            var guid = Guid.NewGuid();
            var zipUnarchivedPath = $"{workDir}\\unarchived";
            var localZipFile = ($@"{zipUnarchivedPath}\{guid}_{fileName}");

            // Create temp folder
            if (!Directory.Exists(zipUnarchivedPath))
            {
                Directory.CreateDirectory(zipUnarchivedPath);
            }
            // Delete the file if it exists.
            if (File.Exists(localZipFile))
            {
                File.Delete(localZipFile);
            }
            return localZipFile;
        }
        static void CleanUp(string fileName)
        {
            var localZipFile = fileName;

            // Delete the file if it exists.
            if (File.Exists(localZipFile))
            {
                File.Delete(localZipFile);
            }
        }
    }
}