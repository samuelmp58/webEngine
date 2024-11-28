using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace webEngine
{
    public class Compiler
    {
        private string _projectPath;
        private WebView2 _webView;

        // Construtor da classe Compiler
        public Compiler(WebView2 webView, string projectPath)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _projectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        }

        public string ReadMainJs()
        {
            string mainJsPath = Path.Combine(_projectPath, "main.js");
            try
            {
                // Lê o conteúdo do arquivo main.js
                string content = File.ReadAllText(mainJsPath);
                return content;
            }
            catch (Exception ex)
            {
                // Lida com qualquer erro que possa ocorrer durante a leitura do arquivo
                Console.WriteLine($"Ocorreu um erro ao ler o arquivo: {ex.Message}");
                return string.Empty;
            }
        }
        private void InsertScriptsTags(ref string html)
        {
            string pattern = @"#include\s*""([^""]*)""";
            Regex regex = new Regex(pattern);

            // Caminho dos arquivos
            string mainJsPath = Path.Combine(_projectPath, "main.js");
            string modifiedMainJsPath = Path.Combine(_projectPath, "scripts", "main.js");

            // Leia o conteúdo do arquivo main.js
            string mainContent = File.ReadAllText(mainJsPath);

            // Lista para armazenar os caminhos dos scripts extraídos
            List<string> scriptPaths = new List<string>();

            // Remove as linhas #include do conteúdo e extrai os caminhos dos scripts
            string modifiedMainContent = regex.Replace(mainContent, match =>
            {
                string relativePath = match.Groups[1].Value;
                scriptPaths.Add(relativePath); // Adiciona o caminho extraído à lista
                return ""; // Remove a linha do conteúdo
            });

            // Salva o conteúdo modificado sem as linhas #include
            File.WriteAllText(modifiedMainJsPath, modifiedMainContent);

            // Adiciona o caminho principal ao array de scripts
            scriptPaths.Add("scripts/main.js");

            // Adiciona as tags <script> ao final do HTML
            StringBuilder scriptTags = new StringBuilder();
            foreach (var script in scriptPaths.Distinct())
            {
                scriptTags.AppendLine($"<script src=\"{script}\"></script>");
            }

            // Insere as tags de script antes do fechamento da tag </body>
            int bodyIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (bodyIndex != -1)
            {
                html = html.Insert(bodyIndex, scriptTags.ToString());
            }
            else
            {
                // Se não houver a tag </body>, adiciona as tags de script no final do HTML
                html += scriptTags.ToString();
            }
        }



        // Método para compilar e salvar o HTML
        public async void Compile()
        {
            // Executar o script para obter o HTML como texto
            string htmlText = await _webView.ExecuteScriptAsync("document.documentElement.outerHTML;");

            // Decodificar os caracteres JSON escapados
            htmlText = DecodeJsonEscapedString(htmlText);

            InsertScriptsTags(ref htmlText);

            // Definir o caminho e nome do arquivo
            string filePath = Path.Combine(_projectPath, "index.html");

            // Salvar o conteúdo HTML em um arquivo
            try
            {
                // Usar StreamWriter para criar e escrever o arquivo
                using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.Write(htmlText);
                }

                // Informar ao usuário que o arquivo foi salvo
                MessageBox.Show($"HTML content has been saved to {filePath}", "File Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                // Mostrar uma mensagem de erro se algo der errado
                MessageBox.Show($"An error occurred while saving the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para decodificar caracteres JSON escapados
        public static string DecodeJsonEscapedString(string jsonString)
        {
            // Decodificar o conteúdo JSON escapado para uma string normal
            return JsonConvert.DeserializeObject<string>(jsonString);
        }
    }
}
