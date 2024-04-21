namespace Celeste.Mod.CelesteArchipelago;

public enum DeathLinkStatus
{
    // no deathlink has been received since the last time madeline completed dying from deathlink
    None = 0,
    // a deathlink has been received but madeline has not started dying yet
    Pending = 1,
    // a deathlink has been received and executed but madeline has not respawned yet
    Dying = 2
}
