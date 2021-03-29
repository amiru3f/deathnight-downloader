using System;
using System.Net;
using System.Threading.Tasks;

namespace DeathKnight.Downloader.Console
{
    class Program : IProgressListener
    {
        static async Task Main(string[] args)
        {
            DeathKnight.Downloader.DownloadManager manager = new DownloadManager(new Program(), "https://address/to/file.extension", "/directory/for/saving", "filename.extension", null);
            await manager.StartDownloadAsync();
        }

        public void Error(Exception ex)
        {
        }

        public void FileFound(long contentLength)
        {
        }

        public void FileNotFound()
        {
        }

        public void GoingToResumeDownload(HttpWebRequest request, long startOfRange, long endOfRange)
        {
        }

        public void Progress(double progress, long currentSize)
        {
            System.Console.WriteLine(progress);
        }

        public void Retrying(int tries, HttpStatusCode statusCode)
        {
        }

        public void Success()
        {
        }
    }
}
