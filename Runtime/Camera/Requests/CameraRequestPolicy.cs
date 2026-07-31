using Immersive.Framework.Common;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declarative arbitration evidence carried by a request.
    /// Winner selection belongs exclusively to CameraOutputContext.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public readonly struct CameraRequestPolicy
    {
        public CameraRequestPolicy(int precedence, string deterministicTieBreakerId = "")
        {
            Precedence = precedence;
            DeterministicTieBreakerId = deterministicTieBreakerId.NormalizeText();
        }

        public int Precedence { get; }

        public string DeterministicTieBreakerId { get; }

        public bool HasDeterministicTieBreaker =>
            !string.IsNullOrWhiteSpace(DeterministicTieBreakerId);

        public override string ToString()
        {
            return HasDeterministicTieBreaker
                ? $"precedence={Precedence};tieBreaker='{DeterministicTieBreakerId}'"
                : $"precedence={Precedence};tieBreaker=none";
        }
    }
}
