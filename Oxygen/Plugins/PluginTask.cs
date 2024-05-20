using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;

namespace Oxygen
{
    internal class PluginTask
    {
        /// <summary>
        /// Need to be careful we conceptually only have readonly
        /// access to the plugin on this thread.
        /// </summary>
        private readonly Plugin plugin;
        private readonly string workspace;
        private string artefactFile = string.Empty;
        /// <summary>
        /// Set volatile to prevent optimising it away.
        /// </summary>
        private volatile bool running = false;
        private readonly bool startedManually;
        private readonly string startedBy;
        private readonly long userId;

        public bool Running => running;
        public string Workspace => workspace;
        public string Artefacts => artefactFile;
        public Plugin Plugin => plugin;
        public bool StartedManually => startedManually;
        public string StartedBy => startedBy;
        public long UserId => userId;

        public PluginTask(Plugin plugin, bool startedManually, string startedBy, long userId)
        {
            this.plugin = plugin;
            this.startedManually = startedManually;
            this.startedBy = startedBy;
            this.userId = userId;

            // Create working area
            this.workspace = CreateWorkingArea();

            this.running = true;
        }

        private static string CreateWorkingArea()
        {
            if (!Directory.Exists("Workspaces"))
            {
                Directory.CreateDirectory("Workspaces");
            }

            string dir = string.Empty;

            Random rnd = new Random();
            for (int i = 0; i < 20; i++)
            {
                dir += "A" + (rnd.Next() % 26);
            }

            string workspace = Path.Combine("Workspaces", dir);
            Directory.CreateDirectory(workspace);

            return workspace;
        }

        private void Clean()
        {
            if (!string.IsNullOrEmpty(workspace))
            {
                try
                {
                    Directory.Delete(workspace, true);
                }
                catch (IOException)
                {
                    Logger.Instance.Log("Failed to delete workspace '{0}'", workspace);
                }
            }
        }

        private void InstallPackage()
        {
            if (!string.IsNullOrEmpty(plugin.Package))
            {
                string srcFolder = Path.Combine("Plugins", plugin.Package);

                if (Directory.Exists(srcFolder))
                {
                    string[] files = Directory.GetFiles(srcFolder);
                    foreach (string file in files)
                    {
                        File.Copy(file, Path.Combine(workspace, Path.GetFileName(file)));
                    }
                }
            }
        }

        public void Start()
        {
            // Install package
            InstallPackage();

            foreach (var action in plugin.Actions)
            {
                if (!string.IsNullOrEmpty(action.Run))
                {
                    using (Process process = new Process())
                    {
                        string file = Path.Combine(Directory.GetCurrentDirectory(), workspace, action.Run);

                        if (string.IsNullOrEmpty(action.Args))
                        {
                            process.StartInfo = new ProcessStartInfo(file);
                        }
                        else
                        {
                            process.StartInfo = new ProcessStartInfo(file, action.Args);
                        }

                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.WorkingDirectory = workspace;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        try
                        {
                            process.Start();
                            process.WaitForExit();

                            string output = process.StandardOutput.ReadToEnd();
                            if (!string.IsNullOrEmpty(output))
                            {
                                Logger.Instance.Log(output);
                            }

                            string err = process.StandardError.ReadToEnd();
                            if (!string.IsNullOrEmpty(err))
                            {
                                Logger.Instance.Log(err);
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            Logger.Instance.Log(ex.Message);
                        }
                        catch (Win32Exception ex)
                        {
                            Logger.Instance.Log(ex.Message);
                        }
                    }
                }
            }

            // Gather artefacts
            if (plugin.Artefacts.Count > 0)
            {
                if (!Directory.Exists("Artefacts"))
                {
                    Directory.CreateDirectory("Artefacts");
                }

                this.artefactFile =  plugin.Name + "_" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".zip";
                using (ZipArchive archive = new ZipArchive(File.Create(Path.Combine("Artefacts", artefactFile)), ZipArchiveMode.Create))
                {
                    foreach (var artefact in plugin.Artefacts)
                    {
                        string srcfile = Path.Combine(workspace, artefact);
                        if (File.Exists(srcfile))
                        {
                            archive.CreateEntryFromFile(srcfile, artefact);
                        }
                    }
                }
            }

            Clean();
            this.running = false;
        }

        public void Stopped()
        {
            Clean();
            this.running = false;
        }
    }
}
