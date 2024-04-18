using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class SpyAgent : Agent {


    [Header("Sensor Settings")]
    public int SensorCount = 5; //レイキャストの本数（この本数のレイを扇型に展開）
    public float SensorDistance = 10f; //レイ１本あたりの長さ
    public float SensorAngle = 120.0f; //レイ全体（扇型）の角度
    [Header("Sensor Color Settings")]
    public Color Detected = Color.red;
    public Color DetectedOthers = Color.yellow;
    public Color NotDetected = Color.blue;
    [Header("Communicate Settings")]
    public string CommunicationTargetTag = "Surpplier";

    private Transform Sensor;
    private float rayDistance = 40f; // レイキャストの距離
    //TODO:以下複数の避難所を検出する場合の対応
    private string targetTag = "Shelter";
    private Vector3 targetPos = Vector3.zero;
    private bool isFindTarget = false;
    
    private DroneController _controller;
    private EnvManager _env;
    private int _findCount = 0;

    public delegate void OnFindShelter(Vector3 pos);
    public OnFindShelter onFindShelter;
    private string LogPrefix = "[Agent Spy]";
    private Vector3 StartPosition;

    private Ray SpySensor;
    
    void Start() {
        _controller = GetComponent<DroneController>();
        _env = GetComponentInParent<EnvManager>();
        _controller.RegisterTeam(gameObject.tag);
        _controller.AddCommunicateTarget(targetTag);
        _controller.onCrash += OnCrash;
        _controller.onEmptyBattery += OnEmpty;
        _controller.onChargingBattery += OnChargingBattery;
        onFindShelter += onDetectShelter;
        Sensor = transform.Find("Sensor");
        StartPosition = transform.localPosition;
        SpySensor = new Ray(Sensor.position, Sensor.forward);
    }

    public override void OnEpisodeBegin() {
        //_env.InitializeRandomPositions();
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
        var findShelterCount = ShelterScan(); //レイキャストによる避難所検出
        //TODO:複数の避難所を検出する場合の対応
        sensor.AddObservation(targetPos);
        sensor.AddObservation(findShelterCount);
        AddReward(findShelterCount);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        _controller.FlyingCtrl(actions);
        RewardDefinition();
    }


    /**EventHandlers**/

    private void OnCrash(Vector3 position) {
        Debug.Log(LogPrefix + "Crash at " + position);
        SetReward(-1.0f);
        EndEpisode();
    }

    private void OnEmpty() {
        Debug.Log(LogPrefix + "Battery is empty");
        SetReward(-1.0f);
    }

    private void OnChargingBattery() {
        Debug.Log(LogPrefix + "Charging Battery");
        if(_controller.batteryLevel < 20) {
            AddReward(0.5f);
        }
    }

    /*********/

    public override void Heuristic(in ActionBuffers actionsOut) {
        _controller.InHeuristicCtrl(actionsOut);
    }

    /// <summary>
    /// レイキャストで避難所を検出した際のイベントハンドラー
    /// </summary>
    /// <param name="pos"></param> <summary>
    private void onDetectShelter(Vector3 pos) {
        //検出情報を発信
        var data = new Types.MessageData {
            type = "Shelter",
            content = pos.ToString()
        };
        //Debug.Log(LogPrefix + "Find shelter at " + pos.ToString());
        isFindTarget = true; 
        //Surpplierエージェントに伝送
        var targetDrones = GameObject.FindGameObjectsWithTag(CommunicationTargetTag);
        foreach (var drone in targetDrones) {
            _controller.Communicate(data, drone);
        }
    }


    private int ShelterScan() {
        int count = 0;
        RaycastHit hit;
        float startAngle = -SensorAngle / 2; // 最初のレイの角度
        float angleStep = SensorAngle / (SensorCount - 1); // 各レイ間の角度

        for (int i = 0; i < SensorCount; i++) {
            float currentAngle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * Sensor.forward; // 現在のレイの方向

            // 子GameObjectの位置と方向からレイキャストを実行
            // 子GameObjectの位置と方向からレイキャストを実行
            if (Physics.Raycast(Sensor.position, direction, out hit, SensorDistance)) {
                if (hit.collider.CompareTag(targetTag)) {
                    targetPos = hit.point;
                    onFindShelter?.Invoke(targetPos);
                    count = 1; //TODO:複数の避難所を検出する場合の対応
                    // レイが避難所にヒットしたことを示すために、色を変更して描画
                    Debug.DrawRay(Sensor.position, direction * SensorDistance, Detected);
                } else {
                    // レイが避難所以外のものにヒットした場合
                    Debug.DrawRay(Sensor.position, direction * SensorDistance, DetectedOthers);
                }
            } else {
                // レイが何もヒットしなかった場合
                Debug.DrawRay(Sensor.position, direction * SensorDistance, NotDetected);
            }
        }
        return count;
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
