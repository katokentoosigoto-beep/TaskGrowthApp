using System.Windows;
using System.Windows.Input;
using TaskGrowthApp.Models;

namespace TaskGrowthApp.Views
{
    public partial class TaskCommandWindow : Window
    {
        public TaskCommandResultType Result { get; private set; }

        private bool _attackSelected = false;
        private bool _deleteSelected = false;

        public TaskCommandWindow()
        {
            InitializeComponent();
        }

        // たおすボタンクリック
        private void AttackButtonGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_attackSelected)
            {
                Result = TaskCommandResultType.Complete;
                DialogResult = true;
            }
            else
            {
                _attackSelected = true;
                _deleteSelected = false;
                AttackCursor.Visibility = Visibility.Visible;
                DeleteCursor.Visibility = Visibility.Collapsed;
            }
        }

        // すてるボタンクリック
        private void DeleteButtonGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_deleteSelected)
            {
                Result = TaskCommandResultType.Delete;
                DialogResult = true;
            }
            else
            {
                _deleteSelected = true;
                _attackSelected = false;
                DeleteCursor.Visibility = Visibility.Visible;
                AttackCursor.Visibility = Visibility.Collapsed;
            }
        }

        // 罰ボタン（×）
        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ウィンドウドラッグ
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
