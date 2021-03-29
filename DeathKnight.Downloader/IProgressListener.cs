using System;
using System.Net;

namespace DeathKnight.Downloader
{
    public interface IProgressListener
    {
        void Retrying(int tries, HttpStatusCode statusCode);
        void FileFound(long contentLength);
        void FileNotFound();
        void Success();
        void GoingToResumeDownload(HttpWebRequest request, long startOfRange, long endOfRange);
        void Error(Exception ex);
        void Progress(double progress, long currentSize);
    }
}
