﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace Dnn.PersonaBar.Vocabularies.Exceptions
{
    using System;

    public class VocabulariesException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VocabulariesException"/> class.
        /// </summary>
        public VocabulariesException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VocabulariesException"/> class.
        /// </summary>
        /// <param name="message"></param>
        public VocabulariesException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VocabulariesException"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public VocabulariesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
