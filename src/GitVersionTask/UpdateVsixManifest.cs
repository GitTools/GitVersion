namespace GitVersionTask
{
    using System;
    using System.ComponentModel;
    using System.Xml.Linq;
    using System.Linq;
    using GitVersion;
    using Microsoft.Build.Framework;

    public class UpdateVsixManifest : GitVersionTaskBase
    {
        TaskLogger logger;

        public UpdateVsixManifest()
        {
            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogDebug, this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string VsixManifest { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        public bool NoFetch { get; set; }

        public override bool Execute()
        {
            try
            {
                VersionVariables variables;
                if (ExecuteCore.TryGetVersion(SolutionDirectory, out variables, NoFetch, new Authentication()))
                {
                    UpdateManifestFile(variables);
                }

                return true;
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }

        private void UpdateManifestFile(VersionVariables variables)
        {
            const string ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";

            var version = variables.AssemblySemFileVer;
            var doc = XDocument.Load(VsixManifest);
            var identity = doc
                .Root
                .Descendants(XName.Get("Identity", ns))
                .Single();

            identity.Attribute("Version").Value = version;

            doc.Save(VsixManifest);

            Log.LogMessage($"Updated {VsixManifest} version to {version}");
        }
    }
}