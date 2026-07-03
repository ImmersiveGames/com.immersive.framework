using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result of evaluating an InputMode request without applying Unity Input behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A passive InputMode request result.")]
    public sealed class InputModeRequestResult
    {
        public InputModeRequestResult(
            InputModeRequestStatus status,
            InputModeRequest request,
            InputModeState previousState,
            InputModeState currentState,
            InputModeRequestIssue[] issues,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeRequestStatus), status) || status == InputModeRequestStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "InputMode request status must be explicit.");
            }

            Status = status;
            Request = request;
            PreviousState = previousState;
            CurrentState = currentState;
            Issues = issues ?? Array.Empty<InputModeRequestIssue>();
            Source = source.NormalizeTextOrFallback(nameof(InputModeRequestResult));
            Reason = reason.NormalizeText();
        }

        public InputModeRequestStatus Status { get; }

        public InputModeRequest Request { get; }

        public InputModeState PreviousState { get; }

        public InputModeState CurrentState { get; }

        public InputModeRequestIssue[] Issues { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeRequestStatus.Succeeded;

        public bool Ignored => Status == InputModeRequestStatus.IgnoredAlreadyInMode;

        public bool Failed => Status == InputModeRequestStatus.FailedInvalidCurrentState || Status == InputModeRequestStatus.FailedInvalidTargetMode;

        public int IssueCount => Issues.Length;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Issues.Length; i++)
                {
                    if (Issues[i].Blocking)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" previousMode='").Append(PreviousState.CurrentKind).Append("'");
            builder.Append(" currentMode='").Append(CurrentState.CurrentKind).Append("'");
            builder.Append(" revision='").Append(CurrentState.Revision).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            for (int i = 0; i < Issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(Issues[i]).Append("'");
            }

            return builder.ToString();
        }
    }
}
