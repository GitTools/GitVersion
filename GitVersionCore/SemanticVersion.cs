namespace GitVersion
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class SemanticVersion : IFormattable, IComparable<SemanticVersion>
    {
        static readonly Regex ParseSemVer = new Regex(
            @"[vV]?(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?",
            RegexOptions.Compiled);

        public int Major;
        public int Minor;
        public int Patch;
        public SemanticVersionPreReleaseTag PreReleaseTag;
        public SemanticVersionBuildMetaData BuildMetaData;

        public SemanticVersion()
        {
            PreReleaseTag = new SemanticVersionPreReleaseTag();
            BuildMetaData = new SemanticVersionBuildMetaData();
        }

        public bool Equals(SemanticVersion obj)
        {
            if (obj == null)
            {
                return false;
            }
            return Major == obj.Major &&
                   Minor == obj.Minor &&
                   Patch == obj.Patch &&
                   PreReleaseTag == obj.PreReleaseTag &&
                   BuildMetaData == obj.BuildMetaData;
        }

        public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
        {
            if (ReferenceEquals(v1, null))
            {
                return ReferenceEquals(v2, null);
            }
            return v1.Equals(v2);
        }

        public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
        {
            return !(v1 == v2);
        }

        public static bool operator >(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");
            return v1.CompareTo(v2) > 0;
        }

        public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");
            return v1.CompareTo(v2) >= 0;
        }

        public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator <(SemanticVersion v1, SemanticVersion v2)
        {
            if (v1 == null)
                throw new ArgumentNullException("v1");
            if (v2 == null)
                throw new ArgumentNullException("v2");

            return v1.CompareTo(v2) < 0;
        }

        public static SemanticVersion Parse(string version)
        {
            SemanticVersion semanticVersion;
            if (!TryParse(version, out semanticVersion))
                throw new ErrorException(string.Format("Failed to parse {0} into a Semantic Version", version));

            return semanticVersion;
        }

        public static bool TryParse(string version, out SemanticVersion semanticVersion)
        {
            var parsed = ParseSemVer.Match(version);

            if (!parsed.Success)
            {
                semanticVersion = null;
                return false;
            }

            var semanticVersionBuildMetaData = SemanticVersionBuildMetaData.Parse(parsed.Groups["BuildMetaData"].Value);
            var fourthPart = parsed.Groups["FourthPart"];
            if (fourthPart.Success && semanticVersionBuildMetaData.CommitsSinceTag == null)
            {
                semanticVersionBuildMetaData.CommitsSinceTag = int.Parse(fourthPart.Value);
            }

            semanticVersion = new SemanticVersion
            {
                Major = int.Parse(parsed.Groups["Major"].Value),
                Minor = parsed.Groups["Minor"].Success ? int.Parse(parsed.Groups["Minor"].Value) : 0,
                Patch = parsed.Groups["Patch"].Success ? int.Parse(parsed.Groups["Patch"].Value) : 0,
                PreReleaseTag = SemanticVersionPreReleaseTag.Parse(parsed.Groups["Tag"].Value),
                BuildMetaData = semanticVersionBuildMetaData
            };

            return true;
        }

        public int CompareTo(SemanticVersion value)
        {
            if (value == null)
            {
                return 1;
            }
            if (Major != value.Major)
            {
                if (Major > value.Major)
                {
                    return 1;
                }
                return -1;
            }
            if (Minor != value.Minor)
            {
                if (Minor > value.Minor)
                {
                    return 1;
                }
                return -1;
            }
            if (Patch != value.Patch)
            {
                if (Patch > value.Patch)
                {
                    return 1;
                }
                return -1;
            }
            if (PreReleaseTag != value.PreReleaseTag)
            {
                if (PreReleaseTag > value.PreReleaseTag)
                {
                    return 1;
                }
                return -1;
            }

            return -1;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// <para>s - Default SemVer [1.2.3-beta.4+5]</para>
        /// <para>f - Full SemVer [1.2.3-beta.4+5]</para>
        /// <para>i - Informational SemVer [1.2.3-beta.4+5.Branch.master.BranchType.Master.Sha.000000]</para>
        /// <para>j - Just the SemVer part [1.2.3]</para>
        /// <para>t - SemVer with the tag [1.2.3-beta.4]</para>
        /// <para>l - Legacy SemVer tag for systems which do not support SemVer 2.0 properly [1.2.3-beta4]</para>
        /// <para>lp - Legacy SemVer tag for systems which do not support SemVer 2.0 properly (padded) [1.2.3-beta0004]</para>
        /// </summary>
        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (string.IsNullOrEmpty(format))
                format = "s";

            if (formatProvider != null)
            {
                var formatter = formatProvider.GetFormat(GetType()) as ICustomFormatter;

                if (formatter != null)
                    return formatter.Format(format, this, formatProvider);
            }

            switch (format.ToLower())
            {
                case "j":
                    return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
                case "s":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag) : ToString("j");
                case "l":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag.ToString("l")) : ToString("j");
                case "lp":
                    return PreReleaseTag.HasTag() ? string.Format("{0}-{1}", ToString("j"), PreReleaseTag.ToString("lp")) : ToString("j");
                case "f":
                    {
                        var buildMetadata = BuildMetaData.ToString();

                        return !string.IsNullOrEmpty(buildMetadata) ? string.Format("{0}+{1}", ToString("s"), buildMetadata) : ToString("s");
                    }
                case "i":
                    {
                        var buildMetadata = BuildMetaData.ToString("f");

                        return !string.IsNullOrEmpty(buildMetadata) ? string.Format("{0}+{1}", ToString("s"), buildMetadata) : ToString("s");
                    }
                case "n":
                    {
                        if (BuildMetaData == null || String.IsNullOrWhiteSpace(BuildMetaData.Branch))
                        {
                            return ToString("j");
                        }

                        var branch = BuildMetaData.Branch;
                        if (branch.Equals("master", StringComparison.CurrentCultureIgnoreCase)
                            || branch.Equals("release", StringComparison.CurrentCultureIgnoreCase)
                            || branch.Equals("develop", StringComparison.CurrentCultureIgnoreCase)
                            || branch.Equals("support", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // "master", "release", "develop", and "support" branches will be versioned without commit count or extra branch metadata
                            return ToString("lp");
                        }

                        if (new[] { "-", "/" }.Any(suffix => 
                                branch.ToLower().StartsWith("hotfix" + suffix, StringComparison.CurrentCultureIgnoreCase) 
                                || branch.ToLower().StartsWith("support" + suffix, StringComparison.CurrentCultureIgnoreCase) 
                                || branch.ToLower().StartsWith("release" + suffix, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            // "hotfix-", "hotfix/", "support-", "support/", "release-", "release/" branches will have commit count appended to version
                            var buildMetaData = BuildMetaData.ToString();
                            return String.Format("{0}+{1}", ToString("lp"), String.IsNullOrWhiteSpace(buildMetaData) ? "0" : buildMetaData);
                        }

                        var specialVersion = SanitizeFeatureBranchNameForNugetSpecialVersion(branch);

                        if (String.IsNullOrWhiteSpace(specialVersion))
                        {
                            // unknown branch types will get default SemVer
                            return ToString("s");
                        }

                        // "feature-" and "feature/" branches will have branch metadata appended as special version information
                        return string.Format("{0}.{1}.{2}-{3}", Major, Minor, Patch, specialVersion);
                    }
                default:
                    throw new ArgumentException(string.Format("Unrecognised format '{0}'", format), "format");
            }
        }

        /// <summary>
        /// <para>Removes feature branch prefix and chops branch name at 20 characters to meet nuget special version requirements</para>
        /// </summary>
        /// <param name="branch">Current branch name</param>
        /// <returns>String.Empty for non-feature branches, otherwise nuget special version formatted string</returns>
        private static String SanitizeFeatureBranchNameForNugetSpecialVersion(String branch)
        {
            if (branch == null)
                throw new ArgumentNullException("branch");

            // Nuget Packages should not include slashes
            var branchWithoutSlashes = branch.Replace(@"/", "-");

            // Remove branch type prefix as it is not required in a version
            var branchWithoutTypePrefix = branchWithoutSlashes.RegexReplace("^feature-", "", RegexOptions.IgnoreCase);

            // https://nuget.codeplex.com/workitem/3426
            // https://github.com/Haacked/NuGet/blob/master/src/Core/Authoring/PackageBuilder.cs#L556-L559
            // nuget does not allow for 'version.SpecialVersion' to be over 20 characters
            var specialVersion = branchWithoutTypePrefix.Length > 20 ? branchWithoutTypePrefix.Substring(0, 20) : branchWithoutTypePrefix;

            return specialVersion;
        }
    }
}