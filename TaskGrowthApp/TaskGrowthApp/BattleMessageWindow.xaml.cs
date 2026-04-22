using System;
using System.Windows;
using System.Windows.Threading;

namespace TaskGrowthApp
{
    public partial class BattleMessageWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;

        /// <summary>
        /// バトルメッセージウィンドウ。
        /// クリックまたは autoCloseSeconds 秒後に自動で閉じる。
        /// </summary>
        public BattleMessageWindow(string message, double autoCloseSeconds = 3.0)
        {
            InitializeComponent();
            MessageText.Text = message;

            // クリックで即閉じ
            this.MouseLeftButtonDown += (_, _) => Close();

            // 自動クローズタイマー
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(autoCloseSeconds)
            };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _autoCloseTimer.Stop();
            base.OnClosed(e);
        }
    }
}
