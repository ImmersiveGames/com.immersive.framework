using Immersive.Framework.Common;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Declarative arbitration evidence carried by a request.
    /// Winner selection belongs exclusively to CameraOutputContext.
    /// </summary>
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
