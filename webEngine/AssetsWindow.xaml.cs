using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Security.Policy;
using System.Diagnostics;

namespace webEngine
{
    public partial class AssetsWindow : Window
    {
        private string selectedFilePath;
        private static string _projectPath;
        private static string _projectObjectsPath;
        private static MainWindow mainWindow;
        private static CodeEditor codeEditor;

        public AssetsWindow(string projectPath, MainWindow main)
        {
            mainWindow = main;
            _projectPath = projectPath;
            _projectObjectsPath = projectPath + "\\objects";

            InitializeComponent();
            InitializeWebView();
            LoadAssetsTreeView();

            codeEditor = new CodeEditor(main);
            //DisplayDirectoryContent(projectPath);

        }
        private async void InitializeWebView()
        {
            await PreviewWebView.EnsureCoreWebView2Async(null);
            PreviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "assets.local",
                _projectObjectsPath,
                CoreWebView2HostResourceAccessKind.Allow
            );
            PreviewWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }

        private void OpenFileInNotepadPlusPlus(string url)
        {
            string filePath = new Uri(url).LocalPath;
            string notepadPlusPlusPath = @"notepad\notepad++.exe";

            ProcessStartInfo startInfo = new ProcessStartInfo(notepadPlusPlusPath)
            {
                Arguments = filePath,
                UseShellExecute = true
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur
                System.Windows.MessageBox.Show($"Failed to open file in Notepad++: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {

            string messageJson = e.TryGetWebMessageAsString();
           
            // Parse the message as JSON
            dynamic data = JsonConvert.DeserializeObject(messageJson);

            if (data.type == "openFile")
            {
                string url = data.data;
                //System.Windows.MessageBox.Show(url, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                //OpenFileInNotepadPlusPlus(url);

                codeEditor.OpenFileInEditor(url, true);
                

            }

            else if(data.type == "uploadImage")
            {
                // Extrair o data (imagem base64)
                string file = data.data;
                string dir = data.dir;
                string className = data.className;

                var base64Data = file.Split(',')[1];

                if (!string.IsNullOrEmpty(base64Data))
                {
                    try
                    {
                        string cssFilePath = _projectObjectsPath + "\\" + dir + "\\style.css";

                        // Vendo se já existe o nome na style.css, se já existir, não criar
                        int line = CssFileHandler.FindClassLine(cssFilePath, className);
                        if (line != -1)
                        {
                            System.Windows.MessageBox.Show("Classe já existe", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            return;
                        }

                        var imageBytes = Convert.FromBase64String(base64Data);
                        //System.Windows.MessageBox.Show(imageBytes.ToString());
                        var imageFile = Path.Combine(_projectObjectsPath + "\\" + dir, className+".png");
                        File.WriteAllBytes(imageFile, imageBytes);
                        CssFileHandler fh = new CssFileHandler(_projectObjectsPath);
                        fh.AddClassToCssFile(dir, className, className + ".png");

                        // Abrindo o css da nova classe
                        line = CssFileHandler.FindClassLine(cssFilePath, className);
                        if (line != -1)
                        {
                             //System.Windows.MessageBox.Show(cssFilePath.ToString());
                             //CssFileHandler.OpenFileAtLine(cssFilePath, line);

                            codeEditor.OpenFileInEditor(cssFilePath, true, line);
                            
                            System.Windows.MessageBox.Show("To use the new sprite, reload the HTML", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            //mainWindow.ReloadHtml();
                        }



                    }
                    catch (Exception ex)
                    {
                        // Handle any exceptions that occur during image processing
                        System.Windows.MessageBox.Show(ex.Message);
                        Console.WriteLine($"Error processing image: {ex.Message}");
                    }
                }
            }
            else if (data.type == "deleteObject")
            {

            }
        }
        private void LoadAssetsTreeView()
        {
            string basePath = _projectObjectsPath.ToString() ;
 
            AssetsTreeView.Items.Clear();
            try
            {
                TreeViewItem textureItem = new TreeViewItem { Header = "Objects", Tag = basePath, IsExpanded = true};
                LoadDirectory(textureItem);
                //textureItem.IsSelected = true;

                AssetsTreeView.Items.Add(textureItem);

            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
          

        }

        private void LoadDirectory(TreeViewItem parentItem)
        {
            string path = parentItem.Tag as string;
            if (Directory.Exists(path))
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    TreeViewItem directoryItem = new TreeViewItem { Header = Path.GetFileName(directory), Tag = directory };
                    LoadDirectory(directoryItem);
                    parentItem.Items.Add(directoryItem);
                }

                //foreach (string file in Directory.GetFiles(path))
                //{
                //    TreeViewItem fileItem = new TreeViewItem { Header = Path.GetFileName(file), Tag = file };
                //    parentItem.Items.Add(fileItem);
                //}
            }
        }

        private void AssetsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem selectedItem = AssetsTreeView.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                string path = selectedItem.Tag as string;
                if (Directory.Exists(path))
                {
                    DisplayDirectoryContent(path);
                }
                else if (File.Exists(path))
                {
                    selectedFilePath = path;
                    string fileName = Path.GetFileName(path);
                    string relativePath = Path.GetRelativePath(_projectObjectsPath, path);
                    string url = $"https://assets.local/{relativePath.Replace("\\", "/")}";
                    DisplayContent(url);
                }
            }
        }

        private void DisplayDirectoryContent(string directoryPath)
        {
            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            var cssFiles = files.Where(f => f.EndsWith(".css")).ToList();
            var jsFiles = files.Where(f => f.EndsWith(".js")).ToList();
            var pngFiles = files.Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".gif")).ToList();

