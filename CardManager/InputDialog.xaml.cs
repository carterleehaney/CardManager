using System.Collections.Generic;
using System.Windows;

namespace CardManager
{
    public partial class InputDialog : Window
    {
        public string InputValue { get; private set; } = string.Empty;

        public InputDialog(string prompt, string defaultValue, IEnumerable<string>? suggestions = null)
        {
            InitializeComponent();
            
            PromptText.Text = prompt;
            
            if (suggestions != null)
            {
                foreach (var item in suggestions)
                {
                    InputComboBox.Items.Add(item);
                }
            }
            
            InputComboBox.Text = defaultValue;
            InputComboBox.Focus();
            
            Loaded += (s, e) => InputComboBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputComboBox.Text?.Trim() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

