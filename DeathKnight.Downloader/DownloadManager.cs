using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DeathKnight.Downloader
{
    public class DownloadManager
    {
        private readonly IProgressListener progressListener;
        private readonly NLog.ILogger logger;

        #region field(s)
        private readonly string downloadPath;
        private readonly string downloadDirectory;
        private readonly string downloadUrl;
        #endregion

        public DownloadManager(IProgressListener progressListener,
            string downloadUrl,
            string downloadDirectory,
            string downloadedFileName,
            NLog.ILogger logger)
        {
            this.logger = logger;
            this.progressListener = progressListener;

            this.downloadUrl = downloadUrl;
            this.downloadDirectory = downloadDirectory;
            this.downloadPath = Path.Combine(downloadDirectory, downloadedFileName);
        }

        public async Task StartDownloadAsync()
        {
            logger?.Info("downloading files from " + downloadUrl);

            int tryAttempts = 3;

            bool found = false;
            bool fileFoundAtServer = false;

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(downloadUrl);
            HttpWebResponse response = null;
            int tries = 0;

            long downloadFileSize = 0;
            long startOfRange;
            long endOfRange = 0;

            #region trying to fetch download file and size from server
            while (tries++ < tryAttempts)
            {
                try
                {
                    response = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                    logger?.Info("response code: " + response.StatusCode);

                    if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.InternalServerError && response.StatusCode != HttpStatusCode.NotFound)
                    {
                        logger?.Error("could not connect to server, status code:" + response.StatusCode);
                        await Task.Delay(10000);
                        progressListener?.Retrying(tries, response.StatusCode);

                        continue;
                    }

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        logger?.Info("file found, size: " + response.ContentLength);
                        fileFoundAtServer = true;
                        downloadFileSize = response.ContentLength;
                        endOfRange = response.ContentLength - 1;

                        progressListener?.FileFound(downloadFileSize);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger?.Error(ex, ex.Message);
                    progressListener?.Retrying(tries, response?.StatusCode ?? HttpStatusCode.NoContent);
                }
            }

            if (!fileFoundAtServer)
            {
                progressListener?.FileNotFound();
                logger?.Error("file not found at server");
                return;
            }

            httpWebRequest.Abort();
            response.Dispose();
            #endregion

            #region download and unzip
            logger?.Info("started downloading and unzipping from server.");

            while (!found)
            {
                startOfRange = 0;
                long existingFileLength;

                //first check.
                #region first check
                if (System.IO.File.Exists(downloadPath))
                {
                    existingFileLength = new FileInfo(downloadPath).Length;
                    found = true;
                    try
                    {
                        if (existingFileLength == downloadFileSize)
                        {
                            progressListener?.Success();
                            break;
                        }
                        else
                        {
                            logger?.Info("file exists but size differ, it seems that resuming needed");
                            startOfRange = new System.IO.FileInfo(downloadPath).Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        File.Delete(downloadPath);
                        found = false;
                        logger?.Error(ex, ex.Message);
                        progressListener?.Error(ex);
                    }
                }
                #endregion

                try
                {

                    httpWebRequest = WebRequest.CreateHttp(downloadUrl);
                    progressListener?.GoingToResumeDownload(httpWebRequest, startOfRange, endOfRange);

                    httpWebRequest.ContinueTimeout = 10000;
                    logger?.Info("starting download");

                    response = await httpWebRequest.GetResponseAsync() as HttpWebResponse;

                    logger?.Info("starting download aft");

                    using (FileStream stream = new FileStream(downloadPath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        stream.Seek(startOfRange, SeekOrigin.Begin);

                        using (var fileSizeWatcher = new SizeWatcher(downloadPath))
                        {
                            logger?.Info("going to copy stream");
                            fileSizeWatcher.SizeChanged += (previousSize, currentSize) =>
                            {
                                double progress = currentSize * 1d / downloadFileSize;
                                progressListener?.Progress(progress, currentSize);
                            };

                            fileSizeWatcher.Watch();
                            await response.GetResponseStream().CopyToAsync(stream);
                            logger?.Info("going to copy stream aft");
                        }
                        stream.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger?.Warn("error occured in downloading file from " + downloadDirectory + ", retrying in 10 seconds");
                    logger?.Error(ex, ex.Message);
                    progressListener?.Error(ex);
                    await Task.Delay(10000);
                }
            }

            #endregion
        }
    }
}