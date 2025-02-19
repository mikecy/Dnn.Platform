﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information
namespace DotNetNuke.Services.Exceptions
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;

    using DotNetNuke.Common;
    using DotNetNuke.Common.Utilities;
    using DotNetNuke.Entities.Host;
    using DotNetNuke.Entities.Modules;
    using DotNetNuke.Entities.Portals;
    using DotNetNuke.Instrumentation;
    using DotNetNuke.Services.Localization;
    using DotNetNuke.Services.Log.EventLog;
    using DotNetNuke.UI.Modules;
    using Microsoft.VisualBasic.CompilerServices;

    /// <summary>
    /// Exceptions class provides operation to log most of the exceptions occured in system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Logging is an import part of a good architecture. A good practice is to provide enough logging to accurately be able to identify which
    /// code caused an exception. Some exceptions are expected and you can display information to the user to help him figure out what happened.
    /// It is however bad practice to catch any and all exceptions (general exception) and display this to users. In such situations it is best
    /// to provide the user with a general message and only log the details in a way that the details are either just visible to administrators
    /// (Event Log) or only to SuperUsers (hosts) in the server logs.
    /// </para>
    /// <para>
    /// Currently we implement a lot of custom exception to use in different levels:
    /// <list type="bullet">
    /// <item><see cref="ModuleLoadException"/></item>
    /// <item><see cref="ObjectHydrationException"/></item>
    /// <item><see cref="PageLoadException"/></item>
    /// <item><see cref="SchedulerException"/></item>
    /// <item><see cref="SearchException"/></item>
    /// <item><see cref="SecurityException"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    [StandardModule]
    public sealed class Exceptions
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(Exceptions));

        /// <summary>
        /// Gets the exception info.
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <returns>Exception info.</returns>
        public static ExceptionInfo GetExceptionInfo(Exception e)
        {
            var objExceptionInfo = new ExceptionInfo(e);

            while (e.InnerException != null)
            {
                e = e.InnerException;
            }

            var st = new StackTrace(e, true);
            StackFrame sf = st.GetFrame(0);
            if (sf != null)
            {
                try
                {
                    // Get the corresponding method for that stack frame.
                    MemberInfo mi = sf.GetMethod();

                    // Get the namespace where that method is defined.
                    string res = mi.DeclaringType.Namespace + ".";

                    // Append the type name.
                    res += mi.DeclaringType.Name + ".";

                    // Append the name of the method.
                    res += mi.Name;
                    objExceptionInfo.Method = res;
                }
                catch (Exception exc)
                {
                    Logger.Error(exc);

                    objExceptionInfo.Method = "N/A - Reflection Permission required";
                }

                if (!string.IsNullOrEmpty(sf.GetFileName()))
                {
                    objExceptionInfo.FileName = sf.GetFileName();
                    objExceptionInfo.FileColumnNumber = sf.GetFileColumnNumber();
                    objExceptionInfo.FileLineNumber = sf.GetFileLineNumber();
                }
            }

            return objExceptionInfo;
        }

        public static void ProcessHttpException()
        {
            var notFoundErrorString = Localization.GetString("ResourceNotFound", Localization.SharedResourceFile);
            var exc = new HttpException(404, notFoundErrorString);
            ProcessHttpException(exc, HttpContext.Current.Request.RawUrl);
        }

        public static void ProcessHttpException(string url)
        {
            var notFoundErrorString = Localization.GetString("ResourceNotFound", Localization.SharedResourceFile);
            var exc = new HttpException(404, notFoundErrorString);
            ProcessHttpException(exc, url);
        }

        public static void ProcessHttpException(HttpException exc)
        {
            ProcessHttpException(exc, HttpContext.Current.Request.RawUrl);
        }

        public static void ProcessHttpException(HttpRequest request)
        {
            var notFoundErrorString = Localization.GetString("ResourceNotFound", Localization.SharedResourceFile);
            var exc = new HttpException(404, notFoundErrorString);
            ProcessHttpException(exc, request.RawUrl);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="objPortalModuleBase">The portal module base.</param>
        /// <param name="exc">The exc.</param>
        public static void ProcessModuleLoadException(PortalModuleBase objPortalModuleBase, Exception exc)
        {
            ProcessModuleLoadException((Control)objPortalModuleBase, exc);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="objPortalModuleBase">The portal module base.</param>
        /// <param name="exc">The exc.</param>
        /// <param name="displayErrorMessage">if set to <c>true</c> display error message.</param>
        public static void ProcessModuleLoadException(PortalModuleBase objPortalModuleBase, Exception exc, bool displayErrorMessage)
        {
            ProcessModuleLoadException((Control)objPortalModuleBase, exc, displayErrorMessage);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="friendlyMessage">The friendly message.</param>
        /// <param name="objPortalModuleBase">The obj portal module base.</param>
        /// <param name="exc">The exc.</param>
        /// <param name="displayErrorMessage">if set to <c>true</c> display error message.</param>
        public static void ProcessModuleLoadException(string friendlyMessage, PortalModuleBase objPortalModuleBase, Exception exc, bool displayErrorMessage)
        {
            ProcessModuleLoadException(friendlyMessage, (Control)objPortalModuleBase, exc, displayErrorMessage);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="ctrl">The CTRL.</param>
        /// <param name="exc">The exc.</param>
        public static void ProcessModuleLoadException(Control ctrl, Exception exc)
        {
            // Exit Early if ThreadAbort Exception
            if (ThreadAbortCheck(exc))
            {
                return;
            }

            ProcessModuleLoadException(ctrl, exc, true);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="ctrl">The CTRL.</param>
        /// <param name="exc">The exc.</param>
        /// <param name="displayErrorMessage">if set to <c>true</c> [display error message].</param>
        public static void ProcessModuleLoadException(Control ctrl, Exception exc, bool displayErrorMessage)
        {
            // Exit Early if ThreadAbort Exception
            if (ThreadAbortCheck(exc))
            {
                return;
            }

            string friendlyMessage = Localization.GetString("ErrorOccurred");
            var ctrlModule = ctrl as IModuleControl;
            if (ctrlModule == null)
            {
                // Regular Control
                friendlyMessage = Localization.GetString("ErrorOccurred");
            }
            else
            {
                // IModuleControl
                string moduleTitle = Null.NullString;
                if (ctrlModule != null && ctrlModule.ModuleContext.Configuration != null)
                {
                    moduleTitle = ctrlModule.ModuleContext.Configuration.ModuleTitle;
                }

                friendlyMessage = string.Format(Localization.GetString("ModuleUnavailable"), moduleTitle);
            }

            ProcessModuleLoadException(friendlyMessage, ctrl, exc, displayErrorMessage);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="friendlyMessage">The friendly message.</param>
        /// <param name="ctrl">The CTRL.</param>
        /// <param name="exc">The exc.</param>
        public static void ProcessModuleLoadException(string friendlyMessage, Control ctrl, Exception exc)
        {
            // Exit Early if ThreadAbort Exception
            if (ThreadAbortCheck(exc))
            {
                return;
            }

            ProcessModuleLoadException(friendlyMessage, ctrl, exc, true);
        }

        /// <summary>
        /// Processes the module load exception.
        /// </summary>
        /// <param name="friendlyMessage">The friendly message.</param>
        /// <param name="ctrl">The CTRL.</param>
        /// <param name="exc">The exc.</param>
        /// <param name="displayErrorMessage">if set to <c>true</c> display error message.</param>
        public static void ProcessModuleLoadException(string friendlyMessage, Control ctrl, Exception exc, bool displayErrorMessage)
        {
            // Exit Early if ThreadAbort Exception
            if (ThreadAbortCheck(exc))
            {
                return;
            }

            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            try
            {
                if (!Host.UseCustomErrorMessages)
                {
                    throw new ModuleLoadException(friendlyMessage, exc);
                }
                else
                {
                    var ctrlModule = ctrl as IModuleControl;
                    ModuleLoadException lex = null;
                    var friendlyMessageOverride = exc.Message;

                    if (Host.DebugMode && exc is HttpParseException)
                    {
                        var httpParseError = (HttpParseException)exc;
                        friendlyMessageOverride = string.Concat(exc.Message, " in ", httpParseError.VirtualPath, ":line ", httpParseError.Line);
                    }

                    if (ctrlModule == null)
                    {
                        lex = new ModuleLoadException(friendlyMessageOverride, exc);
                    }
                    else
                    {
                        lex = new ModuleLoadException(friendlyMessageOverride, exc, ctrlModule.ModuleContext.Configuration);
                    }

                    // publish the exception
                    var objExceptionLog = new ExceptionLogController();
                    objExceptionLog.AddLog(lex);

                    // Some modules may want to suppress an error message
                    // and just log the exception.
                    if (displayErrorMessage)
                    {
                        PlaceHolder errorPlaceholder = null;
                        if (ctrl.Parent != null)
                        {
                            errorPlaceholder = (PlaceHolder)ctrl.Parent.FindControl("MessagePlaceHolder");
                        }

                        if (errorPlaceholder != null)
                        {
                            // hide the module
                            ctrl.Visible = false;
                            errorPlaceholder.Visible = true;
                            errorPlaceholder.Controls.Add(new ErrorContainer(portalSettings, friendlyMessage, lex).Container);
                        }
                        else
                        {
                            // there's no ErrorPlaceholder, add it to the module's control collection
                            ctrl.Controls.Add(new ErrorContainer(portalSettings, friendlyMessage, lex).Container);
                        }
                    }
                }
            }
            catch (Exception exc2)
            {
                Logger.Fatal(exc2);
                ProcessPageLoadException(exc2);
            }

            Logger.ErrorFormat("FriendlyMessage=\"{0}\" ctrl=\"{1}\" exc=\"{2}\"", friendlyMessage, ctrl, exc);
        }

        /// <summary>
        /// Processes the page load exception.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void ProcessPageLoadException(Exception exc)
        {
            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            string appURL = Globals.ApplicationURL();
            if (appURL.IndexOf("?") == Null.NullInteger)
            {
                appURL += "?def=ErrorMessage";
            }
            else
            {
                appURL += "&def=ErrorMessage";
            }

            ProcessPageLoadException(exc, appURL);
        }

        /// <summary>
        /// Processes the page load exception.
        /// </summary>
        /// <param name="exc">The exc.</param>
        /// <param name="url">The URL.</param>
        public static void ProcessPageLoadException(Exception exc, string url)
        {
            Logger.Error(url, exc);
            if (ThreadAbortCheck(exc))
            {
                return;
            }

            PortalSettings portalSettings = PortalController.Instance.GetCurrentPortalSettings();
            if (!Host.UseCustomErrorMessages)
            {
                throw new PageLoadException(exc == null ? string.Empty : exc.Message, exc);
            }
            else
            {
                var lex = new PageLoadException(exc == null ? string.Empty : exc.Message, exc);

                // publish the exception
                var objExceptionLog = new ExceptionLogController();
                objExceptionLog.AddLog(lex);
                if (!string.IsNullOrEmpty(url))
                {
                    // redirect
                    if (url.IndexOf("error=terminate") != -1)
                    {
                        HttpContext.Current.Response.Clear();
                        HttpContext.Current.Server.Transfer("~/ErrorPage.aspx");
                    }
                    else
                    {
                        HttpContext.Current.Response.Redirect(url, true);
                    }
                }
            }
        }

        /// <summary>
        /// Logs the ModuleLoadException.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogException(ModuleLoadException exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.MODULE_LOAD_EXCEPTION);
        }

        /// <summary>
        /// Logs the PageLoadException.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogException(PageLoadException exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.PAGE_LOAD_EXCEPTION);
        }

        /// <summary>
        /// Logs the SchedulerException.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogException(SchedulerException exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.SCHEDULER_EXCEPTION);
        }

        /// <summary>
        /// Logs the SecurityException.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogException(SecurityException exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.SECURITY_EXCEPTION);
        }

        /// <summary>
        /// Logs all the basic exception.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogException(Exception exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.GENERAL_EXCEPTION);
        }

        /// <summary>
        /// Processes the scheduler exception.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void ProcessSchedulerException(Exception exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.SCHEDULER_EXCEPTION);
        }

        /// <summary>
        /// Logs the search exception.
        /// </summary>
        /// <param name="exc">The exc.</param>
        public static void LogSearchException(SearchException exc)
        {
            Logger.Error(exc);
            var objExceptionLog = new ExceptionLogController();
            objExceptionLog.AddLog(exc, ExceptionLogController.ExceptionLogType.SEARCH_INDEXER_EXCEPTION);
        }

        /// <summary>
        /// Threads the abort check if the exception is a ThreadAbortCheck.
        /// </summary>
        /// <param name="exc">The exc.</param>
        /// <returns></returns>
        private static bool ThreadAbortCheck(Exception exc)
        {
            if (exc is ThreadAbortException)
            {
                Thread.ResetAbort();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void ProcessHttpException(HttpException exc, string url)
        {
            var notFoundErrorString = Localization.GetString("ResourceNotFound", Localization.SharedResourceFile);
            Logger.Error(notFoundErrorString + ": - " + url, exc);

            var log = new LogInfo
            {
                BypassBuffering = true,
                LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString(),
            };
            log.LogProperties.Add(new LogDetailInfo(notFoundErrorString, "URL"));
            var context = HttpContext.Current;
            if (context != null)
            {
                log.LogProperties.Add(new LogDetailInfo("URL:", url));
            }

            LogController.Instance.AddLog(log);

            throw exc;
        }
    }
}
