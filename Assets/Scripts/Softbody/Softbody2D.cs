using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Softbody2D))]
public class Softbody2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Create"))
        {
            var component = target as Softbody2D;
            if (component != null)
            {
                component.Create();
            }
        }
        
        base.OnInspectorGUI();
    }
}
#endif

public class Softbody2D : MonoBehaviour
{
    [Header("=== Create Settings ===")]
    [SerializeField] private int particleCount = 32; // パーティクル数
    [SerializeField] private float softbodyRadius = 4f; // ソフトボディの半径
    [SerializeField] private bool aroundConfigureDistance = true; // バネの距離を自動調整
    [SerializeField] private bool centerConfigureDistance = true; // バネの距離を自動調整
    [SerializeField] private SoftbodyParticle _particlePrefab; // パーティクルPrefab
    
    [Header("=== Runtime Settings ===")]
    [SerializeField] private float aroundFrequency = 2f; // 外周パーティクル同士を結ぶバネの硬さ
    [SerializeField] private float centerFrequency = 2f; // 外周-中心パーティクル同士を結ぶバネの硬さ

    [Header("=== Cache ===")]
    [SerializeField] private SoftbodyParticle[] aroundParticles; // 外周パーティクル
    [SerializeField] private SoftbodyParticle centerParticle; // 中心パーティクル
    [SerializeField] private List<SpringJoint2D> aroundJoints = new List<SpringJoint2D>(); // 外周どうしを結ぶばね
    [SerializeField] private List<SpringJoint2D> centerJoints = new List<SpringJoint2D>(); // 中心と外周を結ぶばね

    private void Update()
    {
        foreach (var joint in aroundJoints)
        {
            joint.frequency = aroundFrequency;
        }
        foreach (var joint in centerJoints)
        {
            joint.frequency = centerFrequency;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.Translate(new Vector3(0, 0, -4f));
        foreach (var joint in aroundJoints)
        {
            Gizmos.color = Color.red;
            if (joint != null)
                Gizmos.DrawLine(joint.transform.position, joint.connectedBody.position);
        }
        foreach (var joint in centerJoints)
        {
            Gizmos.color = Color.yellow;
            if (joint != null)
                Gizmos.DrawLine(joint.transform.position, joint.connectedBody.position);
        }
    }

    /// <summary>
    /// ソフトボディの作成
    /// </summary>
    public void Create()
    {
        CreateParticles();
        CreateJoint();
    }

    /// <summary>
    /// パーティクルをJointで接続
    /// </summary>
    private void CreateJoint()
    {
        aroundJoints.Clear();
        centerJoints.Clear();
        
        // 外周パーティクル同士を接続
        for (int i = 0; i < aroundParticles.Length; i++)
        {
            var p1 = aroundParticles[i];
            var p2 = aroundParticles[(i + 1) % aroundParticles.Length];

            var joint1 = p1.gameObject.AddComponent<SpringJoint2D>();
            var joint2 = p2.gameObject.AddComponent<SpringJoint2D>();

            joint1.connectedBody = p2.Rigidbody;
            joint2.connectedBody = p1.Rigidbody;

            aroundJoints.Add(joint1);
            aroundJoints.Add(joint2);
        }
        
        // 中心パーティクルと外周パーティクルを接続
        for (int i = 0; i < aroundParticles.Length; i++)
        {
            var p1 = aroundParticles[i];
            var p2 = centerParticle;

            var joint1 = p1.gameObject.AddComponent<SpringJoint2D>();
            var joint2 = p2.gameObject.AddComponent<SpringJoint2D>();

            joint1.connectedBody  = p2.Rigidbody;
            joint2.connectedBody  = p1.Rigidbody;
            
            centerJoints.Add(joint1);
            centerJoints.Add(joint2);
        }
        
        // Jointの設定
        foreach (var joint in aroundJoints)
        {
            joint.autoConfigureDistance = aroundConfigureDistance;
            joint.enableCollision = false;
        }
        foreach (var joint in centerJoints)
        {
            joint.autoConfigureDistance = aroundConfigureDistance;
            joint.enableCollision = false;
        }
    }

    /// <summary>
    /// パーティクル生成
    /// </summary>
    private void CreateParticles()
    {
        if (centerParticle != null)
        {
            DestroyObject(centerParticle.gameObject);
        }
        foreach (var p in aroundParticles)
        {
            DestroyObject(p.gameObject);
        }

        centerParticle = CreateParticle(Vector3.zero);
        aroundParticles = new SoftbodyParticle[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float radian = i * Mathf.PI * 2f / (particleCount - 1);
            var position = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * softbodyRadius;
            aroundParticles[i] = CreateParticle(position);
        }
    }

    /// <summary>
    /// パーティクル作成
    /// </summary>
    private SoftbodyParticle CreateParticle(Vector3 position)
    {
        var p = Instantiate(_particlePrefab, transform);
        p.transform.localPosition = position;
        return p;
    }

    static void DestroyObject(GameObject target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
