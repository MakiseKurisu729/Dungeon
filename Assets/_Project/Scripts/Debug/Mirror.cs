using UnityEngine;

/// <summary>
/// 简易镜子：把主相机关于镜面的镜像位置/朝向算出来，赋给镜像相机，
/// 镜像相机渲染进 RenderTexture，贴在镜面上。
/// </summary>
public class Mirror : MonoBehaviour
{
    [InspectorName("玩家相机")]
    public Transform playerCam;   // Main Camera
    [InspectorName("镜像相机")]
    public Transform mirrorCam;   // MirrorCam

    void LateUpdate()
    {
        if (playerCam == null || mirrorCam == null) return;

        Vector3 planePos = transform.position;
        Vector3 n = transform.forward;          // 以 Quad 正面为镜面

        // 位置：主相机关于镜面的对称点
        float d = Vector3.Dot(playerCam.position - planePos, n);
        mirrorCam.position = playerCam.position - 2f * d * n;

        // 朝向：反射 forward / up
        Vector3 fwd = Reflect(playerCam.forward, n);
        Vector3 up = Reflect(playerCam.up, n);
        mirrorCam.rotation = Quaternion.LookRotation(fwd, up);
    }

    static Vector3 Reflect(Vector3 v, Vector3 n)
    {
        return v - 2f * Vector3.Dot(v, n) * n;
    }
}