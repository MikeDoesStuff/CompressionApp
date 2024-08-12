using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Lame;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using NAudio.Wave.SampleProviders;
using Path = System.IO.Path;

namespace CompressMyCall;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }


    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            FilePathTextBox.Text = openFileDialog.FileName;
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
        {
            MessageBox.Show("Please select a file first.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SubmitButton.IsEnabled = false;
        ProgressBar.Value = 0;

        try
        {
            await CompressFileAsync(FilePathTextBox.Text);
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }



    private async Task CompressFileAsync(string inputFilePath)
    {
        string outputFilePath = Path.ChangeExtension(inputFilePath, null) + "_COMPRESSED.mp3";

        try
        {
            using (var reader = new AudioFileReader(inputFilePath))
            using (var writer = new LameMP3FileWriter(outputFilePath, reader.WaveFormat, 128))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                long totalBytes = reader.Length;
                long readBytes = 0;

                while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await writer.WriteAsync(buffer, 0, bytesRead);
                    readBytes += bytesRead;
                    int progressPercentage = (int)((double)readBytes / totalBytes * 100);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProgressBar.Value = progressPercentage;
                    });
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusTextBlock.Text = "Compression complete!";
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                MessageBox.Show($"An error occurred during compression: {ex.Message}", "Compression Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
    
    private async Task CompressFileAsyncVOIP(string inputFilePath)
{
    string outputFilePath = Path.ChangeExtension(inputFilePath, null) + "_COMPRESSED.mp3";

    try
    {
        using (var reader = new AudioFileReader(inputFilePath))
        {
            // Convert to mono
            var monoSampleProvider = reader.ToMono();

            // Resample to 8kHz
            var resampler = new WdlResamplingSampleProvider(monoSampleProvider, 8000);
            var resampledProvider = new SampleToWaveProvider(resampler);

            var waveFormat = new WaveFormat(8000, 16, 1);

            using (var writer = new LameMP3FileWriter(outputFilePath, waveFormat, 16))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;
                long totalBytes = reader.Length;
                long readBytes = 0;

                while ((bytesRead = resampledProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await writer.WriteAsync(buffer, 0, bytesRead);
                    readBytes += bytesRead;
                    int progressPercentage = (int)((double)readBytes / totalBytes * 100);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ProgressBar.Value = progressPercentage;
                    });
                }
            }
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusTextBlock.Text = "Compression complete!";
        });
    }
    catch (Exception ex)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            MessageBox.Show($"An error occurred during compression: {ex.Message}", "Compression Error", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}
}