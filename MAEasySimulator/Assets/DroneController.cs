using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DroneController : MonoBehaviour {

    [Header("Movement Parameters")]
    public float moveSpeed = 10f; // 移動速度
    public float verticalForce = 10f; // 上昇・下降の強さ
    public float tiltAng = 10f; // 傾きの角度
    public float tiltVel = 5f; // 傾きの速度
    public float rotSpeed = 100f; // 回転速度
    public float sidewaysTiltAmount;
    public float forwardTiltAmount;
    public float rotAmount;

    [Header("Communication Parameters")]
    public float communicationRange = 10f; // 通信範囲(半径)
    public GameObject communicateArea; //通信電波域を表すオブジェクト円形(Droneの子要素として定義)

    public Rigidbody Rbody;

    void Start() {
        Rbody = GetComponent<Rigidbody>();
        communicateArea.transform.localScale = new Vector3(communicationRange, communicationRange, communicationRange);
    }
   
    public void FlyingCtrl(ActionBuffers actions) {
        float horInput = actions.ContinuousActions[0];
        float verInput = actions.ContinuousActions[1];
        float rotInput = actions.ContinuousActions[2];
        
        // 水平、垂直方向の移動計算
        Vector3 moveDirection = new Vector3(horInput, 0, verInput) * moveSpeed;
        Rbody.AddForce(transform.TransformDirection(moveDirection));

        // 入力に基づいて傾き・回転を計算
        sidewaysTiltAmount = Mathf.Lerp(sidewaysTiltAmount, -horInput * tiltAng, tiltVel * Time.fixedDeltaTime);
        forwardTiltAmount = Mathf.Lerp(forwardTiltAmount, verInput * tiltAng, tiltVel * Time.fixedDeltaTime);
        rotAmount += rotInput * rotSpeed * Time.fixedDeltaTime;

        // 傾き・回転をドローンに適用
        Quaternion targetRot = Quaternion.Euler(forwardTiltAmount, rotAmount, sidewaysTiltAmount);
        transform.rotation = targetRot;
    }

    /// <summary>
    /// 他のドローンにメッセージを送信する。
    /// </summary>
    public bool Communicate(string message) {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, communicationRange); //範囲内にあるコライダーを取得
        var result = false;
        foreach (var hitCollider in hitColliders) {
            if (hitCollider.tag == "Drone") { //TODO:ハードコードを避ける
                hitCollider.GetComponent<DroneController>().ReceiveMessage(message);
                result = true;
            }
        }
        return result;
    }

    /// <summary>
    /// 他のドローンからメッセージを受信する（させる）。
    /// </summary>
    public void ReceiveMessage(string message) {
        Debug.Log("Message received: " + message);
    }

}
