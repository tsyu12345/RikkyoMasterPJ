using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;


/// <summary>
/// 探索・通信ドローンの挙動,報酬を定義するクラス
/// 【行動一覧】
///  ・移動座標の指定
/// </summary>
public class TransmitAgent : Agent {
    
    public GameObject DroneStation;
    private UnityEngine.AI.NavMeshAgent NavAI;
    private Rigidbody rb;
    private DroneController DroneController;

    void Start() {
        NavAI = GetComponent<UnityEngine.AI.NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        DroneController = GetComponent<DroneController>();
    }

    public override void OnEpisodeBegin() {
        //DroneをDroneStationに戻す
        this.transform.position = DroneStation.transform.position;
        this.transform.rotation = DroneStation.transform.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor) {
        //Droneの位置
        sensor.AddObservation(this.transform.position);
        //Droneの速度
        sensor.AddObservation(rb.velocity);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        DroneController.flyingCtrl(actions);
    }


    

}
