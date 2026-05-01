using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Storylines.Services
{
    public class DebugLogger : ILogger
    {
        private const int MaxRecentEntries = 50;
        private readonly Queue<string> _recentEntries = new Queue<string>();
        private readonly object _lock = new object();

        public void Info(string message)
        {
            var entry = $"[INFO] {DateTime.Now:HH:mm:ss} {message}";
            Debug.WriteLine(entry);
            AddEntry(entry);
        }

        public void Warning(string message)
        {
            var entry = $"[WARN] {DateTime.Now:HH:mm:ss} {message}";
            Debug.WriteLine(entry);
            AddEntry(entry);
        }

        public void Error(string message, Exception ex = null)
        {
            var entry = $"[ERROR] {DateTime.Now:HH:mm:ss} {message}";
            if (ex != null)
                entry += $"\n  Exception: {ex.GetType().Name}: {ex.Message}\n  {ex.StackTrace}";
            Debug.WriteLine(entry);
            AddEntry(entry);
        }

        public IEnumerable<string> GetRecentEntries()
        {
            lock (_lock)
            {
                return _recentEntries.ToArray();
            }
        }

        private void AddEntry(string entry)
        {
            lock (_lock)
            {
                _recentEntries.Enqueue(entry);
                if (_recentEntries.Count > MaxRecentEntries)
                    _recentEntries.Dequeue();
            }
        }
    }
}
