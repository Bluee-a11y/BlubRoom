using Unity.Netcode.Components;

namespace ClubhousePC
{
    // Held objects are driven by the player holding them. Unheld physics is
    // returned to the server, so only one machine ever simulates a ball.
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
