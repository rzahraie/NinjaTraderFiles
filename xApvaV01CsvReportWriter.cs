#region Using declarations
using System;
using System.Collections.Generic;
using System.IO;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class xApvaV01CsvReportWriter
    {
        private sealed class Report
        {
            public string Path;
            public string Header;
            public bool HeaderWritten;
            public Func<string> BodyFactory;
        }

        private readonly List<Report> reports =
            new List<Report>();

        public void AddReport(
            string path,
            string header,
            Func<string> bodyFactory)
        {
            reports.Add(new Report
            {
                Path = path,
                Header = header,
                HeaderWritten = false,
                BodyFactory = bodyFactory
            });
        }

        public void DeleteExistingFiles()
        {
            foreach (Report report in reports)
            {
                if (!string.IsNullOrEmpty(report.Path) &&
                    File.Exists(report.Path))
                {
                    File.Delete(report.Path);
                }

                report.HeaderWritten = false;
            }
        }

        public void WriteAll()
        {
            foreach (Report report in reports)
            {
                if (report.BodyFactory == null)
                    continue;

                if (!report.HeaderWritten)
                {
                    File.AppendAllText(
                        report.Path,
                        report.Header + Environment.NewLine);

                    report.HeaderWritten = true;
                }

                string body = report.BodyFactory();

                if (!string.IsNullOrEmpty(body))
                    File.AppendAllText(report.Path, body);
            }
        }
    }
}