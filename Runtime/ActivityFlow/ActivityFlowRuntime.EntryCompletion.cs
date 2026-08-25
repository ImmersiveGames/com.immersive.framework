using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed partial class ActivityFlowRuntime
    {
        private readonly List<IActivityContentEntryCompletionReceiver>
            _activityEntryCompletionReceivers =
                new List<IActivityContentEntryCompletionReceiver>();

        internal void AttachActivityEntryCompletionReceiver(
            IActivityContentEntryCompletionReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (!_activityEntryCompletionReceivers.Contains(receiver))
            {
                _activityEntryCompletionReceivers.Add(receiver);
            }
        }

        private void NotifyActivityEntryCompleted(ActivityAsset activity)
        {
            if (activity == null)
            {
                return;
            }

            for (int index = 0;
                 index < _activityEntryCompletionReceivers.Count;
                 index++)
            {
                IActivityContentEntryCompletionReceiver receiver =
                    _activityEntryCompletionReceivers[index];
                if (receiver == null)
                {
                    continue;
                }

                try
                {
                    receiver.OnActivityContentEntryCompleted();
                }
                catch (Exception exception)
                {
                    _readinessOccurrenceLogger.Error(
                        "Activity entry completion receiver failed. " +
                        $"receiver='{receiver.GetType().FullName}' " +
                        $"exception='{exception.GetType().Name}' message='{exception.Message}'.");
                }
            }
        }
    }
}
