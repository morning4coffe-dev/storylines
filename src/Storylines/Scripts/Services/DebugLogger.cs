using Storylines.Scripts.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Storylines.Scripts.Services
{
    public class DebugLogger : ILogger
    {
        private const int MaxRecentEntries = 50;
        private readonly Queue<string> _recentEntries = new Queue<string>();

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

        public IEnumerable<string> GetRecentEntries() => _recentEntries;

        private void AddEntry(string entry)
        {
            _recentEntries.Enqueue(entry);
            if (_recentEntries.Count > MaxRecentEntries)
                _recentEntries.Dequeue();
        }
    }
}
