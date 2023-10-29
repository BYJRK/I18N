﻿using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Antelcat.Wpf.I18N.SourceGenerators.Generators;

[Generator(LanguageNames.CSharp)]
internal class ResourceKeysGenerator : AttributeDetectBaseGenerator
{
    private const string Attribute            = $"{Global.Namespace}.Attributes.ResourceKeysOfAttribute";
    private const string CultureInfo          = "global::System.Globalization.CultureInfo";
    private const string ResourceProviderBase = $"global::{Global.Namespace}.Abstractions.ResourceProviderBase";

    private static readonly string[] Exceptions =
    {
        "resourceMan",
        "resourceCulture",
        ".ctor",
        "ResourceManager",
        "Culture"
    };

    protected override string AttributeName => Attribute;

    protected override void GenerateCode(SourceProductionContext context,
        ImmutableArray<(GeneratorAttributeSyntaxContext, TypeSyntax)> targets)
    {
        foreach (var (generateCtx, type) in targets)
        {
            var targetSymbol   = generateCtx.SemanticModel.GetSymbolInfo(type).Symbol as INamedTypeSymbol;
            var targetFullName = targetSymbol.GetFullyQualifiedName();
            var names          = targetSymbol!.MemberNames.Except(Exceptions).ToList();
            var fullName       = generateCtx.TargetSymbol.GetFullyQualifiedName();
            var nameSpace      = generateCtx.TargetSymbol.ContainingNamespace.GetFullyQualifiedName();
            var className      = $"__{targetSymbol.Name}Provider";
            var text = $$"""
                         // <auto-generated/> By Antelcat.Wpf.I18N.SourceGenerators

                         #nullable enable

                         namespace {{nameSpace.Replace("global::", "")}}{

                             partial class {{generateCtx.TargetSymbol.Name}}
                             {
                                 private class {{className}} : {{ResourceProviderBase}}
                                 {
                                     public override {{CultureInfo}}? Culture
                                     {
                                         get => {{targetFullName}}.Culture;
                                         set
                                         {
                                             if (value == null) return;
                                             if (Equals({{targetFullName}}.Culture?.EnglishName, value.EnglishName)) return;
                                             {{targetFullName}}.Culture = value;
                                             UpdateSource();
                                             OnChangeCompleted();
                                         }
                                     }
                                 
                                     private void UpdateSource()
                                     {
                         {{string.Concat(names.Select(x =>
                             $"\t\t\t\tOnPropertyChanged(nameof({x}));\n"
                         ))}}
                                     }
                                 
                         {{string.Concat(names.Select(x => $"\t\t\tpublic string {x} => {targetFullName}.{x};\n\n"))}}
                                 }
                         
                         {{string.Concat(names.Select(x => $"""
                                                            
                                                                    /// <summary>
                                                                    /// {x}
                                                                    /// </summary>
                                                                    public static string {x} = nameof({x});
                                                                                                            
                                                            """))
                         }}

                             }
                         }
                         """;
            context.AddSource($"{generateCtx.TargetSymbol.GetFullyQualifiedName().Replace("global::", "")}.g.cs", text);
        }
    }
}