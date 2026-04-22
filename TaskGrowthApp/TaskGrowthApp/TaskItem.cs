using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TaskGrowthApp
{
    public class TaskItem : INotifyPropertyChanged
    {
        // ─── Notion 管理 ──────────────────────────────────────────────
        public string PageId  { get; set; } = "";
        public bool   IsDone  { get; set; } = false;
        public bool   IsSaved => !string.IsNullOrEmpty(PageId);

        // ─── 基本フィールド ───────────────────────────────────────────
        private string _name = "";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private int _priority = 1;
        public int Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriorityText));
                OnPropertyChanged(nameof(PriorityColor));
            }
        }

        // ─── 期限 ─────────────────────────────────────────────────────
        private DateTime? _dueDate;
        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                _dueDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DueDateText));
                OnPropertyChanged(nameof(DueDateColor));
                OnPropertyChanged(nameof(IsOverdue));
                OnPropertyChanged(nameof(IsDueSoon));
                OnPropertyChanged(nameof(IsDueToday));
                OnPropertyChanged(nameof(DueWarningVisible));
            }
        }

        /// <summary>リスト表示用の短い日付文字列</summary>
        public string DueDateText
        {
            get
            {
                if (_dueDate == null) return "";
                var diff = (_dueDate.Value.Date - DateTime.Today).Days;
                return diff switch
                {
                    0  => "今日",
                    1  => "明日",
                    -1 => "昨日",
                    _  => _dueDate.Value.ToString("M/d")
                };
            }
        }

        /// <summary>期限の色（期限切れ=赤、今日=オレンジ、明日=黄、それ以外=グレー）</summary>
        public Brush DueDateColor
        {
            get
            {
                if (_dueDate == null) return Brushes.Transparent;
                var diff = (_dueDate.Value.Date - DateTime.Today).Days;
                return diff switch
                {
                    < 0 => Brushes.Red,
                    0   => new SolidColorBrush(Color.FromRgb(255, 140, 0)),
                    1   => Brushes.Yellow,
                    _   => new SolidColorBrush(Color.FromRgb(150, 150, 150))
                };
            }
        }

        /// <summary>期限切れかどうか</summary>
        public bool IsOverdue => _dueDate.HasValue && _dueDate.Value.Date < DateTime.Today;

        /// <summary>今日が期限かどうか（点滅枠用）</summary>
        public bool IsDueToday => _dueDate.HasValue && _dueDate.Value.Date == DateTime.Today;

        /// <summary>今日または明日が期限かどうか</summary>
        public bool IsDueSoon => _dueDate.HasValue &&
                                  (_dueDate.Value.Date - DateTime.Today).Days is >= 0 and <= 1;

        /// <summary>⚠ アイコンを表示するか（期限切れ or 今日 or 明日）</summary>
        public bool DueWarningVisible => IsOverdue || IsDueSoon;

        // ─── 表示用プロパティ ─────────────────────────────────────────
        public string Cursor { get; set; } = "";

        public string PriorityText => Priority switch
        {
            0 => "低",
            1 => "中",
            2 => "高",
            3 => "緊",
            _ => "中"
        };

        public Brush PriorityColor => Priority switch
        {
            0 => Brushes.Green,
            1 => Brushes.Yellow,
            2 => Brushes.Red,
            _ => Brushes.White
        };

        // ─── ゲームパラメータ ─────────────────────────────────────────
        /// <summary>優先度に応じた獲得EXP</summary>
        public int Exp => Priority switch
        {
            0 => 10,
            1 => 30,
            2 => 60,
            3 => 120,
            _ => 30
        };

        /// <summary>優先度に応じた獲得コイン</summary>
        public int Coin => Priority switch
        {
            0 => 1,
            1 => 3,
            2 => 6,
            3 => 15,
            _ => 3
        };

        // ─── INotifyPropertyChanged ───────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
