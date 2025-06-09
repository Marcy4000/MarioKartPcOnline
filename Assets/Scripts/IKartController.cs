using UnityEngine;
using Photon.Pun;

public interface IKartController
{
    bool IsBot { get; }
    bool IsGrounded { get; }
    bool Star { get; }
    bool BulletBill { get; }
    UnityEngine.LayerMask GroundMask { get; }
    Rigidbody Rigidbody { get; }
    PhotonView PhotonView { get; }
    Transform Transform { get; }
    
    void EnterStarMode();
    void SwitchToBot();
    void SwitchToPlayer();
}
