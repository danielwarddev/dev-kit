using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Dward.Godot.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GodotInputActionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor _unknownAction = new(
            "DWARDGD0007", "Use declared Godot input actions",
            "Input action '{0}' is not declared in project.godot or among the Godot built-ins; correct the argument to an existing action, or add the intended action and bindings under [input] in project.godot",
            "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Use an existing project action or an exact supported Godot built-in name. For a genuinely new action, define its events and deadzone in Project Settings > Input Map (the [input] section of project.godot).");
        private static readonly DiagnosticDescriptor _invalidConfiguration = new(
            "DWARDGD0008", "Supply one readable Godot input configuration to the C# project",
            "Godot input action validation cannot run: {0}; supply exactly one readable project.godot AdditionalFile for the actual Godot project and repair its [input] dictionaries",
            "Configuration", DiagnosticSeverity.Warning, isEnabledByDefault: true,
            description: "Keep exactly one <AdditionalFiles Include=\"project.godot\" /> entry pointing to the actual Godot project's settings; remove duplicate inclusions and correct missing or unreadable paths. Malformed [input] dictionaries can be repaired through Project Settings > Input Map.",
            customTags: [WellKnownDiagnosticTags.CompilationEnd]);
        private static readonly ImmutableHashSet<string> _singleActionMethods = ImmutableHashSet.Create(
            StringComparer.Ordinal, "IsActionPressed", "IsActionJustPressed", "IsActionJustReleased",
            "GetActionStrength", "GetActionRawStrength", "ActionPress", "ActionRelease");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_unknownAction, _invalidConfiguration];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var input = start.Compilation.GetTypeByMetadataName("Godot.Input");
                if (input?.ContainingAssembly.Name != "GodotSharp")
                {
                    return;
                }

                var configurations = start.Options.AdditionalFiles.Where(file =>
                        string.Equals(file.Path.Replace('\\', '/').Split('/').Last(), "project.godot", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (configurations.Length == 0)
                {
                    return;
                }

                var text = configurations.Length == 1 ? configurations[0].GetText(start.CancellationToken) : null;
                var actions = text is null ? null : GodotInputActionCatalog.ReadActions(text.ToString());
                if (actions is null)
                {
                    var reason = configurations.Length != 1
                        ? "multiple project.godot AdditionalFiles were supplied"
                        : text is null ? "project.godot could not be read" : "the [input] section is malformed";
                    start.RegisterCompilationEndAction(end =>
                        end.ReportDiagnostic(Diagnostic.Create(_invalidConfiguration, Location.None, reason)));
                    return;
                }

                start.RegisterOperationAction(operationContext =>
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    var method = invocation.TargetMethod;
                    if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, input))
                    {
                        return;
                    }

                    var actionCount = method.Name switch
                    {
                        "GetAxis" => 2,
                        "GetVector" => 4,
                        _ when _singleActionMethods.Contains(method.Name) => 1,
                        _ => 0
                    };
                    foreach (var argument in invocation.Arguments)
                    {
                        if (argument.IsImplicit || argument.Parameter is null || argument.Parameter.Ordinal >= actionCount)
                        {
                            continue;
                        }

                        IOperation value = argument.Value;
                        while (value is IConversionOperation conversion)
                        {
                            value = conversion.Operand;
                        }

                        if (value is IObjectCreationOperation creation &&
                            creation.Type?.ToDisplayString() == "Godot.StringName" && creation.Arguments.Length == 1)
                        {
                            value = creation.Arguments[0].Value;
                        }

                        if (value.ConstantValue is { HasValue: true, Value: string action } && !actions.Contains(action))
                        {
                            operationContext.ReportDiagnostic(Diagnostic.Create(
                                _unknownAction, value.Syntax.GetLocation(), action));
                        }
                    }
                }, OperationKind.Invocation);
            });
        }
    }

    public class GodotInputActionCatalog
    {
        private static readonly Regex _assignmentRegex = new(
            """^\s*(?<key>"(?:\\.|[^"\\])*"|[^=\s]+)\s*=\s*\{""",
            RegexOptions.CultureInvariant);
    
        // Godot 4.7 core/input/input_map.cpp, InputMap::get_builtins (base names, not .macos overrides).
        private static readonly ImmutableHashSet<string> _builtins = """
                                                                     ui_accept ui_select ui_cancel ui_close_dialog ui_focus_next ui_focus_prev
                                                                     ui_left ui_right ui_up ui_down ui_page_up ui_page_down ui_home ui_end
                                                                     ui_cut ui_copy ui_paste ui_focus_mode ui_undo ui_redo
                                                                     ui_text_completion_query ui_text_newline ui_text_newline_blank ui_text_newline_above
                                                                     ui_text_indent ui_text_dedent ui_text_backspace ui_text_backspace_word
                                                                     ui_text_backspace_all_to_left ui_text_delete ui_text_delete_word
                                                                     ui_text_delete_all_to_right ui_text_caret_left ui_text_caret_word_left
                                                                     ui_text_caret_right ui_text_caret_word_right ui_text_caret_up ui_text_caret_down
                                                                     ui_text_caret_line_start ui_text_caret_line_end ui_text_caret_page_up
                                                                     ui_text_caret_page_down ui_text_caret_document_start ui_text_caret_document_end
                                                                     ui_text_caret_add_below ui_text_caret_add_above ui_text_scroll_up ui_text_scroll_down
                                                                     ui_text_select_all ui_text_select_word_under_caret
                                                                     ui_text_add_selection_for_next_occurrence ui_text_skip_selection_for_next_occurrence
                                                                     ui_text_clear_carets_and_selection ui_text_toggle_insert_mode ui_text_submit
                                                                     ui_graph_duplicate ui_graph_delete ui_graph_follow_left ui_graph_follow_right
                                                                     ui_filedialog_delete ui_filedialog_up_one_level ui_filedialog_refresh
                                                                     ui_filedialog_show_hidden ui_filedialog_find ui_filedialog_focus_path
                                                                     ui_swap_input_direction ui_unicode_start ui_colorpicker_delete_preset
                                                                     ui_accessibility_drag_and_drop ui_menu ui_text_completion_accept ui_text_completion_replace
                                                                     """.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToImmutableHashSet(StringComparer.Ordinal);

        public static ImmutableHashSet<string>? ReadActions(string text)
        {
            var actions = _builtins.ToBuilder();
            var inputSection = false;
            var delimiters = new Stack<char>();
            var quoted = false;
            var escaped = false;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (delimiters.Count == 0 && !quoted)
                {
                    if (line.StartsWith("[", StringComparison.Ordinal))
                    {
                        inputSection = line == "[input]";
                        continue;
                    }

                    if (!inputSection || line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var assignment = _assignmentRegex.Match(line);
                    if (!assignment.Success)
                    {
                        return null;
                    }

                    var key = assignment.Groups["key"].Value;
                    if (key.StartsWith("\"", StringComparison.Ordinal))
                    {
                        var token = SyntaxFactory.ParseToken(key);
                        if (!token.IsKind(SyntaxKind.StringLiteralToken) || token.ContainsDiagnostics)
                        {
                            return null;
                        }

                        key = token.ValueText;
                    }

                    actions.Add(key);
                    line = line.Substring(assignment.Length - 1);
                }

                foreach (var character in line)
                {
                    if (quoted)
                    {
                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (character == '\\')
                        {
                            escaped = true;
                        }
                        else if (character == '"')
                        {
                            quoted = false;
                        }

                        continue;
                    }

                    if (character == ';')
                    {
                        break;
                    }

                    if (character == '"')
                    {
                        quoted = true;
                    }
                    else if (character is '{' or '[' or '(')
                    {
                        delimiters.Push(character);
                    }
                    else if (character is '}' or ']' or ')')
                    {
                        if (delimiters.Count == 0)
                        {
                            return null;
                        }

                        var opening = delimiters.Pop();
                        if ((opening, character) is not ('{', '}') and not ('[', ']') and not ('(', ')'))
                        {
                            return null;
                        }
                    }
                }
            }

            return delimiters.Count == 0 && !quoted ? actions.ToImmutable() : null;
        }
    }
}