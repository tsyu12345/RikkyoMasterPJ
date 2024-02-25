using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class SpyAgent : Agent {

    private Transform Sensor;
    private float rayDistance = 40f; // レイキャストの距離
    //TODO:以下複数の避難所を検出する場合の対応
    private string targetTag = "Shelter";
    private Vector3 targetPos = Vector3.zero;
    private bool isFindTarget = false;
    
    private DroneController _controller;
    private EnvManager _env;
    private int _findCount = 0;

    private delegate void OnFindShelter(Vector3 pos);
    private OnFindShelter _onFindShelter;
    private string LogPrefix = "[Agent Spy]";
    private Vector3 StartPosition;
    
    void Start() {
        _controller = GetComponent<DroneController>();
        _env = GetComponentInParent<EnvManager>();
        _controller.RegisterTeam(gameObject.tag);
        _controller.AddCommunicateTarget(targetTag);
        _controller.onCrash += OnCrash;
        _controller.onEmptyBattery += OnEmpty;
        Sensor = transform.Find("Sensor");
        StartPosition = transform.localPosition;
    }

    public override void OnEpisodeBegin() {
        _env.InitializeRandomPositions();
        Reset();
    }

    /// <summary>
    /// 観測情報
    /// １．自身の速度 Vector3
    /// ２．偵察して得た避難所の位置 Vector3
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(_controller.Rbody.velocity);
        RaycastHit hit;
        // 子GameObjectの位置と方向からレイキャストを実行
        if (Physics.Raycast(Sensor.transform.position, Sensor.forward, out hit, rayDistance)) {
            if (hit.collider.CompareTag(targetTag)) {
                targetPos = hit.point;
                _onFindShelter?.Invoke(targetPos);
            }
        }
        sensor.AddObservation(targetPos);
        sensor.AddObservation(isFindTarget);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        _controller.FlyingCtrl(actions);
        RewardDefinition();
    }

    private void RewardDefinition() {
        //1エピソード中に避難所を検出した回数に応じて報酬を与える
        if (_findCount > 0) {
            AddReward(0.5f);
        }
    }

    private void OnCrash(Vector3 position) {
        Debug.Log(LogPrefix + "Crash at " + position);
        SetReward(-1.0f);
        EndEpisode();
    }

    private void OnEmpty() {
        Debug.Log(LogPrefix + "Battery is empty");
    }

    private void OnChargingBattery() {
        Debug.Log(LogPrefix + "Charging Battery");
        if(_controller.batteryLevel < 20) {
            AddReward(0.5f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut) {
        _controller.InHeuristicCtrl(actionsOut);
    }

    /// <summary>
    /// レイキャストで避難所を検出した際のイベントハンドラー
    /// </summary>
    /// <param name="pos"></param> <summary>
    private void _OnFindShelter(Vector3 pos) {
        //検出情報を発信
        _findCount++;
        var data = new Types.MessageData {
            type = "Shelter",
            content = pos.ToString()
        };
        _controller.Communicate(data);
        Debug.Log(LogPrefix + "Find shelter at " + pos.ToString());
        isFindTarget = true; //TODO:この情報を観測に追加するように
    }


    private void Reset() {
        _findCount = 0;
        transform.localPosition = StartPosition;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        _controller.batteryLevel = 100;
        _controller.Rbody.velocity = Vector3.zero;
        _controller.Rbody.useGravity = false;
        //X,Z回転を固定
        _controller.Rbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }





}
