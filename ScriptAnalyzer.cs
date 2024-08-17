using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditorInternal;

public class ScriptAnalyzer : EditorWindow
{
    private List<string> folderPaths = new List<string>(); // List of folder paths
    private string keyword = "TODO"; // Default keyword
    private int totalLines = 0;
    private int totalCharacters = 0;
    private int totalFiles = 0;
    private int totalComments = 0;
    private int totalComplexity = 0;
    private int keywordTotalCount = 0; // Total count of keyword occurrences
    private int filesContainingKeyword = 0; // Count of files containing the keyword
    private long totalSize = 0; // Total size of all files

    private List<FileMetrics> fileMetricsList = new List<FileMetrics>();

    private Vector2 scrollPosition;
    private float labelWidth = 200f; // Adjusted label width

    private enum SortCriteria
    {
        FileName,
        Size,
        LineCount,
        CharacterCount,
        CommentCount,
        Complexity
    }

    private SortCriteria sortCriteria = SortCriteria.Size;
    private bool sortAscending = true; // Track sort direction

    [MenuItem("Tools/Script Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<ScriptAnalyzer>("Script Analyzer");
    }

    private void OnGUI()
    {
        // Folder paths field and Browse button
        EditorGUILayout.LabelField("Folder(s) to Analyze:");
        EditorGUILayout.BeginVertical();
        for (int i = 0; i < folderPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(folderPaths[i]);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                GUI.FocusControl(null); // Remove focus from input field
                folderPaths.RemoveAt(i);
                i--; // Adjust index after removal
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Folder"))
        {
            GUI.FocusControl(null); // Remove focus from input field
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath) && !folderPaths.Contains(selectedPath))
            {
                folderPaths.Add(selectedPath);
            }
        }
        if (GUILayout.Button("Clear All"))
        {
            GUI.FocusControl(null); // Remove focus from input field
            folderPaths.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // Draw the label
        GUILayout.Label("Keyword (case-sensitive):", GUILayout.Width(160)); // Adjust the width as needed

        // Draw the text field next to the label
        keyword = EditorGUILayout.TextField(keyword, GUILayout.Width(position.width - 172)); // Adjust width based on label width and window size

        // End the horizontal layout
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Analyze"))
        {
            GUI.FocusControl(null); // Remove focus from input field
            AnalyzeScripts();
        }

        GUILayout.Space(10);

        // Display results
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Display keyword analysis results
        DisplayAnalysisSummary();

        GUILayout.Label("All Files", EditorStyles.boldLabel);

        // Display table headers with sorting
        EditorGUILayout.BeginHorizontal();
        DrawSortableColumn("File Name", SortCriteria.FileName);
        DrawSortableColumn("Size (KB)", SortCriteria.Size);
        DrawSortableColumn("Lines", SortCriteria.LineCount);
        DrawSortableColumn("Characters", SortCriteria.CharacterCount);
        DrawSortableColumn("Comments", SortCriteria.CommentCount);
        DrawSortableColumn("Complexity", SortCriteria.Complexity);
        EditorGUILayout.EndHorizontal();

        // Display the file metrics in rows
        foreach (var fileMetrics in fileMetricsList)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fileMetrics.FileName, GUILayout.Width((position.width - 40) * 1 / 6f)); // Adjust width based on window size
            EditorGUILayout.LabelField($"{fileMetrics.Size / 1024f:0.00}", GUILayout.Width((position.width - 40) * 1 / 6f));
            EditorGUILayout.LabelField(fileMetrics.LineCount.ToString(), GUILayout.Width((position.width - 40)   * 1 / 6f));
            EditorGUILayout.LabelField(fileMetrics.CharacterCount.ToString(), GUILayout.Width((position.width - 40) * 1 / 6f));
            EditorGUILayout.LabelField(fileMetrics.CommentCount.ToString(), GUILayout.Width((position.width - 40) * 1 / 6f));
            EditorGUILayout.LabelField(fileMetrics.Complexity.ToString(), GUILayout.Width((position.width - 40) * 1 / 6f));
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        // Display code snippets with keyword highlighting
        GUILayout.Label("Code Snippets with Keyword:", EditorStyles.boldLabel);
        DisplayCodeSnippets();

        EditorGUILayout.EndScrollView();
    }

