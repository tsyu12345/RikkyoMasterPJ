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
    public GameObject FieldArea;

    [Header("State of Drone")]
    public bool isGetSupplie = false; // 物資を持っているかどうか
    public bool wasRelease = false; // 物資を投下したかどうか
    //public bool isOnShelter = false; // 避難所の範囲内にいるかどうか
    public bool canGetSupplie = true; // 物資を取得できるかどうか

    public GameObject Supplie; // 物資
    private DroneController Ctrl;
    private EnvManager env;
    private int GetSupplieCount = 0;
    private Vector3 shelterPosition = Vector3.zero;
    private bool isGetShelterPos = false;
    private string LogPrefix = "[Agent Surpplier]";
    private Vector3 StartPosition;
    public delegate void OnLandingSurpplieOnShelter();
    public OnLandingSurpplieOnShelter onLandingSurpplieShelter;

    void Start() {
        Ctrl = GetComponent<DroneController>();
        env = GetComponentInParent<EnvManager>();
        Ctrl.onReceiveMsg += OnReceiveMessage;
        Ctrl.onCrash += OnCrash;
        Ctrl.onEmptyBattery += OnEmpty;
        Ctrl.onChargingBattery += OnChargingBattery;
        Ctrl.RegisterTeam(gameObject.tag);
        StartPosition = transform.localPosition;

        GetSupplie();
        SurpplieBox box = Supplie.GetComponent<SurpplieBox>();
        box.onLandingShelter += OnLandingSurpplieForShelter;
        box.inRangeCanGet += InRangeSurpplie;
        box.outRangeCanGet += OutRangeSurpplie;
    }

    public override void OnEpisodeBegin() {
        env.InitializeRandomPositions();
        Reset();
    }
    public override void CollectObservations(VectorSensor sensor) {
        //自身の状態・速度を観測
        sensor.AddObservation(isGetSupplie);
        sensor.AddObservation(Ctrl.Rbody.velocity);
        //偵察エージェントから受け取った情報を観測
        sensor.AddObservation(shelterPosition);
        sensor.AddObservation(isGetShelterPos);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        Ctrl.FlyingCtrl(actions);
        var doGetting = actions.DiscreteActions[0] == 2 ? true : false;
        var doRelease = actions.DiscreteActions[0] == 1 ? true : false;
        var doNothing = actions.DiscreteActions[0] == 0 ? true : false;

        if (doRelease) {
            ReleaseSupplie();
        } else if (doGetting) {
            GetSupplie();
        } else if (doNothing) {
            //何もしない

        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        Ctrl.InHeuristicCtrl(actionsOut);
        actionsOut.DiscreteActions.Array[0] = 0;
        if(Input.GetKey(KeyCode.R)) {
            actionsOut.DiscreteActions.Array[0] = 1;
        }
        if(Input.GetKey(KeyCode.G)) {
            Debug.Log("Heuristic: GetSupplie");
            actionsOut.DiscreteActions.Array[0] = 2;
        }
    }

    /// <summary>
    /// 他のドローンからメッセージを受信した時のイベントハンドラー
    /// </summary>
    /// <param name="message">
    /// ShelterのXYZ座標
    /// 例：(10.0, 0.0, 10.0)
    /// </param>
    private void OnReceiveMessage(Types.MessageData data) {
        //Vector3に変換
        var detectType = data.type;
        if(detectType == "Shelter") {
            Vector3 pos = Utils.ConvertStringToVector3(data.content);
            shelterPosition = new Vector3(pos.x, pos.y, pos.z);
            isGetShelterPos = true;
        }
    }

    /// <summary>
    /// 物資が避難所に着陸した時のイベントハンドラー
    /// </summary>
    private void OnLandingSurpplieForShelter() {
        AddReward(1.0f);
        onLandingSurpplieShelter?.Invoke();
        EndEpisode(); //TODO:複数個の物資を運ぶ場合の対応
        
    }

    private void OnCrash(Vector3 position) {
        Debug.Log(LogPrefix + "Crash at " + position);
        SetReward(-1.0f);
        EndEpisode();
    }

    private void OnChargingBattery() {
        if(Ctrl.batteryLevel < 20) {
            AddReward(0.5f);
        }
    }

    private void OnEmpty() {
        Debug.Log(LogPrefix + "Battery is empty");
        //EndEpisode();
    }

    private void InRangeSurpplie() {
        if(isGetSupplie) {
            return;
        }
        Debug.Log(LogPrefix + "InRangeCanGet");
        AddReward(0.1f);
        canGetSupplie = true;
    }

    private void OutRangeSurpplie() {
        Debug.Log(LogPrefix + "OutRangeCanGet");
        canGetSupplie = false;
    }

    private void GetSupplie(bool force=false) { 
        Debug.Log(LogPrefix + "canGetSupplie" + canGetSupplie);
        if(!canGetSupplie && !force) {
            return;
        }
        Debug.Log(LogPrefix + "GetSupplie");
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
    }
    private void ReleaseSupplie() { 
        //物資を落とす
        Supplie.transform.parent = FieldArea.transform;
        Supplie.GetComponent<Rigidbody>().useGravity = true;
        //位置を固定解除
        Supplie.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        isGetSupplie = false;
        wasRelease = true;
    }

    private void Reset() {
        /*
        Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, DronePlatform.transform.localPosition.y + 3f, DronePlatform.transform.localPosition.z);
        transform.localPosition = pos;
        */
        transform.localPosition = StartPosition;
        transform.localRotation = Quaternion.Euler(0, 0, 0);

        Ctrl.Rbody.velocity = Vector3.zero;
        Ctrl.Rbody.useGravity = false;
        //X,Z回転を固定
        Ctrl.Rbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        //バッテリーをリセット
        Ctrl.batteryLevel = 100;
        //Supplie.GetComponent<SurpplieBox>().Reset();
        canGetSupplie = true;
        GetSupplie(true);
    }

    
}
