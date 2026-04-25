using UnityEngine;

public interface IFighterInput
{
    bool GuardHeld { get; }
    bool PunchHeld { get; }
    bool PunchReleasedThisFrame { get; }
    float Focus01 { get; }
    Vector2 RawSwivel { get; }  // normalized -1..1, pre-smoothing
}