    private void DrawSortableColumn(string header, SortCriteria criteria)
    {
        // Determine the arrow based on sort direction
        string sortArrow = "";
        if (sortCriteria == criteria)
        {
            sortArrow = sortAscending ? " ↑" : " ↓";
        }

        // Draw the button with header and arrow
        if (GUILayout.Button(header + sortArrow, GUILayout.Width((position.width - 40) * 1 / 6f))) // Adjust width based on window size
        {
            GUI.FocusControl(null); // Remove focus from input field
            if (sortCriteria == criteria)
            {
                sortAscending = !sortAscending; // Toggle sort direction
            }
            else
            {
                sortCriteria = criteria;
                sortAscending = true; // Reset to ascending when changing criteria
            }
            SortFileMetrics();
        }
    }

    private void DisplayAnalysisSummary()
    {
        labelWidth = (position.width - 40) * 1 / 2f;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Total Count of '{keyword}':", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(keywordTotalCount.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Files Containing '{keyword}':", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(filesContainingKeyword.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Files:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(totalFiles.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Lines:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(totalLines.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Characters:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(totalCharacters.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Comments:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(totalComments.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Comment-to-Code Ratio:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(((float)totalComments / totalLines).ToString("P2"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Cyclomatic Complexity:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField(totalComplexity.ToString());
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total Size:", GUILayout.Width(labelWidth));
        EditorGUILayout.LabelField($"{totalSize / 1024f:0.00} KB");
        EditorGUILayout.EndHorizontal();
    }

    private void DisplayCodeSnippets()
    {
        // Define a custom GUIStyle for colored text
        GUIStyle keywordStyle = new GUIStyle();
        keywordStyle.richText = true; // Enable rich text
        keywordStyle.wordWrap = true; // Ensure text wraps correctly
        keywordStyle.normal.textColor = Color.gray; // Default text color (ensure visibility)

        foreach (var fileMetrics in fileMetricsList)
        {
            if (fileMetrics.Snippets.Any())
            {
                foreach (var snippet in fileMetrics.Snippets)
                {
                    EditorGUILayout.BeginHorizontal();

                    // Add the "Go" button on the left side
                    if (GUILayout.Button("Open", GUILayout.Width(40)))
                    {
                        OpenFileAtLine(fileMetrics.FilePath, snippet.LineNumber);
                    }

                    // Display the file name and line number
                    EditorGUILayout.LabelField($"{fileMetrics.FileName} (Line {snippet.LineNumber}):", EditorStyles.boldLabel);

                    EditorGUILayout.EndHorizontal();

                    // Highlight the keyword in the snippet
                    string highlightedSnippet = HighlightSyntax(snippet.SnippetText);

                    // Use Label to render the rich text
                    EditorGUILayout.LabelField(highlightedSnippet, keywordStyle, GUILayout.Height(40));
                }
            }
        }
    }

    private void OpenFileAtLine(string filePath, int lineNumber)
    {
        if (File.Exists(filePath))
        {
            InternalEditorUtility.OpenFileAtLineExternal(filePath, lineNumber);
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
        }
    }

    private string HighlightSyntax(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Define patterns for keywords, comments, and strings
        string keywordPattern = Regex.Escape(keyword);
        string commentPattern = @"//.*?$|/\*.*?\*/";
        string stringPattern = @"""[^""\\]*(?:\\.[^""\\]*)*""";

        // Apply rich text for highlighting
        string result = text;

        // Highlight comments
        result = Regex.Replace(result, commentPattern, match => $"<color=green>{match.Value}</color>");

        // Highlight strings
        result = Regex.Replace(result, stringPattern, match => $"<color=orange>{match.Value}</color>");

        // Highlight keywords
        result = Regex.Replace(result, keywordPattern, match => $"<color=yellow>{match.Value}</color>");

        return result;
    }

    private void AnalyzeScripts()
    {
        totalLines = 0;
        totalCharacters = 0;
        totalFiles = 0;
        totalComments = 0;
        totalComplexity = 0;
        keywordTotalCount = 0;
        filesContainingKeyword = 0;
        totalSize = 0;
        fileMetricsList.Clear();

        foreach (var folderPath in folderPaths)
        {
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
                totalFiles += files.Length;

                foreach (string file in files)
                {
                    AnalyzeFile(file);
                }
            }
            else
            {
                Debug.LogError("Folder path does not exist: " + folderPath);
            }
        }

        SortFileMetrics(); // Ensure the file metrics are sorted based on the initial criteria
    }

    private void AnalyzeFile(string file)
    {
        string[] lines = File.ReadAllLines(file);
        int fileLineCount = lines.Length;
        int fileCommentCount = 0;
        int fileComplexity = 0;
        int fileCharacterCount = lines.Sum(line => line.Length);
        int fileKeywordCount = 0;
        bool containsKeyword = false;

        // Get file size
        long fileSize = new FileInfo(file).Length;
        totalSize += fileSize;

        var snippets = new List<CodeSnippet>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            // Check for keyword occurrences
            int keywordCountInLine = Regex.Matches(line, Regex.Escape(keyword)).Count;
            fileKeywordCount += keywordCountInLine;

            if (keywordCountInLine > 0)
            {
                containsKeyword = true;

                // Collect snippet around keyword
                int snippetStart = Mathf.Max(0, i); // 2 lines before
                int snippetEnd = Mathf.Min(lines.Length - 1, i); // 2 lines after

                var snippetCode = string.Join("\n", lines.Skip(snippetStart).Take(snippetEnd - snippetStart + 1));
                snippets.Add(new CodeSnippet
                {
                    LineNumber = i + 1, // +1 for 1-based line numbers
                    SnippetText = snippetCode // Updated to use SnippetText
                });
            }

            // Estimate comments
            if (line.TrimStart().StartsWith("//") || line.Contains("/*") || line.Contains("*/"))
            {
                fileCommentCount++;
            }

            // Estimate cyclomatic complexity (simple heuristic)
            if (Regex.IsMatch(line, @"\b(if|else if|for|while|case|catch)\b"))
            {
                fileComplexity++;
            }
        }

        if (containsKeyword)
        {
            filesContainingKeyword++;
            keywordTotalCount += fileKeywordCount;
        }

        totalLines += fileLineCount;
        totalCharacters += fileCharacterCount;
        totalComments += fileCommentCount;
        totalComplexity += fileComplexity;

        fileMetricsList.Add(new FileMetrics
        {
            FilePath = file,
            FileName = Path.GetFileName(file),
            LineCount = fileLineCount,
            CommentCount = fileCommentCount,
            Complexity = fileComplexity,
            CharacterCount = fileCharacterCount,
            Size = fileSize,
            Snippets = snippets
        });
    }

    private void SortFileMetrics()
    {
        switch (sortCriteria)
        {
            case SortCriteria.FileName:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.FileName).ToList() : fileMetricsList.OrderByDescending(f => f.FileName).ToList();
                break;
            case SortCriteria.Size:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.Size).ToList() : fileMetricsList.OrderByDescending(f => f.Size).ToList();
                break;
            case SortCriteria.LineCount:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.LineCount).ToList() : fileMetricsList.OrderByDescending(f => f.LineCount).ToList();
                break;
            case SortCriteria.CharacterCount:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.CharacterCount).ToList() : fileMetricsList.OrderByDescending(f => f.CharacterCount).ToList();
                break;
            case SortCriteria.CommentCount:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.CommentCount).ToList() : fileMetricsList.OrderByDescending(f => f.CommentCount).ToList();
                break;
            case SortCriteria.Complexity:
                fileMetricsList = sortAscending ? fileMetricsList.OrderBy(f => f.Complexity).ToList() : fileMetricsList.OrderByDescending(f => f.Complexity).ToList();
                break;
        }
    }

    private class FileMetrics
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int LineCount { get; set; }
        public int CommentCount { get; set; }
        public int Complexity { get; set; }
        public int CharacterCount { get; set; }
        public long Size { get; set; } // File size in bytes
        public List<CodeSnippet> Snippets { get; set; } = new List<CodeSnippet>();
    }

    private class CodeSnippet
    {
        public int LineNumber { get; set; }
        public string SnippetText { get; set; }
    }
}
