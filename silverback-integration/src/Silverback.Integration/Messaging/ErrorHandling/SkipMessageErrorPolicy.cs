﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy simply skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;
        
        public SkipMessageErrorPolicy(ILogger<SkipMessageErrorPolicy> logger, MessageLogger messageLogger)
            : base(logger, messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public override ErrorAction HandleError(FailedMessage failedMessage, Exception exception)
        {
            _messageLogger.LogTrace(_logger, "The message will be skipped.", failedMessage);

            return ErrorAction.Skip;
        }
    }
}