using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class DroneController : MonoBehaviour {

    [Header("Movement Parameters")]
    public float moveSpeed = 10f; // 移動速度
    public float verticalForce = 10f; // 上昇・下降の強さ
    public float tiltAng = 10f; // 傾きの角度
    public float tiltVel = 5f; // 傾きの速度
    public float rotSpeed = 100f; // 回転速度

    private Rigidbody Rbody;

    void Start() {
        Rbody = GetComponent<Rigidbody>();
    }
   
    public void flyingCtrl(ActionBuffers actions) {
        float horInput = actions.ContinuousActions[0];
        float verInput = actions.ContinuousActions[1];
        float upInput = actions.ContinuousActions[2];
        float downInput = actions.ContinuousActions[3];
        float leftRotStrength = actions.ContinuousActions[4]; // 左回転の強さ
        float rightRotStrength = actions.ContinuousActions[5]; // 右回転の強さ
        var altitudeInput = actions.ContinuousActions[0];
        var moveSpeedInput = actions.ContinuousActions[1];
        
        // 移動方向を計算
        Vector3 moveDirection = new Vector3(horInput, 0, verInput) * moveSpeed;
        // Rigidbodyに力を加えてドローンを移動させる
        Rbody.AddForce(transform.TransformDirection(moveDirection));

        // 上昇キーが押された場合
        if (upInput > 0) {
            // 上方向に力を加える
            Rbody.AddForce(Vector3.up * verticalForce * upInput);
        }

        // 下降キーが押された場合
        if (downInput > 0) {
            // 下方向に力を加える
            Rbody.AddForce(Vector3.down * verticalForce * downInput);
        }


        // 入力に基づいて傾き・回転を計算
        float actualRotStrength = rightRotStrength - leftRotStrength;

        sidewaysTiltAmount = Mathf.Lerp(sidewaysTiltAmount, -horInput * tiltAng, tiltVel * Time.fixedDeltaTime);
        forwardTiltAmount = Mathf.Lerp(forwardTiltAmount, verInput * tiltAng, tiltVel * Time.fixedDeltaTime);
        rotAmount += actualRotStrength * rotSpeed * Time.fixedDeltaTime;

        // 傾き・回転をドローンに適用
        Quaternion targetRot = Quaternion.Euler(forwardTiltAmount, rotAmount, sidewaysTiltAmount);
        transform.rotation = targetRot;
    }

}
