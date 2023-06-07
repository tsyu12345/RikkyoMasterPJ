using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent: Agent {

    Rigidbody rBody;
    public float forceMultiplier = 10;


    public Transform target;//飛行目標

    public Transform fieldPlane;//飛行範囲 

    public 

    void Start() {
        rBody = GetComponent<Rigidbody>();
    }

    /**
    *  エピソード開始時の初期化
    * エピソードが始まるとき、つまり各訓練エピソードの開始時に呼び出されます。
    */
    public override void OnEpisodeBegin() {

        //targetの位置を設定（床の存在する範囲内で）
        target.localPosition = new Vector3(Random.Range(-fieldPlane.transform.localPosition.x, fieldPlane.transform.localPosition.x),
                                            Random.Range(0.5f, 1.5f),
                                            Random.Range(-fieldPlane.transform.localPosition.z, fieldPlane.transform.localPosition.z));
        //エージェントの位置を設定
        if (this.transform.localPosition.y < 0) {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3( 0, 0.5f, 0);
        }

    }

    /**
    * 観測の収集
    * エージェントが観測を行う必要があるときに呼び出されます
    */
    public override void CollectObservations(VectorSensor sensor) {
        // Target and Agent positions
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        // Agent velocity : エージェントの移動速度
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
        sensor.AddObservation(rBody.velocity.y);
    }


    /**
    * 行動を実行
    * エージェントが観測を行う必要があるときに呼び出されます
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        // Actions, size = 3 : 
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[1]; //左/右
        controlSignal.y = actionBuffers.ContinuousActions[2]; //上昇/下降
        controlSignal.z = actionBuffers.ContinuousActions[0]; //前進/後退

        rBody.AddForce(controlSignal * forceMultiplier);
        //報酬の設定
        //ターゲットに近づくと報酬を得る
        var distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);

        //ターゲットに近づくと報酬を得る
        if (distanceToTarget < 1.42f) {
            SetReward(1.0f);
            EndEpisode();
        }
        //飛行範囲(x,z)の外に出ると報酬を失う
        var inArea_X = (this.transform.localPosition.x < fieldPlane.transform.localPosition.x || this.transform.localPosition.x > -fieldPlane.transform.localPosition.x);
        var inArea_Z = (this.transform.localPosition.z < fieldPlane.transform.localPosition.z || this.transform.localPosition.z > -fieldPlane.transform.localPosition.z);
        
        if(!inArea_X || !inArea_Z) {
            SetReward(-1.0f);
            EndEpisode();
        }



    }


    /**
    * エージェントの操作テスト用
    * エージェントを手動で制御したいとき（テストなど）に使用します
    * continuousActions[0]が前進/後退、
    * continuousActions[1]が左/右、
    * continuousActions[2]が上昇/下降
    */
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActions = actionsOut.ContinuousActions;

        continuousActions.Clear();

        if(Input.GetKey(KeyCode.W)) {
            continuousActions[0] = 1;
        } else if(Input.GetKey(KeyCode.S)) {
            continuousActions[0] = -1;
        }

        if(Input.GetKey(KeyCode.A)) {
            continuousActions[1] = -1;
        } else if(Input.GetKey(KeyCode.D)) {
            continuousActions[1] = 1;
        }

        if(Input.GetKey(KeyCode.Q)) {
            continuousActions[2] = 1;
        } else if(Input.GetKey(KeyCode.E)) {
            continuousActions[2] = -1;
        }
    }

}
