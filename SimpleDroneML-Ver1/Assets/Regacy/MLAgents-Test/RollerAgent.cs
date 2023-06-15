using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class RollerAgent: Agent {

    [SerializeField] private Transform target;
    private Rigidbody _rBody;


    /**
    * シーン開始時に呼び出される。初期化処理を行う。
    */
    public override void Initialize() {
        _rBody = GetComponent<Rigidbody>();
    }

    /**
    * 各エピソード開始時に呼び出される.
    * エージェントの初期位置の設定や、ターゲットの位置のリセットを行う。
    */
    public override void OnEpisodeBegin() {
        if (this.transform.localPosition.y < 0) {
            // If the Agent fell, zero its momentum
            _rBody.angularVelocity = Vector3.zero;
            _rBody.velocity = Vector3.zero;
            transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
        }

        // Move the target to a new spot// Targetの位置のリセット
        target.localPosition = new Vector3(Random.value*8-4, 0.5f, Random.value*8-4);

    }

    /**
    * 観測値の取得を行う。
    * 今回は、ターゲットの位置とエージェントの位置情報、エージェントの速度情報を観測値として取得する。
    * @param sensor 観測値を格納するVectorSensor
    * 
    */
    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(_rBody.velocity.x);
        sensor.AddObservation(_rBody.velocity.z);
    }

    /**
    * 行動の実行時に呼び出される。
    * 行動の設定と、報酬の設定を行う。
    * @param actions 行動の設定を格納するActionBuffers
    */
    public override void OnActionReceived(ActionBuffers actions) {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actions.ContinuousActions[0];
        controlSignal.z = actions.ContinuousActions[1];
        _rBody.AddForce(controlSignal * 10);

        // Rewards
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        // Reached target
        if (distanceToTarget < 1.42f) {
            SetReward(1.0f);
            EndEpisode();
        }

        // Fell off platform
        if (transform.localPosition.y < 0) {
            EndEpisode();
        }
    }

    /**
    * ヒューリスティックモード時に呼び出される。
    * キーボードの入力を受け付け、行動を設定する。
    */
    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

}

