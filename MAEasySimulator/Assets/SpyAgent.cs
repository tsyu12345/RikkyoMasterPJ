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
    
    private DroneController _controller;
    private EnvManager _env;
    private int _findCount = 0;

    private delegate void OnFindShelter(Vector3 pos);
    private OnFindShelter _onFindShelter;
    private string LogPrefix = "[Agent Spy]";
    
    void Start() {
        _controller = GetComponent<DroneController>();
        _env = GetComponentInParent<EnvManager>();
        _controller.RegisterTeam(gameObject.tag);
        _controller.AddCommunicateTarget(targetTag);
        Sensor = transform.Find("Sensor");
    }

    public override void OnEpisodeBegin() {
        _env.InitializeRandomPositions();
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
    }

    public override void OnActionReceived(ActionBuffers actions) {
        _controller.FlyingCtrl(actions);
        RewardDefinition();
    }

    private void RewardDefinition() {
        
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
        _controller.Communicate(pos.ToString());
        Debug.Log(LogPrefix + "Find shelter at " + pos.ToString());
    }





}
