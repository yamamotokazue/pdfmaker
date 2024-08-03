using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace tctool.ViewModels
{
    internal partial class PdfRemakerPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _fileName = "";

        //https://www.cnblogs.com/cbaa/p/17378609.html
        public ObservableCollection<fileInfo> fileLists = new ObservableCollection<fileInfo>();
        public ObservableCollection<fileInfo> FileLists
        {
            get => fileLists;
            set => SetProperty(ref fileLists, value);
        }

        [RelayCommand]
        private async Task TriggerFileSelector()
        {
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
                var listFiles = new List<fileInfo>();
                foreach (var file in files)
                {
                    listFiles.Add(
                        new fileInfo()
                        {
                            Name = file.Name,
                            Path = file.Path.LocalPath,
                        }
                    );
                }
                FileLists = new ObservableCollection<fileInfo>(listFiles);
            }
        }
    }

    public class fileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
