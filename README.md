# Script Analyzer

`Script Analyzer` is a Unity Editor tool designed to analyze and provide detailed metrics about your C# scripts. It offers insights into file sizes, line counts, comment density, cyclomatic complexity, and keyword occurrences within your scripts. 

## Features

- **Folder Selection**: Select multiple folders to analyze all the C# files within them.
- **Keyword Search**: Specify a keyword (case-sensitive) to find its occurrences across all analyzed files.
- **Detailed Metrics**:
  - File size (in KB)
  - Number of lines
  - Number of characters
  - Comment count and comment-to-code ratio
  - Cyclomatic complexity estimation
- **Code Snippets**: Display code snippets surrounding the keyword occurrences.
- **Sorting**: Sort files by name, size, line count, character count, comment count, or complexity.

## Installation

1. Clone or download this repository to your local machine.
2. Copy the `ScriptAnalyzer.cs` file into your Unity project's `Editor` folder (create it if it doesn't exist).
3. Open Unity. The `Script Analyzer` tool will be available under the `Tools` menu.

## Usage

1. Open the `Script Analyzer` from the Unity Editor: `Tools > Script Analyzer`.
2. Click `Add Folder` to select one or more folders containing C# scripts you want to analyze.
3. Optionally, enter a keyword to search for specific terms within your scripts.
4. Click `Analyze` to start the analysis.
5. The results will be displayed in a table with sortable columns. Click on column headers to sort by the respective metric.
6. Below the table, view code snippets where the keyword appears. Click `Open` to jump directly to the line in the code.

## Contributing

Feel free to fork the repository and submit pull requests with improvements or bug fixes. Please ensure your changes are well-tested before submitting.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