            var htmlBuilder = new StringBuilder();
            htmlBuilder.Append("<html><body>");

            htmlBuilder.Append("<h2>CSS Files</h2><ul>");
            foreach (var file in cssFiles)
            {
                string fileName = Path.GetFileName(file);
                string relativePath = Path.GetRelativePath(_projectObjectsPath, file);
                string url = $"{_projectObjectsPath.Replace("\\", "/")}/{relativePath.Replace("\\", "/")}";

                //htmlBuilder.Append($"<li><a href='{url}' target='_blank'>{url}</a></li>");
                //htmlBuilder.Append($"<li><a href='file://{url}'>{url}</a></li>");
                htmlBuilder.Append($"<li><a href='#' onclick='openFile(\"{url}\")'>{relativePath}</a></li>");
            }
            htmlBuilder.Append("</ul>");

            // Lista de imagens
            var pngGroups = pngFiles.GroupBy(f => Path.GetDirectoryName(f));
            htmlBuilder.Append("<h2>Sprites</h2>");
            foreach (var group in pngGroups)
            {
                htmlBuilder.Append($"<h3>{Path.GetFileName(group.Key)}</h3><div style='display:flex;flex-wrap:wrap;'>");
                htmlBuilder.Append($"<div style='margin:10px;'><button onclick='openModal(\"{Path.GetFileName(directoryPath)}\", \"{Path.GetFileName(directoryPath) + "/"}\")' style='height: 85px;width: 80px;font-size: 30px;'>+</button></div>");
                htmlBuilder.Append($"<input type='file' id=\"{Path.GetFileName(directoryPath)}\" style='display:none;' accept='.png' />");
                
                foreach (var file in group)
                {
                    string fileName = Path.GetFileName(file);
                    string relativePath = Path.GetRelativePath(_projectObjectsPath, file);
                    string path = relativePath.Replace("\\", "/").Replace(fileName, ""); // Caminho do diretório sem o nome do arquivo
                    string url = $"https://assets.local/{relativePath.Replace("\\", "/")}";
                    htmlBuilder.Append($"" +
                        $"<div style='margin:10px;'>" +
                        $"<img src='{url}' alt='{fileName}' title='{fileName}' style='max-width:100px; max-height:80px;'/>" +
                        $"<p style='text-align: center;'>{fileName}</p>" +
                        $"</div>");
                }
                //htmlBuilder.Append($"<div style='margin:10px;'><button onclick='openModal(\"{Path.GetFileName(group.Key)}\", \"{path}\")' style='height: 85px;width: 80px;font-size: 30px;'>+</button></div>");
                //htmlBuilder.Append($"<input type='file' id=\"{Path.GetFileName(group.Key)}\" style='display:none;' accept='.png' />");
                htmlBuilder.Append("</div>");
            }

            if (pngGroups.Count() == 0)
            {
                htmlBuilder.Append($"<div style='margin:10px;'><button onclick='openModal(\"{Path.GetFileName(directoryPath)}\", \"{Path.GetFileName(directoryPath) + "/"}\")' style='height: 85px;width: 80px;font-size: 30px;'>+</button></div>");
                htmlBuilder.Append($"<input type='file' id=\"{Path.GetFileName(directoryPath)}\" style='display:none;' accept='.png' />");
                htmlBuilder.Append("</div>");
            }
            
            

