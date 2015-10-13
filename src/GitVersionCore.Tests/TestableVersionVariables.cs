using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitVersionCore.Tests
{
    using GitVersion;
    class TestableVersionVariables : VersionVariables
    {
        public TestableVersionVariables(string major = null, string minor = null, string patch = null, string buildMetaData = null, string buildMetaDataPadded = null, string fullBuildMetaData = null, string branchName = null, string sha = null, string majorMinorPatch = null, string semVer = null, string legacySemVer = null, string legacySemVerPadded = null, string fullSemVer = null, string assemblySemVer = null, string preReleaseTag = null, string preReleaseTagWithDash = null, string informationalVersion = null, string commitDate = null) : base(major, minor, patch, buildMetaData, buildMetaDataPadded, fullBuildMetaData, branchName, sha, majorMinorPatch, semVer, legacySemVer, legacySemVerPadded, fullSemVer, assemblySemVer, preReleaseTag, preReleaseTagWithDash, informationalVersion, commitDate)
        {
        }
    }
}
