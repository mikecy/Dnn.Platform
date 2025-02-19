﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information
namespace DotNetNuke.UI.UserControls
{
    using System;
    using System.IO;
    using System.Web.UI.WebControls;

    using DotNetNuke.Common;
    using DotNetNuke.Common.Utilities;
    using DotNetNuke.Entities.Tabs;
    using DotNetNuke.Framework;
    using DotNetNuke.Services.Exceptions;
    using DotNetNuke.Services.FileSystem;
    using DotNetNuke.Services.Localization;

    using Calendar = DotNetNuke.Common.Utilities.Calendar;

    public abstract class URLTrackingControl : UserControlBase
    {
        protected Label Label1;
        protected Label Label2;
        protected Label Label3;
        protected Label Label4;
        protected Label Label5;
        protected Label Label6;
        protected Label Label7;
        protected LinkButton cmdDisplay;
        protected HyperLink cmdEndCalendar;
        protected HyperLink cmdStartCalendar;
        protected DataGrid grdLog;
        protected Label lblClicks;
        protected Label lblCreatedDate;
        protected Label lblLastClick;
        protected Label lblLogURL;
        protected Label lblTrackingURL;
        protected Label lblURL;
        protected Panel pnlLog;
        protected Panel pnlTrack;
        protected TextBox txtEndDate;
        protected TextBox txtStartDate;
        protected CompareValidator valEndDate;
        protected CompareValidator valStartDate;
        private string formattedURL = string.Empty;
        private int moduleID = -2;
        private string trackingURL = string.Empty;
        private string url = string.Empty;
        private string localResourceFile;

        public string FormattedURL
        {
            get
            {
                return this.formattedURL;
            }

            set
            {
                this.formattedURL = value;
            }
        }

        public string TrackingURL
        {
            get
            {
                return this.trackingURL;
            }

            set
            {
                this.trackingURL = value;
            }
        }

        public string URL
        {
            get
            {
                return this.url;
            }

            set
            {
                this.url = value;
            }
        }

        public int ModuleID
        {
            get
            {
                int moduleID = this.ModuleID;
                if (moduleID == -2)
                {
                    if (this.Request.QueryString["mid"] != null)
                    {
                        int.TryParse(this.Request.QueryString["mid"], out moduleID);
                    }
                }

                return moduleID;
            }

            set
            {
                this.moduleID = value;
            }
        }

        public string LocalResourceFile
        {
            get
            {
                string fileRoot;
                if (string.IsNullOrEmpty(this.localResourceFile))
                {
                    fileRoot = this.TemplateSourceDirectory + "/" + Localization.LocalResourceDirectory + "/URLTrackingControl.ascx";
                }
                else
                {
                    fileRoot = this.localResourceFile;
                }

                return fileRoot;
            }

            set
            {
                this.localResourceFile = value;
            }
        }

        /// <inheritdoc/>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.cmdDisplay.Click += this.CmdDisplay_Click;

            try
            {
                // this needs to execute always to the client script code is registred in InvokePopupCal
                this.cmdStartCalendar.NavigateUrl = Calendar.InvokePopupCal(this.txtStartDate);
                this.cmdEndCalendar.NavigateUrl = Calendar.InvokePopupCal(this.txtEndDate);
                if (!this.Page.IsPostBack)
                {
                    if (!string.IsNullOrEmpty(this.URL))
                    {
                        this.lblLogURL.Text = this.URL; // saved for loading Log grid
                        TabType urlType = Globals.GetURLType(this.URL);
                        if (urlType == TabType.File && this.URL.StartsWith("fileid=", StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            // to handle legacy scenarios before the introduction of the FileServerHandler
                            var fileName = Path.GetFileName(this.URL);

                            var folderPath = this.URL.Substring(0, this.URL.LastIndexOf(fileName));
                            var folder = FolderManager.Instance.GetFolder(this.PortalSettings.PortalId, folderPath);

                            var file = FileManager.Instance.GetFile(folder, fileName);

                            this.lblLogURL.Text = "FileID=" + file.FileId;
                        }

                        var objUrls = new UrlController();
                        UrlTrackingInfo objUrlTracking = objUrls.GetUrlTracking(this.PortalSettings.PortalId, this.lblLogURL.Text, this.ModuleID);
                        if (objUrlTracking != null)
                        {
                            if (string.IsNullOrEmpty(this.FormattedURL))
                            {
                                this.lblURL.Text = Globals.LinkClick(this.URL, this.PortalSettings.ActiveTab.TabID, this.ModuleID, false);
                                if (!this.lblURL.Text.StartsWith("http") && !this.lblURL.Text.StartsWith("mailto"))
                                {
                                    this.lblURL.Text = Globals.AddHTTP(this.Request.Url.Host) + this.lblURL.Text;
                                }
                            }
                            else
                            {
                                this.lblURL.Text = this.FormattedURL;
                            }

                            this.lblCreatedDate.Text = objUrlTracking.CreatedDate.ToString();

                            if (objUrlTracking.TrackClicks)
                            {
                                this.pnlTrack.Visible = true;
                                if (string.IsNullOrEmpty(this.TrackingURL))
                                {
                                    if (!this.URL.StartsWith("http"))
                                    {
                                        this.lblTrackingURL.Text = Globals.AddHTTP(this.Request.Url.Host);
                                    }

                                    this.lblTrackingURL.Text += Globals.LinkClick(this.URL, this.PortalSettings.ActiveTab.TabID, this.ModuleID, objUrlTracking.TrackClicks);
                                }
                                else
                                {
                                    this.lblTrackingURL.Text = this.TrackingURL;
                                }

                                this.lblClicks.Text = objUrlTracking.Clicks.ToString();
                                if (!Null.IsNull(objUrlTracking.LastClick))
                                {
                                    this.lblLastClick.Text = objUrlTracking.LastClick.ToString();
                                }
                            }

                            if (objUrlTracking.LogActivity)
                            {
                                this.pnlLog.Visible = true;

                                this.txtStartDate.Text = DateTime.Today.AddDays(-6).ToShortDateString();
                                this.txtEndDate.Text = DateTime.Today.AddDays(1).ToShortDateString();
                            }
                        }
                    }
                    else
                    {
                        this.Visible = false;
                    }
                }
            }
            catch (Exception exc) // Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        private void CmdDisplay_Click(object sender, EventArgs e)
        {
            try
            {
                string strStartDate = this.txtStartDate.Text;
                if (!string.IsNullOrEmpty(strStartDate))
                {
                    strStartDate = strStartDate + " 00:00";
                }

                string strEndDate = this.txtEndDate.Text;
                if (!string.IsNullOrEmpty(strEndDate))
                {
                    strEndDate = strEndDate + " 23:59";
                }

                var objUrls = new UrlController();

                // localize datagrid
                Localization.LocalizeDataGrid(ref this.grdLog, this.LocalResourceFile);
                this.grdLog.DataSource = objUrls.GetUrlLog(this.PortalSettings.PortalId, this.lblLogURL.Text, this.ModuleID, Convert.ToDateTime(strStartDate), Convert.ToDateTime(strEndDate));
                this.grdLog.DataBind();
            }
            catch (Exception exc) // Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }
    }
}
