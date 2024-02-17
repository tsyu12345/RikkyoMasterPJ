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
    public GameObject FieldArea;

    [Header("State of Drone")]
    public bool isGetSupplie = false; // 物資を持っているかどうか
    //public bool isOnShelter = false; // 避難所の範囲内にいるかどうか

    private DroneController Ctrl;
    private EnvManager env;
    private int GetSupplieCount = 0;
    private Vector3 shelterPosition = Vector3.zero;
    private string LogPrefix = "[Agent Surpplier]";
    private Vector3 StartPosition;

    void Start() {
        Ctrl = GetComponent<DroneController>();
        env = GetComponentInParent<EnvManager>();
        Ctrl.onReceiveMsg += OnReceiveMessage;
        Ctrl.onCrash += OnCrash;
        Ctrl.onEmptyBattery += OnEmpty;
        Ctrl.RegisterTeam(gameObject.tag);
        StartPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin() {
        env.InitializeRandomPositions();
        StartPosition = transform.localPosition;
        Reset();
    }
    public override void CollectObservations(VectorSensor sensor) {
        //自身の状態・速度を観測
        sensor.AddObservation(isGetSupplie);
        sensor.AddObservation(Ctrl.Rbody.velocity);
        //偵察エージェントから受け取った情報を観測
        sensor.AddObservation(shelterPosition);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        Ctrl.FlyingCtrl(actions);
        
        var doRelease = actions.DiscreteActions[0] == 2 ? true : false;
        var doGetSupplie = actions.DiscreteActions[0] == 1 ? true : false;
        var doNothing = actions.DiscreteActions[0] == 0 ? true : false;

        if (doRelease) {
            ReleaseSupplie();
        } else if (doGetSupplie && !isGetSupplie) { //TODO:物資が近くにあるかどうかの判定
            GetSupplie();
        }

        RewardDefinition();
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        Ctrl.InHeuristicCtrl(actionsOut);
        if(Input.GetKey(KeyCode.R)) {
            actionsOut.DiscreteActions.Array[0] = 2;
        } else if (Input.GetKey(KeyCode.G)) {
            actionsOut.DiscreteActions.Array[0] = 1;
        } else {
            actionsOut.DiscreteActions.Array[0] = 0;
        }
    }


    /// <summary>
    /// TODO:報酬設計とエピソード終了定義
    /// </summary>
    private void RewardDefinition() {

    }


    /// <summary>
    /// 他のドローンからメッセージを受信した時のイベントハンドラー
    /// </summary>
    /// <param name="message">
    /// ShelterのXYZ座標
    /// 例：(10.0, 0.0, 10.0)
    /// </param>
    private void OnReceiveMessage(string message) {
        //Vector3に変換
        var shelterPos = message.Trim('(', ')').Split(',');
        var x = float.Parse(shelterPos[0]);
        var y = float.Parse(shelterPos[1]);
        var z = float.Parse(shelterPos[2]);

        //観察に追加
        shelterPosition = new Vector3(x, y, z);
    }

    private void OnCrash(Vector3 position) {
        Debug.Log(LogPrefix + "Crash at " + position);
        EndEpisode();
    }

    private void OnEmpty() {
        Debug.Log(LogPrefix + "Battery is empty");
        //EndEpisode();
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

    private void Reset() {
        /*
        Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, DronePlatform.transform.localPosition.y + 3f, DronePlatform.transform.localPosition.z);
        transform.localPosition = pos;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        */
        transform.localPosition = StartPosition;
        //Rigidbodyの状態をリセット
        Ctrl.Rbody.useGravity = false;
        Ctrl.Rbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        //バッテリーをリセット
        Ctrl.batteryLevel = 100;
        GetSupplie();
    }

    
}
