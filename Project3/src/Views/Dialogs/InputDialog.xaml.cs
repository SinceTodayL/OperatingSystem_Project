using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace FileManagerSystem.Views.Dialogs
{
    public partial class InputDialog : Window, INotifyPropertyChanged
    {
        private string _title;
        private string _message;
        private string _result;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        public InputDialog(string title, string message, string defaultValue = "")
        {
            InitializeComponent();
            DataContext = this;
            Title = title;
            Message = message;
            Result = defaultValue;
            
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                if (!string.IsNullOrEmpty(defaultValue))
                    InputTextBox.SelectAll();
            };
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Result))
            {
                MessageBox.Show("请输入有效的名称！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 