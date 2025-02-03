//   Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using PathType = Microsoft.SqlServer.Management.Smo.PathType;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    public class PathWrapperTests : UnitTestBase
    {
        /// <summary>
        /// Sample strings taken from MSDN
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Windows_Combine_mimics_PathdotCombine()
        {
            Assert.That(PathWrapper.Combine(@"c:\temp", @"subdir\file.txt"), Is.EqualTo(@"c:\temp\subdir\file.txt"), "path1 absolute, path2 relative");
            Assert.That(PathWrapper.Combine(@"c:\temp", @"c:\temp.txt"), Is.EqualTo(@"c:\temp.txt"), "path1 root, path2 root");
            Assert.That(PathWrapper.Combine(@"c:\temp.txt", @"subdir\file.txt"),
                Is.EqualTo(@"c:\temp.txt\subdir\file.txt"), "path1 has an extension, path2 relative");
            Assert.That(PathWrapper.Combine(@"c:^*&)(_=@#'\^&#2.*(.txt", @"subdir\file.txt"),
                Is.EqualTo(@"c:^*&)(_=@#'\^&#2.*(.txt\subdir\file.txt"), "unsupported characters");
            Assert.That(PathWrapper.Combine(@"", @"subdir\file.txt"), Is.EqualTo(@"subdir\file.txt"), "path1 empty");
            Assert.That(PathWrapper.Combine(@"\\server\share\", "folder"), Is.EqualTo(@"\\server\share\folder"),
                "unc path");
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => PathWrapper.Combine(null, @"subdir\file.txt"), "path1 null", null).ParamName,
                Is.EqualTo("path1"), "path1 null");

        }

        /// <summary>
        /// Test our custom Path.Combine equivalent for Linux paths
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Linux_Combine_mimics_PathdotCombine()
        {
            Assert.That(PathWrapper.Combine(@"/temp", @"subdir/file.txt"), Is.EqualTo(@"/temp/subdir/file.txt"), "path1 absolute, path2 relative");
            Assert.That(PathWrapper.Combine(@"/temp", @"/temp.txt"), Is.EqualTo(@"/temp.txt"), "path1 root, path2 root");
            Assert.That(PathWrapper.Combine(@"/temp/", @"temp.txt"), Is.EqualTo(@"/temp/temp.txt"), "path1 ends in /, path2 relative");      
            Assert.That(PathWrapper.Combine(@"c:^*&)(_=@#'\^&#2.*(.txt", @"subdir/file.txt", PathType.Linux),
                Is.EqualTo(@"c:^*&)(_=@#'\^&#2.*(.txt/subdir/file.txt"), "unsupported characters");
            Assert.That(PathWrapper.Combine(@"", @"subdir/file.txt", PathType.Linux), Is.EqualTo(@"subdir/file.txt"), "path1 empty");
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => PathWrapper.Combine(null, @"subdir/file.txt", PathType.Linux), "path1 null", null).ParamName,
                Is.EqualTo("path1"), "path1 null");
            // we rely on Path.GetFileName in a few places on the UI. This shows we can use Path.GetFileName combined with PathWrapper.Combine safely
            Assert.That(Path.GetFileName(@"/temp/folder/filename"), Is.EqualTo("filename"),
                "Path.GetFileName on a Linux path");

        }

        /// <summary>
        /// From MSDN
        /// This code produces the following output:
        /// GetDirectoryName('C:\MyDir\MySubDir\myfile.ext') returns 'C:\MyDir\MySubDir'
        /// GetDirectoryName('C:\MyDir\MySubDir') returns 'C:\MyDir'
        /// GetDirectoryName('C:\MyDir\') returns 'C:\MyDir'
        /// GetDirectoryName('C:\MyDir') returns 'C:\'
        /// GetDirectoryName('C:\') returns ''
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Windows_GetDirectoryName_mimics_PathdotGetDirectoryName()
        {
            Assert.That(PathWrapper.GetDirectoryName(@"C:\MyDir\MySubDir\myfile.ext"), Is.EqualTo(@"C:\MyDir\MySubDir"), "filename with extension");
            Assert.That(PathWrapper.GetDirectoryName(@"C:\MyDir\MySubDir"), Is.EqualTo(@"C:\MyDir"), "no file extension");
            Assert.That(PathWrapper.GetDirectoryName(@"C:\MyDir\"), Is.EqualTo(@"C:\MyDir"), @"ending with \");
            Assert.That(PathWrapper.GetDirectoryName(@"C:\MyDir"), Is.EqualTo(@"C:\"), "folder in root");
            Assert.That(PathWrapper.GetDirectoryName(@"C:\"), Is.Null, "root");
            Assert.That(PathWrapper.GetDirectoryName(@"filename", PathType.Windows), Is.Empty, "no folder");
            Assert.That(PathWrapper.GetDirectoryName(@"\\server\share\folder"), Is.EqualTo(@"\\server\share"), "unc path");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Linux_GetDirectoryName_mimics_PathdotGetDirectoryName()
        {
            Assert.That(PathWrapper.GetDirectoryName(@"/MyDir/MySubDir/myfile.ext"), Is.EqualTo(@"/MyDir/MySubDir"), "filename with extension");
            Assert.That(PathWrapper.GetDirectoryName(@"/MyDir/MySubDir"), Is.EqualTo(@"/MyDir"), "no file extension");
            Assert.That(PathWrapper.GetDirectoryName(@"/MyDir/"), Is.EqualTo(@"/MyDir"), @"ending with /");
            Assert.That(PathWrapper.GetDirectoryName(@"/MyDir"), Is.EqualTo(@"/"), "folder in root");
            Assert.That(PathWrapper.GetDirectoryName(@"/"), Is.Null, "root");
            Assert.That(PathWrapper.GetDirectoryName(@"filename", PathType.Linux), Is.Empty, "no folder");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Linux_IsRooted_returns_true_for_path_starting_with_slash()
        {
            Assert.That(PathWrapper.IsRooted("/"), Is.True, "/");
            Assert.That(PathWrapper.IsRooted("/var/opt/src"), Is.True, "/var/opt/src");
            Assert.That(PathWrapper.IsRooted("foobar", PathType.Linux), Is.False, "foobar");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_PathType_is_Windows_IsRooted_returns_matches_Path_IsPathRooted()
        {
            Assert.That(PathWrapper.IsRooted(@"C:\"), Is.True, @"C:\");
            Assert.That(PathWrapper.IsRooted(@"\\server\share"), Is.True, @"\\server\share");
            Assert.That(PathWrapper.IsRooted("foobar", PathType.Windows), Is.False, "foobar");
            Assert.That(PathWrapper.IsRooted("foobar"), Is.False, "foobar");
        }
    }
}
