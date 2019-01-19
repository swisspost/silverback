﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Core.Rx.Tests.TestTypes.Messages.Base
{
    public interface ICommand : IMessage
    {
    }

    public interface ICommand<out TResult> : ICommand, IRequest<TResult>
    {
    }
}