using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;

namespace ComponentGenerator
{
    public static class TypeExtensions
    {
        public static bool IsPrimitive(this ITypeSymbol type)
        {
            if (type == null)
                return true;

            if (type.TypeKind == TypeKind.Enum)
                return true;

            return type.SpecialType == SpecialType.System_Boolean ||
                   type.SpecialType == SpecialType.System_Byte ||
                   type.SpecialType == SpecialType.System_SByte ||
                   type.SpecialType == SpecialType.System_Int16 ||
                   type.SpecialType == SpecialType.System_UInt16 ||
                   type.SpecialType == SpecialType.System_Int32 ||
                   type.SpecialType == SpecialType.System_UInt32 ||
                   type.SpecialType == SpecialType.System_Int64 ||
                   type.SpecialType == SpecialType.System_UInt64 ||
                   type.SpecialType == SpecialType.System_IntPtr ||
                   type.SpecialType == SpecialType.System_UIntPtr ||
                   type.SpecialType == SpecialType.System_Char ||
                   type.SpecialType == SpecialType.System_Double ||
                   type.SpecialType == SpecialType.System_Single;
        }
    }

    [Generator]
    public class ComponentSourceGenerator : ISourceGenerator
    {
        private const string IComponentTypeName = "IComponent";

        // Types to ignore when generating GUID fields
        private static readonly HashSet<string> IgnoredTypes = new HashSet<string>
        {
            "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint",
            "long", "ulong", "short", "ushort", 
            
            "string", "String", "System.String"
        };

        public void Initialize(GeneratorInitializationContext context)
        {
            // Debugger.Launch();
            Console.WriteLine("Source Generator Initialized");

            var receiver = new SyntaxReceiver();
            context.RegisterForSyntaxNotifications(() => receiver);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            ReportMessage(context, "CG001", "Receiver",
                    "Старт генерации", DiagnosticSeverity.Warning);

            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            {
                ReportMessage(context, "CG002", "Receiver Error",
                    "Не удалось получить SyntaxReceiver", DiagnosticSeverity.Error);
                return;
            }

            ReportMessage(context, "CG001", "Receiver",
                    $"Total found components: {receiver.FoundComponents.Count}", DiagnosticSeverity.Warning);
            foreach (var componentStruct in receiver.FoundComponents)
            {
                ReportMessage(context, "CG001", "Receiver",
                    $"Processing component: {componentStruct.StructSyntax.Identifier}", DiagnosticSeverity.Warning);

                try
                {
                    ProcessComponent(context, componentStruct);
                }
                catch (Exception ex)
                {
                    //ReportMessage(context, "CG005", "Component Processing Error",
                    //    $"Ошибка при обработке компонента {componentStruct.StructSyntax.Identifier.Text}: {ex.Message}",
                    //    DiagnosticSeverity.Error);
                }
            }
        }

