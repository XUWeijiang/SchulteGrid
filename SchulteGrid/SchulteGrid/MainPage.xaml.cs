using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SchulteGrid
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int s_difficulty = 5;  // 5 * 5
        private readonly Random _random = new Random();

        private List<Button> _buttons = new List<Button>();
        private int _currentProgress = 0;
        private DispatcherTimer _timer = new DispatcherTimer()
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        private DateTime _begin;
        private bool _highlight = false;

        public MainPage()
        {
            this.InitializeComponent();
            _layoutRoot.Children.Clear();
            for (int i = 0; i < s_difficulty * s_difficulty; ++i)
            {
                Button b = new Button();
                b.BorderBrush = new SolidColorBrush(Windows.UI.Colors.DarkGray);
                b.Click += B_Click;
                b.Background = new SolidColorBrush(Windows.UI.Colors.White);
                b.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                _layoutRoot.Children.Add(b);
                _buttons.Add(b);
            }
            _layoutRoot.SizeChanged += _layoutRoot_SizeChanged;
            _timer.Tick += _timer_Tick;
            Restart();
        }

        private void _timer_Tick(object sender, object e)
        {
            double cost = (DateTime.Now - _begin).TotalSeconds;
            _secondLabel.Text = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", cost);
        }

        private async void B_Click(object sender, RoutedEventArgs e)
        {
            var b = ((Button)sender);
            if (Convert.ToInt32(b.Tag) == _currentProgress + 1)
            {
                await Play("correct");
                if (_highlight)
                {
                    b.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                }
                _currentProgress++;
                if (_currentProgress == 1)
                {
                    _begin = DateTime.Now;
                    _timer.Start();
                }
                else if (_currentProgress == s_difficulty * s_difficulty) // done
                {
                    _timer.Stop();
                    await Play("success");
                    MessageDialog dialog = new MessageDialog(string.Format(CultureInfo.InvariantCulture, "完成！花费时间：{0}秒", _secondLabel.Text));
                    dialog.Commands.Add(new UICommand("再来一次", new UICommandInvokedHandler(x => Restart())));
                    await dialog.ShowAsync();
                }
            }
            else
            {
                await Play("error");
            }
        }

        private void _layoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double size = Math.Min(e.NewSize.Width, e.NewSize.Height);
            double initleft = (e.NewSize.Width - size) / 2;
            double initTop = (e.NewSize.Height - size) / 2;

            double perButtonSize = size / s_difficulty;

            for (int i = 0; i < s_difficulty; ++i)
            {
                for (int j = 0; j < s_difficulty; ++j)
                {
                    var b = _buttons[i * s_difficulty + j];
                    b.Width = b.Height = perButtonSize;
                    b.FontSize = b.Width / 2;
                    Canvas.SetLeft(b, initleft + perButtonSize * j);
                    Canvas.SetTop(b, initTop + perButtonSize * i);
                }
            }
        }
        private async void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProgress == 0)
            {
                Restart();
            }
            else
            {
                MessageDialog dialog = new MessageDialog("确定要重新开始吗？");
                dialog.Commands.Add(new UICommand("确定", new UICommandInvokedHandler(x => Restart())));
                dialog.Commands.Add(new UICommand("取消", new UICommandInvokedHandler(x => { })));
                await dialog.ShowAsync();
            }
        }

        private void RandomizeInPlace(IList<int> source)
        {
            for (int i = 0; i < source.Count; ++i)
            {
                int index = _random.Next(i, source.Count);
                if (index != i)
                {
                    var tmp = source[i];
                    source[i] = source[index];
                    source[index] = tmp;
                }
            }
        }
        private void Restart()
        {
            _timer.Stop();
            _secondLabel.Text = "0.00";
            _currentProgress = 0;
            var numbers = Enumerable.Range(1, s_difficulty * s_difficulty).ToArray();
            RandomizeInPlace(numbers);

            for (int i = 0; i < _buttons.Count; ++i)
            {
                _buttons[i].Content = numbers[i].ToString(CultureInfo.InvariantCulture);
                _buttons[i].Tag = numbers[i];
                _buttons[i].Background = new SolidColorBrush(Windows.UI.Colors.White);
            }
        }

        private void RefreshHighlight()
        {
            for (int i = 0; i < _buttons.Count; ++i)
            {
                if (Convert.ToInt32(_buttons[i].Tag) <= _currentProgress)
                {
                    _buttons[i].Background = _highlight ? new SolidColorBrush(Windows.UI.Colors.Green) : new SolidColorBrush(Windows.UI.Colors.White);
                }
            }
            _nameLabel.Foreground = _highlight ? new SolidColorBrush(Windows.UI.Colors.Green) : new SolidColorBrush(Windows.UI.Colors.White); 
        }

        private void HighlightToggle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _highlight = !_highlight;
            RefreshHighlight();
        }
        private async Task Play(string name)
        {
            string source = string.Format("ms-appx:///Assets/{0}.mp3", name);
            _player.Source = new Uri(source);

            await _player.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _player.Stop();
                _player.Play();
            });
        }
    }
}
