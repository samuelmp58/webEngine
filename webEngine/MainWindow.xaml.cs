using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using TreeView = System.Windows.Controls.TreeView;
using TreeViewItem = System.Windows.Controls.TreeViewItem;
using System.Text.Json;
using System.Net.Http.Json;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties;
using static MaterialDesignThemes.Wpf.Theme;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Configuration;

namespace webEngine
{
    public partial class MainWindow : Window
    {
        private bool _sidebarState;
        private string? _projectPath = null;
        private CodeEditor codeEditor;

        Configuration EditorSettings = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        
        public MainWindow()
        {
            InitializeComponent();
            _sidebarState = false;
            InitializeWebView();
            fileTreeView.SelectedItemChanged += fileTreeView_SelectedItemChanged; // Adicionar evento de seleção de item
            codeEditor = new CodeEditor(this);
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }

        private async void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                try {
                    ScriptInject.LoadJs(webView);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred while adding the script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to initialize WebView2.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string RemoveLastSegment(string url)
        {
            // Verifica se a URL contém barras
            int lastSlashIndex = url.LastIndexOf('/');

            // Se houver uma barra e não for o início da URL
            if (lastSlashIndex > 0)
            {
                // Remove o segmento após a última barra
                return url.Substring(0, lastSlashIndex);
            }

            // Se não houver barra, retorna a URL original
            return url;
        }


        private void DisplayFileExplorer(string directoryPath) // not used
        {
            // Get all files in the directory
            var files = Directory.GetFiles(directoryPath);

            // Separate files by extension
            var cssFiles = files.Where(f => f.EndsWith(".css")).ToList();
            var jsFiles = files.Where(f => f.EndsWith(".js")).ToList();
            var pngFiles = files.Where(f => f.EndsWith(".png")).ToList();

            // Build the HTML content
            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><head>");
            htmlBuilder.Append("<style>");
            htmlBuilder.Append("body { font-family: Arial, sans-serif; margin: 20px; }");
            htmlBuilder.Append(".file-group { margin-bottom: 20px; }");
            htmlBuilder.Append(".file-group h2 { margin-top: 0; }");
            htmlBuilder.Append(".file-item { display: inline-flex; flex-direction: column; align-items: center; margin: 5px; }");
            htmlBuilder.Append(".file-item img { display: block; margin-bottom: 5px; }");
            htmlBuilder.Append("</style>");
            htmlBuilder.Append("</head><body>");

            // Add file groups
            if (cssFiles.Any())
            {
                htmlBuilder.Append("<div class='file-group'>");
                htmlBuilder.Append("<h2>CSS Files</h2>");
                foreach (var file in cssFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string filePath = file.Replace(directoryPath, "");
                    htmlBuilder.Append("<div class='file-item'>");
                    htmlBuilder.Append($"<a href='#' onclick='openFile(\"{file}\")'><img src='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.8.3/icons/file-code.svg' width='60px' alt='Edit CSS'></a>");
                    htmlBuilder.Append($"<p>{fileName}</p>");
                    htmlBuilder.Append("</div>");
                }
                htmlBuilder.Append("</div>");
            }

            if (jsFiles.Any())
            {
                htmlBuilder.Append("<div class='file-group'>");
                htmlBuilder.Append("<h2>JS Files</h2>");
                foreach (var file in jsFiles)
                {
                    string fileName = Path.GetFileName(file);
                    string filePath = file.Replace(directoryPath, "");
                    htmlBuilder.Append("<div class='file-item'>");
                    htmlBuilder.Append($"<a href='#' onclick='openFile(\"{file}\")'><img src='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.8.3/icons/file-code.svg' width='60px' alt='Edit JS'></a>");
                    htmlBuilder.Append($"<p>{fileName}</p>");
                    htmlBuilder.Append("</div>");
                }
                htmlBuilder.Append("</div>");
            }

            if (pngFiles.Any())
            {
                htmlBuilder.Append("<div class='file-group'>");
                htmlBuilder.Append("<h2>PNG Images</h2>");
                foreach (var file in pngFiles)
                {
                    string fileName = Path.GetFileName(file);
                    htmlBuilder.Append("<div class='file-item'>");
                    htmlBuilder.Append($"<img src='file:///{file}' width='60px' alt='{fileName}'>");
                    htmlBuilder.Append($"<p>{fileName}</p>");
                    htmlBuilder.Append("</div>");
                }
                htmlBuilder.Append("</div>");
            }

            //htmlBuilder.Append("<script>");
            //htmlBuilder.Append("function openFile(filePath) { window.open('file:///" + directoryPath + "/" + filePath.ToString() + "', '_blank'); }");
            //htmlBuilder.Append("</script>");

            htmlBuilder.Append("</body></html>");

            // Show in WebView2
            fileExplorerDown.NavigateToString(htmlBuilder.ToString());
        }

        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var messageJson = e.TryGetWebMessageAsString();

            // Parse the message as JSON
            dynamic data = JsonConvert.DeserializeObject(messageJson);

            // Extrair o className e imageUrl
            string className = data.className;
            string imageUrl = data.imageUrl;

            // Exibe os valores extraídos
            // System.Windows.MessageBox.Show($"Class Name: {className}\nImage URL: {imageUrl}");
            string modifiedUrl = RemoveLastSegment(imageUrl);

            fileExplorerDown.Source = new Uri(modifiedUrl, UriKind.Absolute);
         
            classfolder.Text = className;

            string styleUrl = modifiedUrl.Replace("file:///", "") + "/style.css";

            int line = CssFileHandler.FindClassLine(styleUrl, className);
            //System.Windows.MessageBox.Show(line.ToString());


            codeEditor.OpenFileInEditor(styleUrl, true, line);

        }

