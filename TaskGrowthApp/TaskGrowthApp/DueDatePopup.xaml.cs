using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TaskGrowthApp.Views
{
    /// <summary>
    /// 黒背景の自前カレンダーで期限日を選択するポップアップ。
    /// ShowDialog() == true → SelectedDate に結果（null = 期限なし）
    /// </summary>
    public partial class DueDatePopup : Window
    {
        public DateTime? SelectedDate { get; private set; }

        private DateTime _displayMonth;
        private DateTime? _currentSelected;

        // ─── 色定数 ─────────────────────────────────────────────────
        private static readonly SolidColorBrush BgNormal    = new(Color.FromRgb(0x11, 0x11, 0x11));
        private static readonly SolidColorBrush BgHover     = new(Color.FromRgb(0x33, 0x33, 0x33));
        private static readonly SolidColorBrush BgSelected  = new(Color.FromRgb(0xFF, 0xD7, 0x00)); // Gold
        private static readonly SolidColorBrush BgToday     = new(Color.FromRgb(0x1a, 0x3a, 0x1a));
        private static readonly SolidColorBrush BgOverdue   = new(Color.FromRgb(0x3a, 0x0a, 0x0a));

        private static readonly SolidColorBrush FgNormal    = Brushes.White;
        private static readonly SolidColorBrush FgSelected  = Brushes.Black;
        private static readonly SolidColorBrush FgOtherMonth= new(Color.FromRgb(0x55, 0x55, 0x55));
        private static readonly SolidColorBrush FgToday     = Brushes.Lime;
        private static readonly SolidColorBrush FgSun       = new(Color.FromRgb(0xFF, 0x66, 0x66));
        private static readonly SolidColorBrush FgSat       = new(Color.FromRgb(0x66, 0xAA, 0xFF));
        private static readonly SolidColorBrush FgOverdue   = new(Color.FromRgb(0xFF, 0x55, 0x55));

        // ─── コンストラクタ ───────────────────────────────────────────
        public DueDatePopup(DateTime? current = null)
        {
            InitializeComponent();
            _currentSelected = current;
            _displayMonth    = new DateTime((current ?? DateTime.Today).Year,
                                            (current ?? DateTime.Today).Month, 1);

            BuildWeekHeader();
            RebuildCalendar();
        }

        // ─── 曜日ヘッダー（一度だけ生成） ────────────────────────────
        private void BuildWeekHeader()
        {
            string[] days    = { "日", "月", "火", "水", "木", "金", "土" };
            Brush[]  dayColors = { FgSun, FgNormal, FgNormal, FgNormal, FgNormal, FgSat, FgSat };

            foreach (var i in System.Linq.Enumerable.Range(0, 7))
            {
                WeekHeader.Children.Add(new TextBlock
                {
                    Text                = days[i],
                    Foreground          = dayColors[i],
                    FontSize            = 10,
                    FontWeight          = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Margin              = new Thickness(0, 0, 0, 2)
                });
            }
        }

        // ─── カレンダー再描画 ─────────────────────────────────────────
        private void RebuildCalendar()
        {
            MonthLabel.Text = _displayMonth.ToString("yyyy年 M月");
            DayGrid.Children.Clear();

            var today      = DateTime.Today;
            var firstDay   = _displayMonth;
            int startDow   = (int)firstDay.DayOfWeek; // 0=日
            var cursor     = firstDay.AddDays(-startDow);

            for (int i = 0; i < 42; i++, cursor = cursor.AddDays(1))
            {
                var date         = cursor;
                bool isThisMonth = date.Month == _displayMonth.Month;
                bool isToday     = date == today;
                bool isSelected  = _currentSelected.HasValue && date == _currentSelected.Value.Date;
                bool isOverdue   = isThisMonth && date < today;
                int  dow         = (int)date.DayOfWeek;

                // ─── ボタン本体 ─────────────────────────────────────
                var btn = new Border
                {
                    Height          = 26,
                    Background      = isSelected ? BgSelected
                                    : isToday    ? BgToday
                                    : BgNormal,
                    BorderBrush     = isToday    ? Brushes.Lime : Brushes.Transparent,
                    BorderThickness = isToday    ? new Thickness(1) : new Thickness(0),
                    CornerRadius    = new CornerRadius(3),
                    Margin          = new Thickness(1),
                    Cursor          = isThisMonth ? Cursors.Hand : Cursors.Arrow
                };

                var fg = isSelected   ? FgSelected
                       : !isThisMonth ? FgOtherMonth
                       : isToday      ? FgToday
                       : isOverdue    ? FgOverdue
                       : dow == 0     ? FgSun
                       : dow == 6     ? FgSat
                       : FgNormal;

                btn.Child = new TextBlock
                {
                    Text                = date.Day.ToString(),
                    Foreground          = fg,
                    FontSize            = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment   = VerticalAlignment.Center,
                    FontWeight          = isToday ? FontWeights.Bold : FontWeights.Normal
                };

                // ホバー
                btn.MouseEnter += (_, _) =>
                {
                    if (!isSelected) btn.Background = BgHover;
                };
                btn.MouseLeave += (_, _) =>
                {
                    if (!isSelected)
                        btn.Background = isToday ? BgToday : BgNormal;
                };

                // クリック（当月のみ）
                if (isThisMonth)
                {
                    btn.MouseLeftButtonDown += (_, _) =>
                    {
                        _currentSelected = date;
                        SelectedDate     = date;
                        DialogResult     = true;
                    };
                }

                DayGrid.Children.Add(btn);
            }
        }

        // ─── ナビゲーション ───────────────────────────────────────────
        private void OnPrevMonth(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            RebuildCalendar();
        }

        private void OnNextMonth(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(1);
            RebuildCalendar();
        }

        private void OnToday(object sender, RoutedEventArgs e)
        {
            SelectedDate = DateTime.Today;
            DialogResult = true;
        }

        private void OnClear(object sender, RoutedEventArgs e)
        {
            SelectedDate = null;
            DialogResult = true;
        }

        private void OnClose(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
