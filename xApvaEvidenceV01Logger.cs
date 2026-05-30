using System;
using System.IO;
using System.Text;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    // Instrumentation-only CSV output for APVA Evidence v0.1 rows.
    // The caller supplies the full output path so research data stays outside source folders.
    public sealed class ApvaEvidenceV01Logger : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly StreamWriter writer;
        private bool disposed;

        public ApvaEvidenceV01Logger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(
                    "A full output file path is required.",
                    nameof(filePath));

            FilePath = filePath;

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            bool fileExists = File.Exists(filePath);

            writer = new StreamWriter(
                filePath,
                true,
                new UTF8Encoding(false));

            if (!fileExists)
            {
                writer.WriteLine(ApvaEvidenceV01Formatter.CsvHeader());
                writer.Flush();
            }
        }

        public string FilePath { get; }

        public long RowsWritten { get; private set; }

        public void Append(ApvaEvidenceRow row)
        {
            if (row == null)
                return;

            lock (syncRoot)
            {
                ThrowIfDisposed();

                string line = ApvaEvidenceV01Formatter.ToCsv(row);
                writer.WriteLine(line);
                RowsWritten++;
            }
        }

        public void Flush()
        {
            lock (syncRoot)
            {
                ThrowIfDisposed();
                writer.Flush();
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;

                writer.Dispose();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ApvaEvidenceV01Logger));
        }
    }
}
