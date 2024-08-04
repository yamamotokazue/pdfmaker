using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Document = iTextSharp.text.Document;
using Path = System.IO.Path;

namespace tctool.ViewModels
{
    internal partial class PdfRemakerPageViewModel : ViewModelBase
    {
        public PdfRemakerPageViewModel()
        {
        }

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private string _msg = "";

        [ObservableProperty]
        private bool _fileSelector = true;

        [ObservableProperty]
        private bool _pDFAnalyse = false;

        //https://www.cnblogs.com/cbaa/p/17378609.html
        public ObservableCollection<fileInfo> fileLists = new ObservableCollection<fileInfo>();
        public ObservableCollection<fileInfo> FileLists
        {
            get => fileLists;
            set => SetProperty(ref fileLists, value);
        }

        public ObservableCollection<pdfPageInfo> pdfPageLists = new ObservableCollection<pdfPageInfo>();
        public ObservableCollection<pdfPageInfo> PdfPageLists
        {
            get => pdfPageLists;
            set => SetProperty(ref pdfPageLists, value);
        }

        [RelayCommand]
        private async Task TriggerFileSelector()
        {
            Msg = string.Empty;

            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "選擇一個或多個PDF檔案",
                FileTypeFilter = new[] { FilePickerFileTypes.Pdf },
                AllowMultiple = true
            });
            if (files is not null)
            {
                if (files.Count() > 10)
                {
                    Msg = "最多僅能選擇10個PDF檔案";
                    return;
                }
                var listFiles = new List<fileInfo>();
                int i = 0;
                foreach (var file in files.OrderBy(p => p.Name.Length).ThenBy(p => p.Name))
                {
                    i++;
                    listFiles.Add(
                        new fileInfo()
                        {
                            Name = file.Name,
                            Path = file.Path.LocalPath,
                            Seq = i,
                        }
                    );
                }
                FileLists = new ObservableCollection<fileInfo>(listFiles);
                PDFAnalyse = true;
            }
            else
            {
                Msg = "尚未選擇PDF檔案";
                PDFAnalyse = false;
            }
        }

        [RelayCommand]
        private async Task TriggerPDFAnalyse()
        {
            Msg = string.Empty;
            if (FileLists is null) return;
            if (!checkFileExists()) return;

            var listPdfPageInfo = new List<pdfPageInfo>();

            var seq = 1;

            var tmpFilelist = FileLists.OrderBy(p => p.Seq).ToList();
            tmpFilelist.ForEach(p =>
            {
                p.Seq = seq++;
            });
            FileLists = new ObservableCollection<fileInfo>(tmpFilelist);

            foreach (var row in FileLists)
            {
                PdfReader reader = new PdfReader(row.Path);
                // 获得文档页数
                int n = reader.NumberOfPages;

                for (var i = 1; i <= n; i++)
                {
                    listPdfPageInfo.Add(new pdfPageInfo
                    {
                        Seq = row.Seq,
                        Page = i,
                        Retain = true,
                        Rotate = 0,
                        NewPage = 999,
                    });
                }
            }
            PdfPageLists = new ObservableCollection<pdfPageInfo>(listPdfPageInfo);
            FileSelector = false;
        }

        [RelayCommand]
        private async Task TriggerPDFMake()
        {
            Msg = string.Empty;
            if (!checkFileExists()) return;
            if (!checkPageInfoData()) return;

            var data = PdfPageLists.ToList();

            //濾掉不保留的
            data = data.Where(p => p.Retain == true).OrderBy(p => p.NewPage).ThenBy(p => p.Seq).ThenBy(p => p.Page).ToList();
            if (data.Count() == 0) return;

            string newFile = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}-{FileLists.Select(p => p.Name).First()}";
            Document document = new Document();
            using (Stream resultPDFOutputStream = new FileStream(
                path: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), newFile),
                mode: FileMode.Create))
            {
                PdfCopy writer = new PdfCopy(document, resultPDFOutputStream);
                document.Open();

                foreach (var row in data)
                {
                    var filePath = FileLists.Where(p => p.Seq == row.Seq).Select(p => p.Path).FirstOrDefault();
                    if (filePath is null) continue;
                    using (var reader = new PdfReader(filePath))
                    {
                        reader.ConsolidateNamedDestinations();

                        PdfDictionary page = reader.GetPageN(row.Page);
                        page.Put(PdfName.Rotate, new PdfNumber(row.Rotate));

                        // Import the page
                        PdfImportedPage importedPage = writer.GetImportedPage(reader, row.Page);
                        writer.AddPage(importedPage);
                    }
                }

                writer.Close();
                document.Close();
            }
            Process.Start("explorer.exe", $"/select,\"{newFile}\"");
        }

        private bool checkFileExists()
        {
            //TODO: 檢查檔案是否存在
            foreach (var row in FileLists)
            {
                if (!File.Exists(row.Path))
                {
                    Msg = $"{row.Name} 檔案不存在";
                    return false;
                }
            }
            return true;
        }
        private bool checkPageInfoData()
        {
            int[] validAngles = { 0, 90, 180, 270 };
            bool containsAny = PdfPageLists.Any(p => !validAngles.Contains(p.Rotate));
            if (containsAny)
            {
                Msg = $"設定資料中，旋轉角度僅接受 0, 90, 180, 270 之數值";
                return false;
            }
            return true;
        }
    }

    public class fileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Seq { get; set; }
    }
    public class pdfPageInfo
    {
        public int Seq { get; set; }
        public int Page { get; set; }
        public int Rotate { get; set; }
        public bool Retain { get; set; }
        public int NewPage { get; set; }
    }
}
