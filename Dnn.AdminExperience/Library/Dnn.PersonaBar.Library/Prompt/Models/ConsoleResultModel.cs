﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information
namespace Dnn.PersonaBar.Library.Prompt.Models
{
    using System;

    using Newtonsoft.Json;

    /// <summary>
    /// Standard response object sent to client.
    /// </summary>
    [Obsolete("Moved to DotNetNuke.Prompt in the core library project. Will be removed in DNN 11.", false)]
    public class ConsoleResultModel
    {
        // the returned result - text or HTML
        [JsonProperty(PropertyName = "output")]
        public string Output;

        // is the output an error message?
        [JsonProperty(PropertyName = "isError")]
        public bool IsError;

        // is the Output HTML?
        [JsonProperty(PropertyName = "isHtml")]
        public bool IsHtml;

        // should the client reload after processing the command
        [JsonProperty(PropertyName = "mustReload")]
        public bool MustReload;

        // the response contains data to be formatted by the client
        [JsonProperty(PropertyName = "data")]
        public object Data;

        // optionally tell the client in what order the fields should be displayed
        [JsonProperty(PropertyName = "fieldOrder")]
        public string[] FieldOrder;

        [JsonProperty(PropertyName = "pagingInfo")]
        public PagingInfo PagingInfo;

        [JsonProperty(PropertyName = "nextPageCommand")]
        public string NextPageCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleResultModel"/> class.
        /// </summary>
        public ConsoleResultModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleResultModel"/> class.
        /// </summary>
        /// <param name="output"></param>
        public ConsoleResultModel(string output)
        {
            this.Output = output;
        }

        [JsonProperty(PropertyName = "records")]
        public int Records { get; set; }
    }
}
