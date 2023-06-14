using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CubeAgent: Agent {

    [SerializeField] private Transform target;
    private Rigidbody _rBody;
    public float moveSpeed = 10f;

    public override void Initialize() {
        _rBody = GetComponent<Rigidbody>();
    }

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
        if (Input.GetKey(KeyCode.Q)) {
            continuousActions[2] = 1;
        } else if (Input.GetKey(KeyCode.E)) {
            continuousActions[2] = -1;
        }
    }

}

