using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace iRailTracker
{
    public sealed class AutoRefreshMessage : ValueChangedMessage<bool>
    {
        public AutoRefreshMessage() : base(true) { }
    }
}
