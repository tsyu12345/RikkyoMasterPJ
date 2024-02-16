using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

///<summary>
/// 物資輸送エージェント 1台分のエージェントクラス
/// </summary>
public class SurpplieAgent : Agent {
   [Header("Operation Targets")]
    public GameObject DronePlatform; //ドローンの離着陸プラットフォーム 
    public GameObject Supplie; // 物資
    //public GameObject FieldFloor; // フィールド
    public GameObject FieldArea;

    [Header("State of Drone")]
    public bool isGetSupplie = false; // 物資を持っているかどうか
    //public bool isOnShelter = false; // 避難所の範囲内にいるかどうか

    private DroneController Ctrl;
    private EnvManager env;
    private int GetSupplieCount = 0;
    void Start() {
        Ctrl = GetComponent<DroneController>();
        env = GetComponentInParent<EnvManager>();
    }

    public override void OnEpisodeBegin() {
        env.InitializeRandomPositions();
    }
    public override void CollectObservations(VectorSensor sensor) {
        //状態を観測
        sensor.AddObservation(isGetSupplie);
        //速度を観測
        sensor.AddObservation(Ctrl.Rbody.velocity);

    }

    public override void OnActionReceived(ActionBuffers actions) {
        Ctrl.FlyingCtrl(actions);
        
        var doRelease = actions.DiscreteActions[0] == 2 ? true : false;
        var doGetSupplie = actions.DiscreteActions[0] == 1 ? true : false;
        var doSaving = actions.DiscreteActions[0] == 0 ? true : false;

        if (doRelease) {
            ReleaseSupplie();
        } else if (doGetSupplie) {
            GetSupplie();
        }

        RewardDefinition();
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        //todo: implement
    }


    /// <summary>
    /// 報酬設計とエピソード終了定義
    /// </summary>
    private void RewardDefinition() {

    }

    private void GetSupplie() {
        Debug.Log("[Agent] Get Supplie");
        //物資の重力を無効化 
        Supplie.GetComponent<Rigidbody>().useGravity = false;
        // 物資を取る : オブジェクトの親をドローンに設定
        Supplie.transform.parent = transform;
        // 物資の位置をドローンの下部に設定
        Supplie.transform.localPosition = new Vector3(0, -4f, 0);
        Supplie.transform.localRotation = Quaternion.Euler(0, 0, 0);
        //位置を固定
        Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        
        isGetSupplie = true;
        GetSupplieCount++;
        //GetSupplieCounter.text = GetSupplieCount.ToString();
    }
    private void ReleaseSupplie() {
        //物資を落とす
        Supplie.GetComponent<Rigidbody>().useGravity = true;
        Supplie.transform.parent = FieldArea.transform;
        //位置を固定解除
        Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        isGetSupplie = false;
    }

    
}