            // JavaScript para abrir o arquivo
            htmlBuilder.Append(@"
            <script>
                function openFile(url) {
                    window.chrome.webview.postMessage(JSON.stringify({ type: 'openFile', data: url }));
                }
            </script>");

            // HTML e JavaScript para o modal
            htmlBuilder.Append(@"
            <div id='modal' style='display:none; position:fixed; top:50%; left:50%; transform:translate(-50%, -50%); background-color:white; border:1px solid black; padding:20px; box-shadow:0px 0px 10px rgba(0,0,0,0.1); z-index:1000;'>
                <h3>Upload Object</h3>
                <label for='className'>Class Name:</label>
                <input type='text' id='className' style='display:block; margin-bottom:10px;' />
                <label for='fileInput'>Select Image:</label>
                <input type='file' id='fileInput' accept='.png' />
                <div style='margin-top: 20px; text-align: center;'>
                    <button onclick='submitForm()'>Submit</button>
                    <button onclick='closeModal()'>Cancel</button>
                </div>
                
            </div>
            <div id='overlay' style='display:none; position:fixed; top:0; left:0; width:100%; height:100%; background-color:rgba(0,0,0,0.5); z-index:999;'></div>
            <script>
                function openModal(fileInputId, directoryPath) {
                    window.fileInputId = fileInputId;
                    window.directoryPath = directoryPath;
                    document.getElementById('modal').style.display = 'block';
                    document.getElementById('overlay').style.display = 'block';
                }

                function closeModal() {
                    document.getElementById('modal').style.display = 'none';
                    document.getElementById('overlay').style.display = 'none';
                }

                function submitForm() {
                    var className = document.getElementById('className').value;
                    var fileInput = document.getElementById('fileInput');
                    var file = fileInput.files[0];

                    if (file && className) {
                        var reader = new FileReader();
                        reader.onload = function (e) {
                            var imageData = e.target.result;
                            window.chrome.webview.postMessage(JSON.stringify({ 
                                type: 'uploadImage',
                                data: imageData,
                                className: className,
                                dir: window.directoryPath
                            }));
                            closeModal();
                        };
                        reader.readAsDataURL(file);
                    } else {
                        alert('Please enter a class name and select an image.');
                    }
                }
            </script>");

            // Finaliza o HTML
            htmlBuilder.Append("</body></html>");

            // Navega para o conteúdo gerado
            PreviewWebView.NavigateToString(htmlBuilder.ToString());
        }


        private void DisplayContent(string url)
        {
            if (url.EndsWith(".png") || url.EndsWith(".jpg") || url.EndsWith(".gif"))
            {
                PreviewWebView.NavigateToString($@"
                    <html>
                    <body style='margin:0'>
                        <img src='{url}' style='width:100%; height:100%; object-fit:contain;' />
                    </body>
                    </html>");
            }
            else if (url.EndsWith(".js"))
            {
                string scriptContent = File.ReadAllText(new Uri(url).LocalPath);
                PreviewWebView.NavigateToString($@"
                    <html>
                    <body>
                        <pre>{System.Net.WebUtility.HtmlEncode(scriptContent)}</pre>
                    </body>
                    </html>");
            }
            else if (url.EndsWith(".css"))
            {
                string cssContent = File.ReadAllText(new Uri(url).LocalPath);
                PreviewWebView.NavigateToString($@"
                    <html>
                    <head>
                        <style>
                            {System.Net.WebUtility.HtmlEncode(cssContent)}
                        </style>
                    </head>
                    <body>
                        <pre>{System.Net.WebUtility.HtmlEncode(cssContent)}</pre>
                    </body>
                    </html>");
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = AssetsTreeView.SelectedItem as TreeViewItem;
            string selectedPath = selectedItem?.Tag as string;

            if (string.IsNullOrEmpty(selectedPath) || !Directory.Exists(selectedPath))
            {
                MessageBox.Show("Please select a valid directory in the tree view.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Cria a janela de entrada de texto
            Window inputWindow = new Window
            {
                Title = "New Folder",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            // Cria o layout da janela
            StackPanel stackPanel = new StackPanel { Margin = new Thickness(10) };
            TextBlock textBlock = new TextBlock { Text = "Enter the name of the new folder:" };
            TextBox textBox = new TextBox { Width = 250, Margin = new Thickness(0, 10, 0, 10) };
            StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0) };
            Button cancelButton = new Button { Content = "Cancel", Width = 75 };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);
            inputWindow.Content = stackPanel;

            // Adiciona eventos para os botões
            okButton.Click += (s, args) =>
            {
                string folderName = textBox.Text.Trim();
                if (string.IsNullOrEmpty(folderName))
                {
                    MessageBox.Show("Folder name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string newFolderPath = Path.Combine(selectedPath, folderName);
                if (Directory.Exists(newFolderPath))
                {
                    MessageBox.Show("A folder with this name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Directory.CreateDirectory(newFolderPath);

                // Create style.css inside objects
                string cssContent = @"/* Add your CSS styles here */";
                File.WriteAllText(Path.Combine(newFolderPath, "style.css"), cssContent);

                MessageBox.Show($"New folder created: {newFolderPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                inputWindow.DialogResult = true;
                inputWindow.Close();

                // Atualiza a TreeView para mostrar a nova pasta
                // Aqui você pode atualizar a TreeView ou adicionar lógica para refletir a mudança
            };

            cancelButton.Click += (s, args) =>
            {
                inputWindow.DialogResult = false;
                inputWindow.Close();
            };

            inputWindow.ShowDialog();
        }

    }
    /*
private void AddFile_Click(object sender, RoutedEventArgs e)
{
OpenFileDialog openFileDialog = new OpenFileDialog();
if (openFileDialog.ShowDialog() == true)
{
   string destinationPath = Path.Combine(Path.GetDirectoryName(selectedFilePath), Path.GetFileName(openFileDialog.FileName));
   File.Copy(openFileDialog.FileName, destinationPath, true);
   LoadAssetsTreeView();
}
}

private void DeleteFile_Click(object sender, RoutedEventArgs e)
{
if (File.Exists(selectedFilePath))
{
   File.Delete(selectedFilePath);
   LoadAssetsTreeView();
}
}
*/

}
