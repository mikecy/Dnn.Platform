﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace DotNetNuke.Services.DependencyInjection
{
    using System;
    using System.Collections;
    using System.Web;

    using DotNetNuke.Common.Extensions;
    using Microsoft.Extensions.DependencyInjection;

    public interface IScopeAccessor
    {
        IServiceScope GetScope();
    }

    public class ScopeAccessor : IScopeAccessor
    {
        private static Func<IDictionary> fallbackGetContextItems = () => HttpContext.Current?.Items;

        private Func<IDictionary> getContextItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeAccessor"/> class.
        /// </summary>
        public ScopeAccessor()
        {
            this.getContextItems = fallbackGetContextItems;
        }

        /// <inheritdoc/>
        public IServiceScope GetScope()
        {
            return HttpContextDependencyInjectionExtensions.GetScope(this.getContextItems());
        }

        internal void SetContextItemsFunc(Func<IDictionary> getContextItems)
        {
            this.getContextItems = getContextItems ?? fallbackGetContextItems;
        }
    }
}
