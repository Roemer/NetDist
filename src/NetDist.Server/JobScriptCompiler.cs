using Microsoft.CSharp;
using NetDist.Server.XDomainObjects;
using System;
using System.CodeDom.Compiler;
using System.IO;

namespace NetDist.Server
{
    public static class JobScriptCompiler
    {
        private static string BuildScriptAssemblyName(JobScriptFile jobScriptFile)
        {
            return String.Format("_job_{0}.dll", jobScriptFile.Hash);
        }

        public static JobScriptCompileResult Compile(JobScriptFile jobScriptFile, string currentPackageFolder)
        {
            var jobScriptAssemblyName = BuildScriptAssemblyName(jobScriptFile);
            var fullAssemblyPath = Path.Combine(currentPackageFolder, jobScriptAssemblyName);
            if (File.Exists(fullAssemblyPath))
            {
                // Skipped since the same file was already compiled
                return new JobScriptCompileResult(fullAssemblyPath);
            }

            // Prepare compiler
            var codeProvider = new CSharpCodeProvider();
            var options = new CompilerParameters
            {
                GenerateInMemory = false,
                OutputAssembly = fullAssemblyPath,
                IncludeDebugInformation = true,
                CompilerOptions = String.Format("/lib:\"{0}\",\"Libs\"", currentPackageFolder)
            };
            // Add libraries
            foreach (var library in jobScriptFile.CompilerLibraries)
            {
                options.ReferencedAssemblies.Add(library);
            }
            // Compile it
            var compilerResults = codeProvider.CompileAssemblyFromSource(options, jobScriptFile.JobScript);

            // Build and return the result object
            var result = new JobScriptCompileResult(compilerResults);
            return result;
        }
    }
}
