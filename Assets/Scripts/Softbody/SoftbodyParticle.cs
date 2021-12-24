using UnityEngine;

public class SoftbodyParticle : MonoBehaviour
{
    [SerializeField] private new Rigidbody2D rigidbody;

    public Rigidbody2D Rigidbody => rigidbody;
}