        private void btn_hideShow_Click(object sender, RoutedEventArgs e)
        {
            if (_sidebarState)
            {
                sidebar.Visibility = Visibility.Collapsed;
                btn_hideShow.Content = "Show";
            }
            else
            {
                sidebar.Visibility = Visibility.Visible;
                btn_hideShow.Content = "Hide";
            }
            _sidebarState = !_sidebarState;
        }

        public async void ReloadHtml() {
            string filePath = Path.Combine(_projectPath, "build.html");

            if (File.Exists(filePath))
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string serverPath = Path.Combine("C:\\TempServer", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(serverPath));
                    File.Copy(filePath, serverPath, true);

                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.Navigate($"file:///{filePath}");
                    }
                    else
                    {
                        await webView.EnsureCoreWebView2Async();
                        webView.CoreWebView2.Navigate($"file:///{filePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ScriptInject.LoadJs(webView);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("The file does not exist. Please check the path and try again.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private async void LoadHtml_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
                Title = "Select an HTML file"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                if (File.Exists(filePath))
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string serverPath = Path.Combine("C:\\TempServer", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(serverPath));
                        File.Copy(filePath, serverPath, true);

                        if (webView.CoreWebView2 != null)
                        {
                            webView.CoreWebView2.Navigate($"file:///{filePath}");
                        }
                        else
                        {
                            await webView.EnsureCoreWebView2Async();
                            webView.CoreWebView2.Navigate($"file:///{filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        ScriptInject.LoadJs(webView);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("The file does not exist. Please check the path and try again.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Project_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new CommonOpenFileDialog { IsFolderPicker = true };
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _projectPath = folderDialog.FileName;
                LoadProjectFiles(_projectPath);
                fileExplorerDown.Source = new Uri(_projectPath, UriKind.Absolute);

                LoadIndexHtml();
            }
        }

        public void OpenProject(string path)
        {
            //System.Windows.Forms.MessageBox.Show(path);
            _projectPath = path;
            LoadProjectFiles(_projectPath);
            fileExplorerDown.Source = new Uri(_projectPath, UriKind.Absolute);

            LoadIndexHtml();
        }

        private void LoadProjectFiles(string path)
        {
            fileTreeView.Items.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);
            fileTreeView.Items.Add(CreateDirectoryNode(rootDirectoryInfo));
        }

        private static TreeViewItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new TreeViewItem { Header = directoryInfo.Name, Tag = directoryInfo.FullName };

            foreach (var directory in directoryInfo.GetDirectories())
            {
                directoryNode.Items.Add(CreateDirectoryNode(directory));
            }

            foreach (var file in directoryInfo.GetFiles("*.*"))
            {
                var fileItem = new TreeViewItem { Header = file.Name, Tag = file.FullName };
                directoryNode.Items.Add(fileItem);
            }

            return directoryNode;
        }