        private static void ReportMessage(GeneratorExecutionContext context, string id, string title, string message, DiagnosticSeverity severity)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    id,
                    title,
                    message,
                    "ComponentGenerator",
                    severity,
                    isEnabledByDefault: true),
                Location.None));
        }

        private void ProcessComponent(
            GeneratorExecutionContext context,
            (StructDeclarationSyntax StructSyntax, INamedTypeSymbol Symbol) component)
        {
            var structSyntax = component.StructSyntax;
            var symbol = component.Symbol;

            string namespaceValue = GetNamespace(structSyntax);

            var membersToAddGuid = GetMembersRequiringGuids(symbol);

            if (membersToAddGuid.Count == 0)
            {
                return;
            }

            var generatedCode = GeneratePartialClass(namespaceValue, structSyntax.Identifier.Text, membersToAddGuid);
            string fileName = $"{structSyntax.Identifier.Text}_GeneratedGUIDs.g.cs";

            try
            {
                context.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));

                ReportMessage(context, "CG010", "Source Added",
                    $"Код успешно добавлен в компиляцию: {fileName}", DiagnosticSeverity.Warning);
            }
            catch (Exception ex)
            {
                //ReportMessage(context, "CG011", "Source Addition Error",
                //    $"Ошибка при добавлении сгенерированного кода в компиляцию: {ex.Message}", DiagnosticSeverity.Error);
            }
        }

        private List<ISymbol> GetMembersRequiringGuids(INamedTypeSymbol symbol)
        {
            var membersToAddGuid = new List<ISymbol>();
            var existingMembers = new HashSet<string>(
                symbol.GetMembers()
                    .Where(m => m.Name.EndsWith("GUID"))
                    .Select(m => m.Name.Substring(0, m.Name.Length - 4))
            );

            foreach (var member in symbol.GetMembers())
            {
                if (member.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (member is IFieldSymbol fieldSymbol && !fieldSymbol.IsStatic)
                {
                    if (!ShouldIgnoreType(fieldSymbol.Type) &&
                        !existingMembers.Contains(fieldSymbol.Name))
                    {
                        membersToAddGuid.Add(fieldSymbol);
                    }
                }
                else if (member is IPropertySymbol propertySymbol && !propertySymbol.IsStatic)
                {
                    if (propertySymbol.GetMethod != null && propertySymbol.SetMethod != null &&
                        !ShouldIgnoreType(propertySymbol.Type) &&
                        !existingMembers.Contains(propertySymbol.Name))
                    {
                        membersToAddGuid.Add(propertySymbol);
                    }
                }
            }

            return membersToAddGuid;
        }

        //private List<ISymbol> GetMembersRequiringGuids(INamedTypeSymbol symbol)
        //{
        //    var membersToAddGuid = new List<ISymbol>();

        //    try
        //    {
        //        foreach (var member in symbol.GetMembers())
        //        {
        //            if (member.DeclaredAccessibility != Accessibility.Public)
        //                continue;

        //            if (member is IFieldSymbol fieldSymbol && !fieldSymbol.IsStatic)
        //            {
        //                if (!ShouldIgnoreType(fieldSymbol.Type))
        //                {
        //                    membersToAddGuid.Add(fieldSymbol);
        //                }
        //            }
        //            else if (member is IPropertySymbol propertySymbol && !propertySymbol.IsStatic)
        //            {
        //                if (propertySymbol.GetMethod != null && propertySymbol.SetMethod != null &&
        //                    !ShouldIgnoreType(propertySymbol.Type))
        //                {
        //                    membersToAddGuid.Add(propertySymbol);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }

        //    return membersToAddGuid;
        //}

        private bool ShouldIgnoreType(ITypeSymbol type)
        {
            if (type == null)
                return true;

            try
            {
                if (IgnoredTypes.Contains(type.Name))
                    return true;

                if (IgnoredTypes.Contains(type.ToDisplayString()))
                    return true;

                return type.IsValueType && type.IsPrimitive();
            }
            catch (Exception)
            {
                return true;
            }
        }


        private string GetNamespace(SyntaxNode syntaxNode)
        {
            try
            {
                NamespaceDeclarationSyntax namespaceDeclaration = syntaxNode.Ancestors()
                                                                            .OfType<NamespaceDeclarationSyntax>()
                                                                            .FirstOrDefault();

                BaseNamespaceDeclarationSyntax fileScopedNamespace = syntaxNode.Ancestors()
                                                                               .OfType<FileScopedNamespaceDeclarationSyntax>()
                                                                               .FirstOrDefault();

                if (fileScopedNamespace != null)
                {
                    return fileScopedNamespace.Name.ToString();
                }

                return namespaceDeclaration?.Name.ToString() ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string GeneratePartialClass(string namespaceValue, string structName, List<ISymbol> members)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This code was generated by ComponentGenerator");
            sb.AppendLine("// Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();

            sb.AppendLine("using System;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(namespaceValue))
            {
                sb.AppendLine($"namespace {namespaceValue}");
                sb.AppendLine("{");
            }

            string indent = string.IsNullOrEmpty(namespaceValue) ? "" : "    ";

            sb.AppendLine($"{indent}public partial struct {structName}");
            sb.AppendLine($"{indent}{{");

            foreach (var member in members)
            {
                string memberName = member.Name;
                string memberType = "Unknown";
                string guidFieldName = $"{memberName}GUID";

                if (member is IFieldSymbol fieldSymbol)
                {
                    memberType = fieldSymbol.Type.ToDisplayString();
                }
                else if (member is IPropertySymbol propertySymbol)
                {
                    memberType = propertySymbol.Type.ToDisplayString();
                }

                // Добавляем GUID-поле только если его еще нет
                sb.AppendLine($"{indent}    /// <summary>");
                sb.AppendLine($"{indent}    /// GUID поле для {memberName} типа {memberType}");
                sb.AppendLine($"{indent}    /// </summary>");
                sb.AppendLine($"{indent}    public string {guidFieldName};");
                sb.AppendLine();
            }

            sb.AppendLine($"{indent}}}");

            if (!string.IsNullOrEmpty(namespaceValue))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        //private string GeneratePartialClass(string namespaceValue, string structName, List<ISymbol> members)
        //{
        //    var sb = new StringBuilder();

        //    sb.AppendLine("// <auto-generated>");
        //    sb.AppendLine("// This code was generated by ComponentGenerator");
        //    sb.AppendLine("// Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //    sb.AppendLine("// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated");
        //    sb.AppendLine("// </auto-generated>");
        //    sb.AppendLine();

        //    sb.AppendLine("using System;");
        //    sb.AppendLine();

        //    if (!string.IsNullOrEmpty(namespaceValue))
        //    {
        //        sb.AppendLine($"namespace {namespaceValue}");
        //        sb.AppendLine("{");
        //    }

        //    string indent = string.IsNullOrEmpty(namespaceValue) ? "" : "    ";

        //    sb.AppendLine($"{indent}public partial struct {structName}");
        //    sb.AppendLine($"{indent}{{");

        //    foreach (var member in members)
        //    {
        //        string memberName = member.Name;
        //        string memberType = "Unknown";

        //        if (member is IFieldSymbol fieldSymbol)
        //        {
        //            memberType = fieldSymbol.Type.ToDisplayString();
        //        }
        //        else if (member is IPropertySymbol propertySymbol)
        //        {
        //            memberType = propertySymbol.Type.ToDisplayString();
        //        }

        //        sb.AppendLine($"{indent}    /// <summary>");
        //        sb.AppendLine($"{indent}    /// GUID поле для {memberName} типа {memberType}");
        //        sb.AppendLine($"{indent}    /// </summary>");
        //        sb.AppendLine($"{indent}    public string {memberName}GUID;");
        //        sb.AppendLine();
        //    }

        //    sb.AppendLine($"{indent}}}");

        //    if (!string.IsNullOrEmpty(namespaceValue))
        //    {
        //        sb.AppendLine("}");
        //    }

        //    return sb.ToString();
        //}

        private class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<(StructDeclarationSyntax StructSyntax, INamedTypeSymbol Symbol)> FoundComponents { get; } =
                new List<(StructDeclarationSyntax, INamedTypeSymbol)>();

            public int VisitedNodes { get; private set; } = 0;
            public int StructsFound { get; private set; } = 0;
            public int PartialStructsFound { get; private set; } = 0;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                VisitedNodes++;

                if (context.Node is StructDeclarationSyntax structDeclarationSyntax)
                {
                    StructsFound++;

                    if (structDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        PartialStructsFound++;

                        try
                        {
                            var semanticModel = context.SemanticModel;
                            var structSymbol = semanticModel.GetDeclaredSymbol(structDeclarationSyntax) as INamedTypeSymbol;

                            if (structSymbol != null && ImplementsIComponent(structSymbol))
                            {
                                FoundComponents.Add((structDeclarationSyntax, structSymbol));
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }

            private bool ImplementsIComponent(INamedTypeSymbol structSymbol)
            {
                try
                {
                    foreach (var interfaceSymbol in structSymbol.AllInterfaces)
                    {
                        if (interfaceSymbol.Name == IComponentTypeName)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                }
                return false;
            }
        }
    }
}




