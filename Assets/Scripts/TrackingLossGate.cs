using System;

namespace Facens.SafetyLens
{
    public enum TargetVisibility { Tracking, Unstable, Lost }

    public sealed class TrackingLossGate
    {
        readonly float _graceSeconds;
        readonly int _minFrames;
        float _degradedSince = -1f;
        int _degradedFrames;

        public TrackingLossGate(float graceSeconds = 1.5f, int minFrames = 10)
        {
            _graceSeconds = Math.Max(0f, graceSeconds);
            _minFrames = Math.Max(1, minFrames);
        }

        public TargetVisibility Evaluate(bool isTracking, float now)
        {
            if (isTracking) { Reset(); return TargetVisibility.Tracking; }
            if (_degradedSince < 0f) _degradedSince = now;
            _degradedFrames++;
            bool expired = now - _degradedSince >= _graceSeconds
                           && _degradedFrames >= _minFrames;
            return expired ? TargetVisibility.Lost : TargetVisibility.Unstable;
        }

        public void Reset() { _degradedSince = -1f; _degradedFrames = 0; }
    }
}
