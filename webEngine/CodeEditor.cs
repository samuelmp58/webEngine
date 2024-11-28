using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Web.WebView2.WinForms;
using ICSharpCode.AvalonEdit;
using System.IO;

namespace webEngine
{
    public class CodeEditor
    {
        private static MainWindow mainWindow;
        public CodeEditor(MainWindow main) 
        { 
            mainWindow = main;
        }
        private TabItem FindOpenTab(string filePath)
        {
            foreach (TabItem tab in mainWindow.tabControl.Items)
            {
                if (tab.Tag is string tabFilePath && tabFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    return tab;
                }
            }
            return null;
        }

        public void ActivateTab(TabItem tab)
        {
            mainWindow.tabControl.SelectedItem = tab;
        }
        private static void ApplyEditorSettingsToAllTabs()
        {
            foreach (TabItem tabItem in mainWindow.tabControl.Items)
            {
                if (tabItem.Content is TextEditor editor)
                {
                    editor.FontFamily = new System.Windows.Media.FontFamily(Properties.Settings.Default.EditorFont);
                    editor.FontSize = Convert.ToDouble(Properties.Settings.Default.EditorFontSize);
                }
            }
        }
        private static void ShowEditorSettingsDialog()
        {
            var settingsWindow = new Window
            {
                Title = "Editor Settings",
                Width = 300,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };

            // Fonte
            var fontComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 250,
                Margin = new Thickness(10)
            };
            fontComboBox.Items.Add("Consolas");
            fontComboBox.Items.Add("Courier New");
            fontComboBox.Items.Add("Arial");
            fontComboBox.SelectedItem = Properties.Settings.Default.EditorFont;
            stackPanel.Children.Add(new TextBlock { Text = "Font:" });
            stackPanel.Children.Add(fontComboBox);

            // Tamanho da Fonte
            var fontSizeComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 250,
                Margin = new Thickness(10)
            };
            fontSizeComboBox.Items.Add("10");
            fontSizeComboBox.Items.Add("12");
            fontSizeComboBox.Items.Add("14");
            fontSizeComboBox.Items.Add("16");
            fontSizeComboBox.Items.Add("18");
            fontSizeComboBox.SelectedItem = Properties.Settings.Default.EditorFontSize.ToString();
            stackPanel.Children.Add(new TextBlock { Text = "Font Size:" });
            stackPanel.Children.Add(fontSizeComboBox);

            // Botão de Aplicar
            var applyButton = new System.Windows.Controls.Button
            {
                Content = "Apply",
                Margin = new Thickness(10),
                IsDefault = true
            };
            applyButton.Click += (s, e) =>
            {
                // Aplicar configurações
                Properties.Settings.Default.EditorFont = fontComboBox.SelectedItem.ToString();
                Properties.Settings.Default.EditorFontSize = fontSizeComboBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                CodeEditor.ApplyEditorSettingsToAllTabs();
                settingsWindow.Close();
            };
            stackPanel.Children.Add(applyButton);

            settingsWindow.Content = stackPanel;
            settingsWindow.ShowDialog();
        }


        public void OpenFileInEditor(string filePath, bool openDownExplorer = false, int lineNumber = 1)
        {
            // Criar uma instância do AvalonEdit:TextEditor
            var textEditor = new TextEditor
            {
                Text = System.IO.File.ReadAllText(filePath),
                FontFamily = new System.Windows.Media.FontFamily(Properties.Settings.Default.EditorFont),
                FontSize = Convert.ToDouble(Properties.Settings.Default.EditorFontSize),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                ShowLineNumbers = true,
                SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(GetHighlightingDefinition(filePath))
            };

            // Criar o menu de contexto
            var contextMenu = new ContextMenu();

            var copyMenuItem = new MenuItem { Header = "Copy" };
            copyMenuItem.Click += (s, e) => textEditor.Copy();
            contextMenu.Items.Add(copyMenuItem);

            var cutMenuItem = new MenuItem { Header = "Cut" };
            cutMenuItem.Click += (s, e) => textEditor.Cut();
            contextMenu.Items.Add(cutMenuItem);

            var pasteMenuItem = new MenuItem { Header = "Paste" };
            pasteMenuItem.Click += (s, e) => textEditor.Paste();
            contextMenu.Items.Add(pasteMenuItem);

            var selectAllMenuItem = new MenuItem { Header = "Select All" };
            selectAllMenuItem.Click += (s, e) => textEditor.SelectAll();
            contextMenu.Items.Add(selectAllMenuItem);

            // Adicionar menu de configurações
            var settingsMenuItem = new MenuItem { Header = "Editor Settings" };
            settingsMenuItem.Click += (s, e) => CodeEditor.ShowEditorSettingsDialog();
            contextMenu.Items.Add(settingsMenuItem);

            // Associar o menu de contexto ao TextEditor
            textEditor.ContextMenu = contextMenu;

            // Adicionar o editor ao TabControl
            var tabItem = new TabItem
            {
                Header = mainWindow.CreateTabHeader(Path.GetFileName(filePath)),
                Content = textEditor,
                Tag = filePath
            };
            mainWindow.tabControl.Items.Add(tabItem);
            mainWindow.tabControl.SelectedItem = tabItem;
            mainWindow.tabControl.Focus();

            // Usar Dispatcher para garantir que a linha seja visível após o carregamento
            textEditor.Loaded += (sender, e) =>
            {
                MoveCursorToLine(textEditor, lineNumber);
            };

            if (openDownExplorer)
            {
                string filePathWithoutStyle = filePath.Replace("/style.css", "").Replace("/behaviour.js", "").Replace("\\style.css", "").ToString();
                mainWindow.fileExplorerDown.Source = new Uri(filePathWithoutStyle, UriKind.Absolute);
            }
        }

        private static void MoveCursorToLine(TextEditor editor, int lineNumber)
        {
            // Verifica se o número da linha está dentro do intervalo válido
            if (lineNumber > 0 && lineNumber <= editor.LineCount)
            {
                // Define a posição do cursor para a linha especificada
                var line = editor.Document.GetLineByNumber(lineNumber);
                editor.ScrollTo(line.LineNumber, 0);
                editor.CaretOffset = line.Offset;
                editor.Focus();
            }
            else
            {
                // Adicionar tratamento para linha fora do intervalo
                System.Windows.MessageBox.Show("Line number is out of range.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string GetHighlightingDefinition(string filePath)
        {
            if (filePath.EndsWith(".js"))
                return "JavaScript";
            if (filePath.EndsWith(".html"))
                return "HTML";
            if (filePath.EndsWith(".css"))
                return "CSS";
            return "Text"; // Default highlighting
        }

    }
}
