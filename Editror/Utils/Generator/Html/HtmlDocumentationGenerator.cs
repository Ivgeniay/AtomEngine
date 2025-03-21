using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Editor
{
    public class HtmlDocumentationGenerator
    {
        public string GenerateDocumentation(List<DocumentInfo> documents, DocumentTreeNode rootNode)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ru\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("    <title>AtomEngine Documentation</title>");
            sb.AppendLine("    <style>");
            sb.AppendLine(GetCssStyles());
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("    <header>");
            sb.AppendLine("        <h1>AtomEngine Documentation</h1>");
            sb.AppendLine("    </header>");

            sb.AppendLine("    <div class=\"container\">");

            sb.AppendLine("        <nav class=\"sidebar\">");
            sb.AppendLine("            <h2>Категории</h2>");
            sb.AppendLine("            <ul class=\"tree\">");

            GenerateCategoryTree(sb, rootNode.Children, "");

            sb.AppendLine("            </ul>");
            sb.AppendLine("        </nav>");

            sb.AppendLine("        <main class=\"content\">");

            var rootDocuments = documents.Where(d => string.IsNullOrEmpty(d.Section)).ToList();
            if (rootDocuments.Any())
            {
                sb.AppendLine("            <div class=\"category\">");
                sb.AppendLine("                <h2>Корневая документация</h2>");

                foreach (var doc in rootDocuments)
                {
                    GenerateDocumentHtml(sb, doc);
                }

                sb.AppendLine("            </div>");
            }

            var sections = documents.Where(d => !string.IsNullOrEmpty(d.Section))
                                   .Select(d => d.Section)
                                   .Distinct()
                                   .OrderBy(s => s);

            foreach (var section in sections)
            {
                sb.AppendLine($"            <div class=\"category\" id=\"{SanitizeId(section)}\">");
                sb.AppendLine($"                <h2>{section}</h2>");

                var sectionDocs = documents.Where(d => d.Section == section && string.IsNullOrEmpty(d.SubSection)).ToList();
                foreach (var doc in sectionDocs)
                {
                    GenerateDocumentHtml(sb, doc);
                }

                var subSections = documents.Where(d => d.Section == section && !string.IsNullOrEmpty(d.SubSection))
                                        .Select(d => d.SubSection)
                                        .Distinct()
                                        .OrderBy(s => s);

                foreach (var subSection in subSections)
                {
                    var subSectionId = $"{SanitizeId(section)}-{SanitizeId(subSection)}";
                    sb.AppendLine($"                <div class=\"subsection\" id=\"{subSectionId}\">");
                    sb.AppendLine($"                    <h3>{subSection}</h3>");

                    var subSectionDocs = documents.Where(d => d.Section == section && d.SubSection == subSection).ToList();
                    foreach (var doc in subSectionDocs)
                    {
                        GenerateDocumentHtml(sb, doc);
                    }

                    sb.AppendLine("                </div>");
                }

                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </main>");
            sb.AppendLine("    </div>");

            sb.AppendLine("    <script>");
            sb.AppendLine(GetJavaScript());
            sb.AppendLine("    </script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private void GenerateCategoryTree(StringBuilder sb, List<DocumentTreeNode> nodes, string indent)
        {
            foreach (var node in nodes)
            {
                if (node.IsCategory)
                {
                    sb.AppendLine($"{indent}                <li class=\"folder\">");
                    sb.AppendLine($"{indent}                    <span class=\"category-name\">{node.Name}</span>");

                    if (node.Children.Count > 0)
                    {
                        sb.AppendLine($"{indent}                    <ul>");
                        GenerateCategoryTree(sb, node.Children, indent + "    ");
                        sb.AppendLine($"{indent}                    </ul>");
                    }

                    sb.AppendLine($"{indent}                </li>");
                }
                else
                {
                    var docId = SanitizeId(node.Document.Name);
                    sb.AppendLine($"{indent}                <li class=\"file\">");
                    sb.AppendLine($"{indent}                    <a href=\"#{docId}\">{node.DisplayName}</a>");
                    sb.AppendLine($"{indent}                </li>");
                }
            }
        }

        private void GenerateDocumentHtml(StringBuilder sb, DocumentInfo doc)
        {
            var docId = SanitizeId(doc.Name);

            sb.AppendLine($"                <div class=\"document\" id=\"{docId}\">");
            sb.AppendLine($"                    <h3>{doc.Name}</h3>");
            sb.AppendLine($"                    <h4>{doc.Title}</h4>");

            if (!string.IsNullOrEmpty(doc.Author))
            {
                sb.AppendLine($"                    <p class=\"author\">Автор: {doc.Author}</p>");
            }

            if (!string.IsNullOrEmpty(doc.Description))
            {
                var processedDescription = ProcessLinksInDescription(doc.Description);
                sb.AppendLine($"                    <div class=\"description\">{processedDescription}</div>");
            }

            sb.AppendLine("                </div>");
        }

        private string ProcessLinksInDescription(string description)
        {
            return Regex.Replace(description, @"<a>(.*?)</a>", match =>
            {
                var linkText = match.Groups[1].Value;
                var linkId = SanitizeId(linkText);
                return $"<a href=\"#{linkId}\">{linkText}</a>";
            });
        }

        private string SanitizeId(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9-_]", "-").ToLowerInvariant();
        }

        private string GetCssStyles()
        {
            return @"
/* Основные стили */
:root {
    --bg-color: #1E1E1E;
    --text-color: #CCCCCC;
    --header-color: #E6E6E6;
    --link-color: #569CD6;
    --link-hover-color: #9CDCFE;
    --border-color: #3F3F46;
    --sidebar-bg: #252526;
    --card-bg: #2D2D30;
    --hover-bg: #3E3E42;
    --selected-bg: #0E639C;
}

body {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    background-color: var(--bg-color);
    color: var(--text-color);
    margin: 0;
    padding: 0;
    line-height: 1.5;
}

.container {
    display: flex;
    max-width: 1600px;
    margin: 0 auto;
    height: calc(100vh - 80px);
    overflow: hidden;
}

header {
    background-color: var(--card-bg);
    padding: 1rem;
    border-bottom: 1px solid var(--border-color);
    text-align: center;
}

h1, h2, h3, h4 {
    color: var(--header-color);
    margin: 0 0 1rem 0;
}

h1 {
    font-size: 1.8rem;
}

h2 {
    font-size: 1.5rem;
}

h3 {
    font-size: 1.3rem;
}

h4 {
    font-size: 1.1rem;
}

a {
    color: var(--link-color);
    text-decoration: none;
}

a:hover {
    color: var(--link-hover-color);
    text-decoration: underline;
}

/* Боковая панель */
.sidebar {
    flex: 0 0 300px;
    background-color: var(--sidebar-bg);
    padding: 1rem;
    overflow-y: auto;
    border-right: 1px solid var(--border-color);
}

.tree {
    list-style-type: none;
    padding: 0 0 0 1rem;
    margin: 0;
}

.folder > span {
    cursor: pointer;
    display: block;
    padding: 0.25rem 0;
}

.folder > span:hover {
    color: var(--header-color);
}

.folder > span::before {
    content: '📁 ';
}

.folder > ul {
    display: none;
    padding-left: 1.5rem;
}

.folder.open > ul {
    display: block;
}

.folder.open > span::before {
    content: '📂 ';
}

.file {
    padding: 0.25rem 0 0.25rem 0.5rem;
}

.file::before {
    content: '📄 ';
}

/* Основной контент */
.content {
    flex: 1;
    padding: 1rem;
    overflow-y: auto;
}

.category {
    margin-bottom: 2rem;
    border-left: 3px solid var(--selected-bg);
    padding-left: 1rem;
}

.subsection {
    margin: 1rem 0;
    padding-left: 1rem;
    border-left: 1px solid var(--border-color);
}

.document {
    background-color: var(--card-bg);
    border-radius: 4px;
    padding: 1rem;
    margin-bottom: 1rem;
    border: 1px solid var(--border-color);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
}

.document:target {
    border-color: var(--selected-bg);
    box-shadow: 0 0 8px var(--selected-bg);
}

.author {
    font-style: italic;
    color: #AAAAAA;
    margin-bottom: 1rem;
}

.description {
    line-height: 1.5;
}

/* Адаптивный дизайн */
@media (max-width: 1024px) {
    .container {
        flex-direction: column;
        height: auto;
    }
    
    .sidebar {
        flex: 0 0 auto;
        max-height: 300px;
        width: 100%;
        border-right: none;
        border-bottom: 1px solid var(--border-color);
    }
}
";
        }

        private string GetJavaScript()
        {
            return @"
// Обработка кликов по категориям
document.addEventListener('DOMContentLoaded', function() {
    const folders = document.querySelectorAll('.folder > span');
    
    folders.forEach(folder => {
        folder.addEventListener('click', function() {
            this.parentNode.classList.toggle('open');
        });
    });
    
    // По умолчанию открываем все категории первого уровня
    document.querySelectorAll('.sidebar > .tree > .folder').forEach(folder => {
        folder.classList.add('open');
    });
    
    // Обработка якорных ссылок для плавной прокрутки
    document.querySelectorAll('a[href^=""#""]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href').substring(1);
            const targetElement = document.getElementById(targetId);
            
            if (targetElement) {
                targetElement.scrollIntoView({
                    behavior: 'smooth'
                });
                
                // Добавляем подсветку для визуального выделения
                targetElement.classList.add('highlight');
                setTimeout(() => {
                    targetElement.classList.remove('highlight');
                }, 2000);
            }
        });
    });
});
";
        }
    }
}