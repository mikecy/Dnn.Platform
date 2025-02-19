﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace DotNetNuke.Entities.Content.Workflow
{
    using System;
    using System.Linq;

    using DotNetNuke.Entities.Content.Workflow.Entities;
    using DotNetNuke.Entities.Content.Workflow.Repositories;
    using DotNetNuke.Framework;
    using DotNetNuke.Services.Localization;

    public class SystemWorkflowManager : ServiceLocator<ISystemWorkflowManager, SystemWorkflowManager>, ISystemWorkflowManager
    {
        public const string DirectPublishWorkflowKey = "DirectPublish";
        public const string SaveDraftWorkflowKey = "SaveDraft";
        public const string ContentAprovalWorkflowKey = "ContentApproval";
        private readonly IWorkflowRepository workflowRepository;
        private readonly IWorkflowStateRepository workflowStateRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemWorkflowManager"/> class.
        /// </summary>
        public SystemWorkflowManager()
        {
            this.workflowRepository = WorkflowRepository.Instance;
            this.workflowStateRepository = WorkflowStateRepository.Instance;
        }

        /// <inheritdoc/>
        public void CreateSystemWorkflows(int portalId)
        {
            this.CreateDirectPublishWorkflow(portalId);
            this.CreateSaveDraftWorkflow(portalId);
            this.CreateContentApprovalWorkflow(portalId);
        }

        /// <inheritdoc/>
        public Entities.Workflow GetDirectPublishWorkflow(int portalId)
        {
            return this.workflowRepository.GetSystemWorkflows(portalId).SingleOrDefault(sw => sw.WorkflowKey == DirectPublishWorkflowKey);
        }

        /// <inheritdoc/>
        public Entities.Workflow GetSaveDraftWorkflow(int portalId)
        {
            return this.workflowRepository.GetSystemWorkflows(portalId).SingleOrDefault(sw => sw.WorkflowKey == SaveDraftWorkflowKey);
        }

        /// <inheritdoc/>
        public Entities.Workflow GetContentApprovalWorkflow(int portalId)
        {
            return this.workflowRepository.GetSystemWorkflows(portalId).SingleOrDefault(sw => sw.WorkflowKey == ContentAprovalWorkflowKey);
        }

        /// <inheritdoc/>
        public WorkflowState GetDraftStateDefinition(int order)
        {
            var state = this.GetDefaultWorkflowState(order);
            state.StateName = Localization.GetString("DefaultWorkflowState1.StateName");
            return state;
        }

        /// <inheritdoc/>
        public WorkflowState GetPublishedStateDefinition(int order)
        {
            var state = this.GetDefaultWorkflowState(order);
            state.StateName = Localization.GetString("DefaultWorkflowState3.StateName");
            return state;
        }

        /// <inheritdoc/>
        public WorkflowState GetReadyForReviewStateDefinition(int order)
        {
            var state = this.GetDefaultWorkflowState(order);
            state.StateName = Localization.GetString("DefaultWorkflowState2.StateName");
            state.SendNotification = true;
            state.SendNotificationToAdministrators = true;
            return state;
        }

        /// <inheritdoc/>
        protected override Func<ISystemWorkflowManager> GetFactory()
        {
            return () => new SystemWorkflowManager();
        }

        private WorkflowState GetDefaultWorkflowState(int order)
        {
            return new WorkflowState
            {
                IsSystem = true,
                SendNotification = true,
                SendNotificationToAdministrators = false,
                Order = order,
            };
        }

        private void CreateDirectPublishWorkflow(int portalId)
        {
            var workflow = new Entities.Workflow
            {
                WorkflowName = Localization.GetString("DefaultDirectPublishWorkflowName"),
                Description = Localization.GetString("DefaultDirectPublishWorkflowDescription"),
                WorkflowKey = DirectPublishWorkflowKey,
                IsSystem = true,
                PortalID = portalId,
            };
            this.workflowRepository.AddWorkflow(workflow);
            var publishedState = this.GetPublishedStateDefinition(1);
            publishedState.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(publishedState);
        }

        private void CreateSaveDraftWorkflow(int portalId)
        {
            var workflow = new Entities.Workflow
            {
                WorkflowName = Localization.GetString("DefaultSaveDraftWorkflowName"),
                Description = Localization.GetString("DefaultSaveDraftWorkflowDescription"),
                WorkflowKey = SaveDraftWorkflowKey,
                IsSystem = true,
                PortalID = portalId,
            };
            this.workflowRepository.AddWorkflow(workflow);

            var state = this.GetDraftStateDefinition(1);
            state.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(state);

            state = this.GetPublishedStateDefinition(2);
            state.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(state);
        }

        private void CreateContentApprovalWorkflow(int portalId)
        {
            var workflow = new Entities.Workflow
            {
                WorkflowName = Localization.GetString("DefaultWorkflowName"),
                Description = Localization.GetString("DefaultWorkflowDescription"),
                WorkflowKey = ContentAprovalWorkflowKey,
                IsSystem = true,
                PortalID = portalId,
            };
            this.workflowRepository.AddWorkflow(workflow);

            var state = this.GetDraftStateDefinition(1);
            state.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(state);

            state = this.GetReadyForReviewStateDefinition(2);
            state.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(state);

            state = this.GetPublishedStateDefinition(3);
            state.WorkflowID = workflow.WorkflowID;
            this.workflowStateRepository.AddWorkflowState(state);
        }
    }
}
