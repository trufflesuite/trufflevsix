using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace TruffleVSIX.Helpers
{

    class TruffleENV
    {
        public delegate void InstalledCallback(bool installed);

        public static void CheckNPMInstalled(InstalledCallback callback)
        {
            bool isNPMInstalled = false;

            ProcessRunner runner = new ProcessRunner();

            runner.OnLine += (line) =>
            {
                if (isNPMInstalled == true) return;
                if (line == null) return;

                Regex regex = new Regex("^(\\d+\\.)?(\\d+\\.)?(\\*|\\d+)$");

                if (regex.IsMatch(line.Trim()))
                {
                    isNPMInstalled = true;
                }
            };

            runner.OnExit += () =>
            {
                callback(isNPMInstalled);
            };

            runner.Run("npm -v");
        }

        public static string ExpectedTruffleBinary(string projectPath)
        {
            return Path.Combine(new string[] { projectPath, "node_modules", ".bin", "truffle.cmd" });
        }

        public static bool CheckTruffleInstalled(string projectPath)
        {
            return File.Exists(ExpectedTruffleBinary(projectPath));
        }

        public static bool CheckTruffleProjectInitialized(string projectPath)
        {
            return CheckConfigExists(projectPath) && CheckContractsExist(projectPath) && CheckMigrationsExist(projectPath) && CheckTestsExist(projectPath);
        }

        public static bool CheckContractsExist(string projectPath)
        {
            return Directory.Exists(Path.Combine(new string[] { projectPath, "contracts" }));
        }

        public static bool CheckTestsExist(string projectPath)
        {
            return Directory.Exists(Path.Combine(new string[] { projectPath, "test" }));
        }

        public static bool CheckMigrationsExist(string projectPath)
        {
            return Directory.Exists(Path.Combine(new string[] { projectPath, "migrations" }));
        }

        public static bool CheckConfigExists(string projectPath)
        {
            return File.Exists(Path.Combine(new string[] { projectPath, "truffle.js" })) || File.Exists(Path.Combine(new string[] { projectPath, "truffle-config.js" }));
        }

        public static string ExpectedTestRPCBinary(string projectPath)
        {
            return Path.Combine(new string[] { projectPath, "node_modules", ".bin", "testrpc.cmd" });
        }

        public static bool CheckTestRPCInstalled(string projectPath)
        {
            return File.Exists(ExpectedTestRPCBinary(projectPath));
        }
    }
}
