using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class DroneAgent: Agent {

    Rigidbody rBody;

    void Start() {
        rBody = GetComponent<Rigidbody>();
    }

    /**
    *  エピソード開始時の初期化
    * エピソードが始まるとき、つまり各訓練エピソードの開始時に呼び出されます。
    */
    public override void OnEpisodeBegin() {

    }

    /**
    * 観測の収集
    * エージェントが観測を行う必要があるときに呼び出されます
    */
    public override void CollectObservations(VectorSensor sensor) {

    }


    /**
    * 行動を実行
    * エージェントが観測を行う必要があるときに呼び出されます
    */
    public override void OnActionReceived(ActionBuffers actionBuffers) {

    }


    /**
    * エージェントの操作テスト用
    * エージェントを手動で制御したいとき（テストなど）に使用します
    */
    public override void Heuristic(in ActionBuffers actionsOut) {

    }

}
