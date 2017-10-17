using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using PlatformCompat.Analyzers.Deprecated;
using PlatformCompat.Analyzers.Exceptions;
using PlatformCompat.Analyzers.ModernSdk;
using PlatformCompat.Analyzers.Net461;

namespace PlatformCompat.Analyzers.Fixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReportIssueCodeFixProvider))]
    [Shared]
    public class ReportIssueCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                var result = new List<string>();
                result.Add(ExceptionAnalyzer.DiagnosticId);
                result.Add(ModernSdkAnalyzer.DiagnosticId);
                result.Add(Net461Analyzer.DiagnosticId);
                result.AddRange(DeprecatedAnalyzer.GetDescriptors().Select(d => d.Id));
                return result.ToImmutableArray();
            }
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var title = $"{diagnostic.Id}: {diagnostic.GetMessage()}";
            var action = new ReportIssueCodeAction(title);
            context.RegisterCodeFix(action, diagnostic);

            return Task.CompletedTask;
        }
        
        private sealed class ReportIssueCodeAction : CodeAction
        {
            private readonly string _issueTitle;

            public ReportIssueCodeAction(string issueTitle)
            {
                _issueTitle = issueTitle;
            }

            public override string Title => Resources.ReportAnIssueTitle;

            public override string EquivalenceKey => Title;

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var issueTitleEncoded = WebUtility.UrlEncode(_issueTitle);
                var url = $"https://github.com/dotnet/platform-compat/issues/new?title={issueTitleEncoded}";
                var result = new[] { new StartProcessCodeOperation(url) };
                return Task.FromResult<IEnumerable<CodeActionOperation>>(result);
            }
        }

        private sealed class StartProcessCodeOperation : CodeActionOperation
        {
            public StartProcessCodeOperation(string fileName)
            {
                FileName = fileName;
            }

            public string FileName { get; }

            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            {
                Process.Start(FileName);
            }
        }
    }
}
