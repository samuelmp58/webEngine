using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public class CssFileHandler
{
    private string _projectObjectsPath;

    public CssFileHandler(string projectObjectsPath)
    {
        _projectObjectsPath = projectObjectsPath;
    }

    public static int FindClassLine(string filePath, string className)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSS file not found.", filePath);

        int lineNumber = 0;
        foreach (var line in File.ReadLines(filePath))
        {
            lineNumber++;
            if (line.Contains($".{className}"))
            {
                return lineNumber;
            }
        }

        return -1; // Class not found
    }

    public static void OpenFileAtLine(string filePath, int lineNumber)
    {

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        // Argumentos para abrir o arquivo no Notepad++ na linha especificada
        string arguments = $"\"{filePath}\" -n{lineNumber}";

        try
        {
            // Certifique-se de que o Notepad++ está no PATH do sistema ou forneça o caminho completo
            string notepadPlusPlusPath = @"notepad\notepad++.exe"; // Substitua pelo caminho do Notepad++ se necessário

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = notepadPlusPlusPath,
                Arguments = arguments
            };

            Process.Start(psi);
            System.Diagnostics.Debug.WriteLine($"Executed command: {psi.FileName} {psi.Arguments}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start process: {ex.Message}");
        }
    }

    public void AddClassToCssFile(string dir, string className, string imageFilePath)
    {
        // Caminho do arquivo CSS
        string cssFilePath = Path.Combine(_projectObjectsPath, dir, "style.css");

        // Verifica se o arquivo CSS existe
        if (!File.Exists(cssFilePath))
        {
            Console.WriteLine($"O arquivo CSS não foi encontrado em {cssFilePath}");
            return;
        }

        // Lê o conteúdo do arquivo CSS
        string cssContent = File.ReadAllText(cssFilePath);

        // Cria uma nova regra CSS
        string newCssRule = $@"
.{className} {{
    background-image: url('{imageFilePath}');
    width: 200px;
    height: 200px;
    background-size: contain; 
    background-repeat: no-repeat; /* A imagem não se repete */
    background-position: center; /* A imagem está centralizada */
}}";

        // Adiciona a nova regra CSS ao conteúdo existente
        cssContent += newCssRule;

        // Salva o conteúdo modificado de volta ao arquivo CSS
        File.WriteAllText(cssFilePath, cssContent);

        Console.WriteLine("Regra CSS adicionada com sucesso!");
    }


    public void DeleteClassFromCssFile(string dir, string className)
    {
        // Caminho do arquivo CSS
        string cssFilePath = Path.Combine(_projectObjectsPath, dir, "style.css");

        // Verifica se o arquivo CSS existe
        if (!File.Exists(cssFilePath))
        {
            Console.WriteLine($"O arquivo CSS não foi encontrado em {cssFilePath}");
            return;
        }

        // Lê o conteúdo do arquivo CSS
        string cssContent = File.ReadAllText(cssFilePath);

        // Regex para encontrar a regra da classe
        string pattern = @$"\.{className}\s*\{{[^}}]*\}}";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Remove a regra da classe
        cssContent = regex.Replace(cssContent, string.Empty);

        // Salva o conteúdo modificado de volta ao arquivo CSS
        File.WriteAllText(cssFilePath, cssContent);

        Console.WriteLine("Regra CSS removida com sucesso!");
    }
}
