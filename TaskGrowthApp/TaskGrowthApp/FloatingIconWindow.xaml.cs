using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace TaskGrowthApp
{
    public partial class FloatingIconWindow : Window
    {
        private const double EdgeMargin = 16.0;
        private const double FloatAnimationHeight = 3.0;

        private bool _isDragging = false;
        private Point _dragOffset;

        private Storyboard? _floatSb;
        private Storyboard? _pulseSb;

        private PlayerStatus _playerStatus;

        public FloatingIconWindow(PlayerStatus status)
        {
            InitializeComponent();
            _playerStatus = status;
            LevelText.Text = $"Lv.{_playerStatus.Level}";
            InitWindow();
        }

        public FloatingIconWindow()
        {
            InitializeComponent();
            _playerStatus = new PlayerStatus();
            InitWindow();
        }

        private void InitWindow()
        {
            this.MouseLeftButtonDown += OnDragStart;
            this.MouseMove += OnDragMove;
            this.MouseLeftButtonUp += OnDragEnd;

            this.Loaded += (_, _) =>
            {
                _floatSb = (Storyboard)FindResource("FloatLoop");
                _floatSb.Begin();

                // ★ 絶対パスで動画をセットして再生
                var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "character.mp4");
                if (File.Exists(videoPath))
                {
                    CharVideo.Source = new Uri(videoPath);
                    CharVideo.Play();
                    CharVideo.MediaEnded += (s, e) =>
                    {
                        CharVideo.Position = TimeSpan.Zero;
                        CharVideo.Play();
                    };
                }

                SnapToEdge();
            };
        }

        public void SetLevel(int level)
            => LevelText.Text = $"Lv.{level}";

        public void SetEmergency(bool active)
        {
            EmergencyDot.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            EmergencyMark.Visibility = active ? Visibility.Visible : Visibility.Collapsed;

            if (active)
            {
                _pulseSb ??= (Storyboard)FindResource("EmergencyPulse");
                _pulseSb.Begin();
            }
            else
            {
                _pulseSb?.Stop();
            }
        }

        public void SetDueTodayAlert(bool active)
            => SetEmergency(active);

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;
            _isDragging = true;
            _dragOffset = e.GetPosition(this);
            this.CaptureMouse();
        }

        private void OnDragMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var pos = PointToScreen(e.GetPosition(this));
            this.Left = pos.X - _dragOffset.X;
            this.Top = pos.Y - _dragOffset.Y;
        }

        private void OnDragEnd(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            this.ReleaseMouseCapture();
            SnapToEdge();
        }

        private void SnapToEdge()
        {
            var screen = SystemParameters.PrimaryScreenWidth;
            var centerX = this.Left + this.ActualWidth / 2;

            this.Left = centerX < screen / 2
                ? EdgeMargin
                : screen - this.ActualWidth - EdgeMargin;

            double minTop = EdgeMargin + FloatAnimationHeight;
            double maxTop = SystemParameters.PrimaryScreenHeight - this.ActualHeight - EdgeMargin;
            this.Top = Math.Max(minTop, Math.Min(this.Top, maxTop));
        }

        private void OnIconClick(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging) return;

            if (this.Owner is MainWindow main)
            {
                main.WindowState = WindowState.Normal;
                main.Show();
                main.Activate();
            }

            this.Hide();
            e.Handled = true;
        }

        private void OnIconRightClick(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();

            var itemOpen = new MenuItem { Header = "▶ 開く", FontFamily = new System.Windows.Media.FontFamily("Consolas") };
            itemOpen.Click += (_, _) => OnIconClick(sender, null!);

            var itemQuit = new MenuItem { Header = "× 終了", FontFamily = new System.Windows.Media.FontFamily("Consolas") };
            itemQuit.Click += (_, _) =>
            {
                if (this.Owner is MainWindow main)
                    main.ForceClose();
            };

            menu.Items.Add(itemOpen);
            menu.Items.Add(new Separator());
            menu.Items.Add(itemQuit);
            menu.IsOpen = true;
        }
    }
}