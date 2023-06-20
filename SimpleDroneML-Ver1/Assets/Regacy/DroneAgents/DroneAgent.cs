using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DroneAgent_Regacy: Agent {

    [SerializeField] private Transform target;
    public Transform startZone;

    private Rigidbody _rBody;
    public float moveSpeed = 2f;
    public bool powerOn = false;
    

    public override void Initialize() {
        _rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin() {
        if (this.transform.localPosition.y < 0) {
            // If the Agent fell, zero its momentum
            _rBody.angularVelocity = Vector3.zero;
            _rBody.velocity = Vector3.zero;
            
            //startZoneの真上にセット
            transform.localPosition = new Vector3(startZone.localPosition.x, startZone.localPosition.y + 5, startZone.localPosition.z);
        }

        // Move the target to a new spot// Targetの位置のリセット
        //ステージの範囲内でランダムにTargetの位置を決定
        target.localPosition = new Vector3(Random.Range(-8f, 8f), Random.Range(0f, 30f), Random.Range(-8f, 8f));

    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(_rBody.velocity.x);
        sensor.AddObservation(_rBody.velocity.z);
        sensor.AddObservation(_rBody.velocity.y);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actions.ContinuousActions[0];
        controlSignal.z = actions.ContinuousActions[1];
        controlSignal.y = actions.ContinuousActions[2];
        //電源ON時はその高度を保持,rigidbodyのy軸方向をFreeze
        if (powerOn) {
            _rBody.constraints = RigidbodyConstraints.FreezePositionY;
        } else {
            _rBody.constraints = RigidbodyConstraints.None;
        }

        //_rBody.AddForce(controlSignal * 10);
        transform.position += controlSignal * moveSpeed * Time.deltaTime;

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

    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
        continuousActions[2] = 0;
        //Pキー押下で電源ON
        if (Input.GetKey(KeyCode.P) && !powerOn) {
            powerOn = true;
        } else if (Input.GetKey(KeyCode.P) && powerOn) { //Pキー押下で電源OFF
            powerOn = false;
        }

        //電源オンでかつ、スペースキー押下で上昇,上昇後の高度を保持
        if (powerOn && Input.GetKey(KeyCode.Space)) {
            continuousActions[2] = 1;
        } else if(powerOn && Input.GetKey(KeyCode.E)) { //電源オンでかつ、Eキー押下で下降
            continuousActions[2] = -1;
        }
    }

}