        private async void LoadIndexHtml()
        {
            string indexPath = Path.Combine(_projectPath, "build.html");
            if (File.Exists(indexPath))
            {
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate($"file:///{indexPath}");
                    ScriptInject.LoadJs(webView);
                }
                else
                {
                    await webView.EnsureCoreWebView2Async();
                    webView.CoreWebView2.Navigate($"file:///{indexPath}");
                    ScriptInject.LoadJs(webView);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("The build.html file was not found in the selected project folder.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void fileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem)
            {
                if (selectedItem.Tag is string filePath)
                {
                    if (File.Exists(filePath))
                    {
                        // Verificar se o arquivo já está aberto
                        var existingTab = FindOpenTab(filePath);
                       
                        if (existingTab != null)
                        {
                            // Ativar a aba existente
                            codeEditor.ActivateTab(existingTab);
                        }
                        else
                        {
                            // Abrir o arquivo em uma nova aba
                            codeEditor.OpenFileInEditor(filePath);
                        }
                    }
                    else if (Directory.Exists(filePath))
                    {
                        // Formatar o caminho para URL
                        string formattedPath = new DirectoryInfo(filePath).FullName.Replace("\\", "/");
                        string url = "file:///" + formattedPath;
                        fileExplorerDown.Navigate(url);
                    }
                }
            }
        }
        private TabItem FindOpenTab(string filePath)
        {
            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.Tag is string tabFilePath && tabFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                {
                    return tab;
                }
            }
            return null;
        }

        public StackPanel CreateTabHeader(string header)
        {
            var stackPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            var textBlock = new TextBlock { Text = header };
            var closeButton = new System.Windows.Controls.Button { Content = "X", Width = 20, Height = 20, Margin = new Thickness(5, 0, 0, 0) };
            closeButton.Click += (s, e) => CloseTab_Click(stackPanel);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(closeButton);
            return stackPanel;
        }

        private void CloseTab_Click(StackPanel tabHeader)
        {
            var tabItem = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header == tabHeader);
            if (tabItem != null)
            {
                tabControl.Items.Remove(tabItem);
            }
        }

        private bool IsFileOpenInEditor(string filePath)
        {
            foreach (TabItem tab in tabControl.Items)
            {
                if (tab.Tag is string openFilePath && openFilePath == filePath)
                {
                    return true;
                }
            }
            return false;
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e) // Save
        {
            if (tabControl.SelectedItem is TabItem selectedTab && selectedTab.Tag is string filePath)
            {
                if (selectedTab.Content is TextEditor textEditor)
                {
                    try
                    {
                        // Salvar o conteúdo do AvalonEdit.TextEditor no arquivo
                        File.WriteAllText(filePath, textEditor.Text);
                        System.Windows.MessageBox.Show("File saved successfully.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"An error occurred while saving the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("The current tab is not associated with a valid editor.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No file is currently selected for saving.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private async void btn_saveHtml_Click(object sender, RoutedEventArgs e)
        {
            // Executar o script para obter o HTML como texto
            string htmlText = await webView.ExecuteScriptAsync("document.documentElement.outerHTML;");

            // Decodificar os caracteres JSON escapados
            htmlText = Compiler.DecodeJsonEscapedString(htmlText);

            // Definir o caminho e nome do arquivo
            string filePath = Path.Combine(_projectPath, "build.html");

            // Salvar o conteúdo HTML em um arquivo
            try
            {
                // Usar StreamWriter para criar e escrever o arquivo
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.Write(htmlText);
                }

                // Informar ao usuário que o arquivo foi salvo
                System.Windows.MessageBox.Show($"HTML content has been saved to {filePath}", "File Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                // Mostrar uma mensagem de erro se algo der errado
                System.Windows.MessageBox.Show($"An error occurred while saving the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btn_assets_Click(object sender, RoutedEventArgs e) // btn_assets
        {
            if (_projectPath != null)
            {
                AssetsWindow assetsWindow = new AssetsWindow(_projectPath, this);
                assetsWindow.Show();
            }
            else
            {
                System.Windows.MessageBox.Show("Open or Create a project first.", "Ops...", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void btn_compile_Click(object sender, RoutedEventArgs e)
        {
            if (_projectPath == null)
            {
                System.Windows.MessageBox.Show("Open or Create a project first.", "Ops...", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Compiler compiler = new Compiler(webView, _projectPath);
            compiler.Compile();
        }

        private void btn_createNewProject_Click(object sender, RoutedEventArgs e)
        {
            CreateNewProjectWindow cnpw = new CreateNewProjectWindow(this);
            cnpw.Show();
        }

        private void btn_run_Click(object sender, RoutedEventArgs e)
        {
            if (_projectPath == null)
            {
                System.Windows.MessageBox.Show("Open or Create a project first.", "Ops...", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string indexPath = Path.Combine(_projectPath, "index.html");

            if (File.Exists(indexPath))
            {
                // O arquivo index.html existe
                try
                {
                    // Execute o arquivo index.html
                    // Aqui estamos assumindo que você quer abrir o arquivo no navegador padrão
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = indexPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to open {indexPath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // O arquivo index.html não existe
                System.Windows.MessageBox.Show("index.html not found in the project path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        } 
    }
}

