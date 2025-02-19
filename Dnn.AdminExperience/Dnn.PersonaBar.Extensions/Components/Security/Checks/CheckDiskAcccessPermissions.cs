﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace Dnn.PersonaBar.Security.Components.Checks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.AccessControl;
    using System.Security.Principal;

    using DotNetNuke.Common;

    public class CheckDiskAcccessPermissions : IAuditCheck
    {
        private const char Yes = 'Y';
        private const char No = 'N';

        /// <inheritdoc/>
        public string Id => "CheckDiskAccess";

        /// <inheritdoc/>
        public bool LazyLoad => false;

        /// <inheritdoc/>
        public CheckResult Execute()
        {
            var result = new CheckResult(SeverityEnum.Unverified, this.Id);
            IList<string> accessErrors = new List<string>();
            try
            {
                accessErrors = CheckAccessToDrives();
            }
            catch (IOException)
            {
                // e.g., a disk error or a drive was not ready
            }
            catch (UnauthorizedAccessException)
            {
                // The caller does not have the required permission.
            }
            catch (System.Security.SecurityException)
            {
                // Some security exception
            }

            if (accessErrors.Count == 0)
            {
                result.Severity = SeverityEnum.Pass;
            }
            else
            {
                result.Severity = SeverityEnum.Warning;
                result.Notes = accessErrors;
            }

            return result;
        }

        private static IList<string> CheckAccessToDrives()
        {
            var errors = new List<string>();
            var dir = new DirectoryInfo(Globals.ApplicationMapPath);

            while (dir.Parent != null)
            {
                try
                {
                    dir = dir.Parent;
                    var permissions = CheckPermissionOnDir(dir);
                    var isRoot = dir.Name == dir.Root.Name;
                    if (permissions.Create == Yes || permissions.Write == Yes || permissions.Delete == Yes || (!isRoot && permissions.Read == Yes))
                    {
                        errors.Add(GetPermissionText(dir, permissions, isRoot));
                    }
                }
                catch (IOException)
                {
                    // e.g., a disk error or a drive was not ready
                }
                catch (UnauthorizedAccessException)
                {
                    // The caller does not have the required permission.
                }
            }

            var drives = DriveInfo.GetDrives();
            var checkedDrives = new List<string>();
            foreach (var drive in drives.Where(d => d.IsReady && d.RootDirectory.Name != dir.Root.Name))
            {
                try
                {
                    var driveType = drive.DriveType;
                    if (driveType == DriveType.Fixed || driveType == DriveType.Network)
                    {
                        var dir2 = drive.RootDirectory;
                        var key = dir2.FullName.ToLowerInvariant();
                        if (checkedDrives.Contains(key))
                        {
                            continue;
                        }

                        checkedDrives.Add(key);

                        var permissions = CheckPermissionOnDir(dir2);
                        if (permissions.AnyYes)
                        {
                            errors.Add(GetPermissionText(dir2, permissions));
                        }
                    }
                }
                catch (IOException)
                {
                    // e.g., a disk error or a drive was not ready
                }
                catch (UnauthorizedAccessException)
                {
                    // The caller does not have the required permission.
                }
            }

            return errors;
        }

        private static string GetPermissionText(DirectoryInfo dir, Permissions permissions, bool ignoreRead = false)
        {
            var message = ignoreRead
                ? @"{0} - Write:{2}, Create:{3}, Delete:{4}"
                : @"{0} - Read:{1}, Write:{2}, Create:{3}, Delete:{4}";
            return string.Format(
                @"{0} - Read:{1}, Write:{2}, Create:{3}, Delete:{4}",
                dir.FullName, permissions.Read, permissions.Write, permissions.Create, permissions.Delete);
        }

        private static Permissions CheckPermissionOnDir(DirectoryInfo dir)
        {
            var permissions = new Permissions(No);
            var disSecurity = dir.GetAccessControl(AccessControlSections.Access);
            var accessRules = disSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var poolIdentity = WindowsIdentity.GetCurrent();
            if (poolIdentity.User != null && poolIdentity.Groups != null)
            {
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if (poolIdentity.User.Value == rule.IdentityReference.Value || poolIdentity.Groups.Contains(rule.IdentityReference))
                    {
                        if ((rule.FileSystemRights & (FileSystemRights.CreateDirectories | FileSystemRights.CreateFiles)) != 0)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                permissions.Create = Yes;
                            }
                            else
                            {
                                permissions.SetThenLockCreate(No);
                            }
                        }

                        if ((rule.FileSystemRights & FileSystemRights.Write) != 0)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                permissions.Write = Yes;
                            }
                            else
                            {
                                permissions.SetThenLockWrite(No);
                            }
                        }

                        if ((rule.FileSystemRights & (FileSystemRights.Read | FileSystemRights.ReadData)) != 0)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                permissions.Read = Yes;
                            }
                            else
                            {
                                permissions.SetThenLockRead(No);
                            }
                        }

                        if ((rule.FileSystemRights & (FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles)) != 0)
                        {
                            if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                permissions.Delete = Yes;
                            }
                            else
                            {
                                permissions.SetThenLockDelete(No);
                            }
                        }
                    }
                }
            }

            return permissions;
        }

        private class Permissions
        {
            private char create;

            private bool createLocked;
            private char delete;
            private bool deleteLocked;
            private char read;
            private bool readLocked;

            private char write;
            private bool writeLocked;

            public Permissions(char initial)
            {
                this.create = this.write = this.read = this.delete = initial;
            }

            public bool AnyYes
            {
                get { return this.Create == Yes || this.Write == Yes || this.Read == Yes || this.Delete == Yes; }
            }

            public char Create
            {
                get
                {
                    return this.create;
                }

                set
                {
                    if (!this.createLocked)
                    {
                        this.create = value;
                    }
                }
            }

            public char Write
            {
                get
                {
                    return this.write;
                }

                set
                {
                    if (!this.writeLocked)
                    {
                        this.write = value;
                    }
                }
            }

            public char Read
            {
                get
                {
                    return this.read;
                }

                set
                {
                    if (!this.readLocked)
                    {
                        this.read = value;
                    }
                }
            }

            public char Delete
            {
                get
                {
                    return this.delete;
                }

                set
                {
                    if (!this.deleteLocked)
                    {
                        this.delete = value;
                    }
                }
            }

            public void SetThenLockCreate(char value)
            {
                if (!this.createLocked)
                {
                    this.createLocked = true;
                    this.create = value;
                }
            }

            public void SetThenLockWrite(char value)
            {
                if (!this.writeLocked)
                {
                    this.writeLocked = true;
                    this.write = value;
                }
            }

            public void SetThenLockRead(char value)
            {
                if (!this.readLocked)
                {
                    this.readLocked = true;
                    this.read = value;
                }
            }

            public void SetThenLockDelete(char value)
            {
                if (!this.deleteLocked)
                {
                    this.deleteLocked = true;
                    this.delete = value;
                }
            }
        }
    }
}
