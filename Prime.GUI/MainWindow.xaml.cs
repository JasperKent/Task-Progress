using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Prime.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cancellationSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationSource?.Cancel();
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cancel.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;

                _cancellationSource = new CancellationTokenSource();

                Output.Items.Add("Calculating...");

                var lo = ulong.Parse(Lo.Text, NumberStyles.AllowThousands);
                var hi = ulong.Parse(Hi.Text, NumberStyles.AllowThousands);

                var progress = new Progress<int>();

                progress.ProgressChanged += (_, value) => ProgressBar.Value = Math.Max (value, ProgressBar.Value);

                Task<int>[] tasks =
                {
                    Task.Run(() => PrimeCount(lo, hi, _cancellationSource.Token, progress)),
                    Task.Run(() => PrimeCount(lo, hi, _cancellationSource.Token, progress)),
                    Task.Run(() => PrimeCount(lo, hi, _cancellationSource.Token, progress))
                };

                var finisher = await Task.WhenAny(tasks);

                _cancellationSource.Cancel();

                Output.Items.Clear();
             
                ReportResult(finisher, tasks, lo, hi);
            }
            catch (Exception ex)
            {
                Output.Items.Clear();
                Output.Items.Add(ex.Message);
            }
            finally
            {
                Cancel.IsEnabled = false;
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ReportResult(Task<int> finisher, Task<int>[] tasks, ulong lo, ulong hi)
        {
            Output.Items.Add($"Task {Array.IndexOf(tasks, finisher)} reported {finisher.Result:N0} primes between {lo:N0}, and {hi:N0}");
        }

        private static int PrimeCount(ulong lo, ulong hi, CancellationToken token, IProgress<int> progress)
        {
            int count = 0;

            var root = Math.Sqrt(hi);
            var rand = new Random();

            var primes = PrimeList.Primes.TakeWhile(p => p <= root).OrderBy(x => rand.Next()).ToArray();

            var factor = 100.0 / (hi - lo);
            var oldPercentage = 0;

            for (ulong num = lo; num <= hi; ++num)
            {
                token.ThrowIfCancellationRequested();

                if (IsPrime(num))
                    ++count;

                var percentage = (int)((num - lo) * factor);

                if (percentage > oldPercentage)
                {
                    progress.Report(percentage);
                    oldPercentage = percentage;
                }
            }

            return count;

            bool IsPrime(ulong num)
            {
                foreach (ulong x in primes)
                {
                    if (num % x == 0)
                        return false;
                }

                return true;
            }
        }
    }
}
