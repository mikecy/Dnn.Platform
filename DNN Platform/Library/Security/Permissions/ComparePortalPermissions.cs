﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information
namespace DotNetNuke.Security.Permissions
{
    using System.Collections;

    /// -----------------------------------------------------------------------------
    /// Project  : DotNetNuke
    /// Namespace: DotNetNuke.Security.Permissions
    /// Class    : ComparePortalPermissions
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// ComparePortalPermissions provides the a custom IComparer implementation for
    /// PortalPermissionInfo objects.
    /// </summary>
    /// -----------------------------------------------------------------------------
    internal class ComparePortalPermissions : IComparer
    {
        /// <inheritdoc/>
        public int Compare(object x, object y)
        {
            return ((PortalPermissionInfo)x).PortalPermissionID.CompareTo(((PortalPermissionInfo)y).PortalPermissionID);
        }
    }
}
