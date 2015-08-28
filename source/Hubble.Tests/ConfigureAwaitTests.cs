using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Hubble.Tests.Helpers;
using Hubble.Tests.Verifiers;

namespace Hubble.Tests
{
    [TestClass]
    public class ConfigureAwaitTests : CodeFixVerifier
    {
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestConfigureAwaitAnalyzer()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task DoSomething()
            {
                await Task.Delay(1);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "ConfigureAwaitAnalyzer",
                Message = "await keyword used without first calling ConfigureAwait",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 15, 17)
                        }
            };

            this.VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task DoSomething()
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
        }
    }";
            this.VerifyCSharpFix(test, fixtest, codeFixIndex: 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ConfigureAwaitCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ConfigureAwaitAnalyzer();
        }
    }
}
