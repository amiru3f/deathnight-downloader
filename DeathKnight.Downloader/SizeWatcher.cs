using System;
using System.IO;
using System.Timers;

namespace DeathKnight.Downloader
{
    public delegate void SizeChangedDelegate(long previousSize, long currentSize);

    public class SizeWatcher : IDisposable
    {
        public event SizeChangedDelegate SizeChanged;

        private long previousSize;
        private readonly string filePath;
        private readonly Timer timer;

        public SizeWatcher(string filePath, long timeIntervalMs = 1000)
        {
            this.filePath = filePath;
            timer = new Timer();
            timer.Interval = timeIntervalMs <= 0 ? timeIntervalMs : 1000;
            timer.Elapsed += OnWatch;
        }

        private void OnWatch(object sender, ElapsedEventArgs e)
        {
            if (File.Exists(filePath))
            {
                long existingFileSize = new FileInfo(filePath).Length;
                if (previousSize != existingFileSize)
                {
                    SizeChanged?.Invoke(previousSize, existingFileSize);
                }

                previousSize = existingFileSize;
            }
        }

        public void Watch()
        {
            timer.Stop();
            timer.AutoReset = true;
            timer.Start();
        }

        public void Dispose()
        {
            this.timer?.Stop();
            this.timer?.Dispose();
        }
    }
}