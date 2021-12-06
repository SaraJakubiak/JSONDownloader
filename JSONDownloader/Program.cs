using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace JSONDownloader {

    using JSONInfo = ConcurrentDictionary<string, string>;
    

    /// <summary>
    /// Simple program that downloads and saves provided JSON URLs to a provided directory.
    /// </summary>
    public class Program {


        static void Main(string[] args) {

            // get JSON URLs
            Console.Write("Enter JSON URLs (seperated by semicolon): ");
            string strUrls = Console.ReadLine();

            // get save folder
            Console.Write("Enter target save folder: ");
            string saveDir = Console.ReadLine();

            // get valid URLs and save filepaths
            JSONInfo jsonInfo = GenerateJSONInfo(strUrls, saveDir);

            if (!jsonInfo.IsEmpty) {

                FileSystem fileSystem = new FileSystem();

                try {
                    // check name provided is valid, otherwise default to cwd
                    fileSystem.Path.GetDirectoryName(saveDir);

                    // create the directory if it doesn't exist
                    Directory.CreateDirectory(saveDir);

                } catch (ArgumentException) {   
                    Console.WriteLine($"\t[info] '{saveDir}' is not a valid folder name; using current directory instead.");
                    saveDir = ".";
                } catch (PathTooLongException) {
                    Console.WriteLine($"\t[info] '{saveDir}' is too long; using current directory instead.");
                    saveDir = ".";
                }
                    
                // download and save each URL asynchronously
                using (HttpClient client = new HttpClient()) {

                    Task[] tasks = jsonInfo.Select(
                        pair => DownloadAsync(pair.Value, pair.Key, client, fileSystem))
                        .ToArray();
                    Task.WaitAll(tasks);
                    
                }
                
                Console.WriteLine("Done.");

            } else {
                Console.WriteLine("No files to download.");
            }
        }


        /// <summary>
        /// Get valid URLs and path to save their data to.
        /// </summary>
        /// <param name="strUrls">Input string of URLs</param>
        /// <param name="saveDir">Directory to save the file to</param>
        /// <returns>Dictionary of valid save filepaths and URLs</returns>
        public static JSONInfo GenerateJSONInfo(string strUrls, string saveDir) {

            // split the URLs string
            string[] urls = strUrls.Split(';');

            // remove duplicates
            urls = urls.Distinct().ToArray();

            // create a mapping between filepaths and URLs
            JSONInfo dict = new JSONInfo();

            // validate URLs and generate filenames
            Parallel.ForEach(urls, url => {
                if (IsValidURL(url)) {

                    // generate save filepath
                    string name = GetName(url);
                    string filepath = GetFullPath(name, saveDir);

                    // if filename already exists, propose new one 
                    int copyCount = 2;
                    while (!dict.TryAdd(filepath, url)) {
                        filepath = GetFullPath(name, saveDir, copyCount.ToString());
                        copyCount++;
                    }

                    // inform if using non-default name
                    if (copyCount > 2) {
                        Console.WriteLine($"\t[info] '{url}' will be saved to {filepath} due to name duplication");
                    }

                } else {
                    Console.WriteLine($"\t[info] Skipping '{url}'; invalid URL");
                }
            });

            return dict;
        }

        
        /// <summary>
        /// Download and save the JSON.
        /// </summary>
        /// <param name="url">URL to JSON to download</param>
        /// <param name="path">Filepath to save to</param>
        /// <param name="client">HTTP client to use</param>
        /// <param name="fileSystem">File system to use</param>
        public static async Task DownloadAsync(string url, string path, HttpClient client, IFileSystem fileSystem) {
            try {
                // download the data
                String data = await client.GetStringAsync(url);

                // save to file
                await fileSystem.File.WriteAllTextAsync(path, data).ContinueWith(task =>
                    Console.WriteLine($"\t[success] Saved '{url}' to {path}")
                );

            } catch (HttpRequestException e) {
                Console.WriteLine($"\t[info] Skipping '{url}'; bad response ({e.StatusCode})");
            }
        }


        /// <summary>
        /// Returns whether given string is a valid URL.
        /// NOTE: this solution does not allow URLs missing HTTP/HTTPS
        /// (like google.com)
        /// </summary>
        /// <param name="url">URL address to be validated</param>
        /// <returns>Whether the URL is valid</returns>
        public static bool IsValidURL(string url) {

            // check for valid URI string formatting
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) {

                // ensure the scheme is http or https
                Uri uri = new Uri(url);
                return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            } else {
                return false;
            }
        }


        /// <summary>
        /// Generate valid name based on the URL.
        /// If JSON file name is in the URL, the name is returned.
        /// Otherwise, returns name generated based on the URL address.
        /// </summary>
        /// <param name="url">Validated URL address</param>
        /// <returns>Name without extension</returns>
        public static string GetName(string url) {

            string name;

            if (url.ToLower().EndsWith(".json")) {
                Uri uri = new Uri(url);
                name = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
            } else {
                // since the url has been verified and is either http or https
                // split on double slashes and take later part
                name = url.Split("//")[1];
                name = name.Replace("/", ".");
            }

            // remove illegal filename characters
            name = string.Concat(name.Split(Path.GetInvalidFileNameChars()));

            return name;
        }


        /// <summary>
        /// Construct full path to a file
        /// </summary>
        /// <param name="name">Name of the file without extension</param>
        /// <param name="saveDir">Directory to save the file to</param>
        /// <param name="copyCount">Additional identifier to be included in the filename</param>
        /// <returns>Full filepath</returns>
        public static string GetFullPath(string name, string saveDir, string copyCount="") {
            name = name.EndsWith(".") ? name.Remove(name.Length - 1): name;
            string filename = name + copyCount + ".json";
            return Path.Combine(new string[] {saveDir, filename});
        }
    }
}
