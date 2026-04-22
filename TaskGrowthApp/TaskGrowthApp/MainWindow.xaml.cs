using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TaskGrowthApp.Models;
using TaskGrowthApp.Views;

namespace TaskGrowthApp
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<TaskItem> _tasks = new();
        private readonly NotionClient _client = new();
        private PlayerStatus _playerStatus = new();

        private readonly DispatcherTimer _characterTimer;
        private readonly DispatcherTimer _dailyEventTimer;
        private readonly Random _random = new();

        private bool _emergencyQuestTriggeredToday = false;
        private Storyboard? _emergencyStoryboard;
        private DateTime _lastDate = DateTime.Today;

        private bool _isUpdatingTask = false;
        private bool _isSavingOnClose = false;

        private DateTime? _pendingDueDate = null;

        public MainWindow()
        {
            InitializeComponent();
            TaskList.ItemsSource = _tasks;
            this.Closing += Window_Closing;

            _characterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _characterTimer.Tick += CharacterTimer_Tick;

            _dailyEventTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _dailyEventTimer.Tick += DailyEventTimer_Tick;
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // ★ 絶対パスで動画をセットして再生
            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "character_1.mp4");
            if (File.Exists(videoPath))
            {
                CharacterVideo.Source = new Uri(videoPath);
                CharacterVideo.Play();
            }

            _playerStatus = await _client.GetPlayerStatusAsync() ?? new PlayerStatus();
            UpdateStatus();

            var tasks = await _client.GetActiveTasksAsync();
            _tasks.Clear();
            foreach (var t in tasks)
                _tasks.Add(t);

            if (_tasks.Any(t => t.Priority == 3))
            {
                ShowEmergencyScrollWithMessage("緊急クエスト 出現中！");
                _emergencyQuestTriggeredToday = true;
            }

            _characterTimer.Start();
            _dailyEventTimer.Start();

            CheckEmergencyQuest();
            RefreshDueDateDisplay();
        }

        private string GetRandomVideoFileName()
        {
            int rand = _random.Next(100); // 0〜99の乱数を取得

            // 確率の振り分け（合計が100になるように調整）
            if (rand < 60) return "character_1.mp4"; // 60% の確率（基本の動き）
            if (rand < 70) return "character_2.mp4"; // 10% の確率
            if (rand < 80) return "character_3.mp4"; // 10% の確率
            if (rand < 90) return "character_4.mp4"; // 10% の確率

            return "character_5.mp4";                // 残りの10%
        }

        // ★ 動画ループ
        private void CharacterVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            // 専用メソッドを呼んで、次に再生するファイル名を取得
            string fileName = GetRandomVideoFileName();

            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);
            if (File.Exists(videoPath))
            {
                CharacterVideo.Source = new Uri(videoPath);
            }

            CharacterVideo.Position = TimeSpan.Zero;
            CharacterVideo.Play();
        }

        private void UpdateStatus()
        {
            LevelText.Text = $"Lv.{_playerStatus.Level}";
            ExpBar.Maximum = _playerStatus.ExpToNextLevel;
            ExpBar.Value = _playerStatus.CurrentExp;
            ExpLabel.Text = $"EXP {_playerStatus.CurrentExp} / {_playerStatus.ExpToNextLevel}";
            CoinText.Text = $"💰 {_playerStatus.Coin}";
        }

        private void OnOpenDueDatePopup(object sender, RoutedEventArgs e)
        {
            var popup = new DueDatePopup(_pendingDueDate);
            ShowSubWindow(popup);

            if (popup.ShowDialog() == true)
            {
                _pendingDueDate = popup.SelectedDate;
                UpdateDueDateUI();
            }
        }

        private void OnClearDueDate(object sender, RoutedEventArgs e)
        {
            _pendingDueDate = null;
            UpdateDueDateUI();
        }

        private void UpdateDueDateUI()
        {
            if (_pendingDueDate.HasValue)
            {
                var diff = (_pendingDueDate.Value.Date - DateTime.Today).Days;
                SelectedDueDateText.Text = diff switch
                {
                    0 => $"今日 ({_pendingDueDate.Value:M/d})",
                    1 => $"明日 ({_pendingDueDate.Value:M/d})",
                    _ => _pendingDueDate.Value.ToString("yyyy/M/d")
                };
                SelectedDueDateText.Foreground = diff < 0
                    ? Brushes.Red
                    : diff <= 1
                        ? Brushes.Yellow
                        : new SolidColorBrush(Color.FromRgb(170, 170, 170));
                DueDateClearButton.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedDueDateText.Text = "（なし）";
                SelectedDueDateText.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
                DueDateClearButton.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnAddTask(object sender, RoutedEventArgs e)
            => await AddTaskFromInputAsync();

        private void TaskNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = AddTaskFromInputAsync();
        }

        private async Task AddTaskFromInputAsync()
        {
            var name = TaskNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            int priority = PriorityBox.SelectedIndex;

            var task = new TaskItem
            {
                Name = name,
                Priority = priority,
                DueDate = _pendingDueDate
            };

            _tasks.Add(task);

            TaskNameBox.Clear();
            _pendingDueDate = null;
            UpdateDueDateUI();

            var pageId = await _client.AddTaskAsync(task);
            if (pageId != null)
                task.PageId = pageId;

            if (priority == 3)
            {
                ShowEmergencyScrollWithMessage("緊急クエスト 追加！");
                _emergencyQuestTriggeredToday = true;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            var floating = new FloatingIconWindow(_playerStatus);
            floating.Owner = this;

            bool hasDueToday = _tasks.Any(t => t.IsDueToday);
            floating.SetDueTodayAlert(hasDueToday);

            floating.Show();
            this.Hide();
        }

        private void OnClose(object sender, RoutedEventArgs e)
            => AttemptClose();

        private void AttemptClose()
        {
            if (_isUpdatingTask || _isSavingOnClose)
            {
                ShowBattleMessage("しかし回り込まれてしまった！");
                return;
            }

            _isSavingOnClose = true;
            _ = SaveAndCloseAsync();
        }

        private async Task SaveAndCloseAsync()
        {
            var msg = new BattleMessageWindow("冒険の書に書き込んでいます...", autoCloseSeconds: 30);
            ShowSubWindow(msg);
            msg.Show();

            try
            {
                await _client.UpdatePlayerStatusAsync(_playerStatus);

                foreach (var task in _tasks.Where(t => t.IsSaved))
                    await _client.UpdateTaskAsync(task);
            }
            catch (Exception ex)
            {
                msg.Close();
                ShowBattleMessage($"保存に失敗しました！\n{ex.Message}");
                _isSavingOnClose = false;
                return;
            }

            msg.Close();
            _isSavingOnClose = false;
            this.Closing -= Window_Closing;
            Close();
        }

        private void CharacterTimer_Tick(object? sender, EventArgs e)
        {
            if (_random.Next(100) < 20)
                PlayDropAnimation();
        }

        private void PlayDropAnimation()
        {
            var sb = (Storyboard)FindResource("DropAndBounce");
            sb.Begin();
        }

        private void DailyEventTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;

            if (now.Date != _lastDate)
            {
                _lastDate = now.Date;
                _emergencyQuestTriggeredToday = false;

                var emergencyTasks = _tasks.Where(t => t.Priority == 3).ToList();
                foreach (var task in emergencyTasks)
                {
                    _tasks.Remove(task);
                    _ = _client.DeleteTaskAsync(task);
                }

                HideEmergencyScroll();
            }

            RefreshDueDateDisplay();
            CheckEmergencyQuest();
        }

        private void RefreshDueDateDisplay()
        {
            foreach (var task in _tasks)
                task.DueDate = task.DueDate;
        }

        private async Task ShowEmergencyQuestAsync()
        {
            var quest = new TaskItem
            {
                Name = "【緊急】工数管理表",
                Priority = 3,
                DueDate = DateTime.Today
            };

            _tasks.Add(quest);
            ShowEmergencyScrollWithMessage("緊急クエスト 出現！");

            var pageId = await _client.AddTaskAsync(quest);
            if (pageId != null)
                quest.PageId = pageId;
        }

        private void StartEmergencyScroll()
        {
            _emergencyStoryboard?.Stop();
            EmergencyCanvas.Visibility = Visibility.Visible;

            if (EmergencyText.ActualWidth == 0 || EmergencyCanvas.ActualWidth == 0)
            {
                EmergencyText.LayoutUpdated += EmergencyText_LayoutUpdated;
                return;
            }

            BeginEmergencyScroll();
        }

        private void EmergencyText_LayoutUpdated(object? sender, EventArgs e)
        {
            if (EmergencyText.ActualWidth == 0 || EmergencyCanvas.ActualWidth == 0) return;
            EmergencyText.LayoutUpdated -= EmergencyText_LayoutUpdated;
            BeginEmergencyScroll();
        }

        private void BeginEmergencyScroll()
        {
            double canvasWidth = EmergencyCanvas.ActualWidth;
            double textWidth = EmergencyText.ActualWidth;

            var animation = new DoubleAnimation
            {
                From = canvasWidth,
                To = -textWidth,
                Duration = TimeSpan.FromSeconds(5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            _emergencyStoryboard = new Storyboard();
            _emergencyStoryboard.Children.Add(animation);
            Storyboard.SetTarget(animation, EmergencyText);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            _emergencyStoryboard.Begin();
        }

        private void HideEmergencyScroll()
        {
            _emergencyStoryboard?.Stop();
            EmergencyCanvas.Visibility = Visibility.Collapsed;
        }

        private void ShowEmergencyScrollWithMessage(string message)
        {
            PlayEmergencyAnimation();
            StartEmergencyScroll();
            ShowBattleMessage(message);
        }

        private void PlayEmergencyAnimation()
        {
            var sb = (Storyboard)FindResource("DropAndBounce");
            sb.Begin();

            var flash = new ColorAnimation
            {
                From = Colors.Black,
                To = Colors.DarkRed,
                Duration = TimeSpan.FromSeconds(0.25),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(4)
            };

            var brush = new SolidColorBrush(Colors.Black);
            this.Background = brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
        }

        private async void OnTaskCommand(object sender, MouseButtonEventArgs e)
        {
            if (TaskList.SelectedItem is not TaskItem task) return;

            var cmd = new TaskCommandWindow();
            ShowSubWindow(cmd);

            if (cmd.ShowDialog() != true) return;

            if (cmd.Result == TaskCommandResultType.Complete)
                await CompleteTaskAsync(task);
            else if (cmd.Result == TaskCommandResultType.Delete)
                await DeleteTaskAsync(task);
        }

        private async Task DeleteTaskAsync(TaskItem task)
        {
            await _client.DeleteTaskAsync(task);
            _tasks.Remove(task);

            if (!_tasks.Any(t => t.Priority == 3))
                HideEmergencyScroll();

            ShowBattleMessage($"「{task.Name}」を すてた。");
        }

        private async Task CompleteTaskAsync(TaskItem task)
        {
            _isUpdatingTask = true;
            try
            {
                int gainedExp = task.Exp;
                int gainedCoin = task.Coin;

                _playerStatus.CurrentExp += gainedExp;
                _playerStatus.TotalExp += gainedExp;
                _playerStatus.Coin += gainedCoin;

                int levelUpCount = 0;
                while (_playerStatus.CurrentExp >= _playerStatus.ExpToNextLevel)
                {
                    _playerStatus.CurrentExp -= _playerStatus.ExpToNextLevel;
                    _playerStatus.Level++;
                    _playerStatus.Coin += 50;
                    levelUpCount++;
                }

                UpdateStatus();

                if (task.Priority == 3)
                    _playerStatus.LastEmergencyCompletedDate = DateTime.Today;

                await _client.UpdatePlayerStatusAsync(_playerStatus);
                await _client.MarkTaskDoneAsync(task);
                await _client.AddTaskLogAsync(task.Name, task.Exp, 0);

                _tasks.Remove(task);

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"「{task.Name}」を たおした！");
                sb.AppendLine($"EXP + {gainedExp}　💰 + {gainedCoin}");
                if (levelUpCount > 0)
                    sb.AppendLine($"★ LEVEL UP！ Lv.{_playerStatus.Level} になった！");

                ShowBattleMessage(sb.ToString().TrimEnd());

                if (!_tasks.Any(t => t.Priority == 3))
                    HideEmergencyScroll();
            }
            finally
            {
                _isUpdatingTask = false;
            }
        }

        private void ShowBattleMessage(string message)
        {
            var msg = new BattleMessageWindow(message);
            ShowSubWindow(msg);
            msg.ShowDialog();
        }

        public void ForceClose()
        {
            AttemptClose();
        }

        private void ShowSubWindow(Window sub)
        {
            sub.Owner = this;
            sub.WindowStartupLocation = WindowStartupLocation.Manual;

            double left = this.Left + this.ActualWidth + 8;
            double top = this.Top + 40;

            if (left + sub.Width > SystemParameters.PrimaryScreenWidth)
                left = this.Left - sub.Width - 8;

            sub.Left = left;
            sub.Top = top;
        }

        private void CheckEmergencyQuest()
        {
            var now = DateTime.Now;
            bool completedToday = _playerStatus.LastEmergencyCompletedDate == DateTime.Today;

            if (now.Hour >= 10 && !_emergencyQuestTriggeredToday && !completedToday)
            {
                _emergencyQuestTriggeredToday = true;
                _ = ShowEmergencyQuestAsync();
            }
        }

        private bool IsDueToday(TaskItem t)
            => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today;

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isUpdatingTask)
            {
                e.Cancel = true;
                ShowBattleMessage("しかし回り込まれてしまった！");
                return;
            }

            if (_isSavingOnClose)
            {
                e.Cancel = true;
                return;
            }

            e.Cancel = true;
            _isSavingOnClose = true;
            _ = SaveAndCloseAsync();
        }
    }
}