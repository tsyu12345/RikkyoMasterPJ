using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace Drone {


    public class DroneAgent : Agent {

        [Header("Operation Targets")]
        public GameObject DronePlatform; //ドローンの離着陸プラットフォーム
        public 

        [Header("Movement Parameters")]
        public float moveSpeed = 5f; // 移動速度
        public float rotSpeed = 100f; // 回転速度
        public float verticalForce = 20f; // 上昇・下降速度
        public float forwardTiltAmount = 0; // 前傾角
        public float sidewaysTiltAmount = 0; // 横傾角
        public float tiltVel = 2f; // 傾きの変化速度
        public float tiltAng = 45f; // 傾きの最大角度


    
        private Rigidbody Rbody;
        public override void Initialize() {
            Rbody = GetComponent<Rigidbody>();

        }

        public override void OnEpisodeBegin() {
            // ドローンの位置をDronePlatformの位置に初期化
            Vector3 pos = new Vector3(DronePlatform.transform.localPosition.x, DronePlatform.transform.localPosition.y + 1f, DronePlatform.transform.localPosition.z);
        }

        public override void CollectObservations(VectorSensor sensor) {}

        public override void OnActionReceived(ActionBuffers actions) {
            Operation(actions);
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            // デフォルトの入力
            float horInput = Input.GetAxis("Horizontal");
            float verInput = Input.GetAxis("Vertical");
            float upInput = Input.GetKey(KeyCode.Q) ? 1 : 0;
            float downInput = Input.GetKey(KeyCode.E) ? 1 : 0;
            float rotInput = Input.GetAxis("Mouse X");

            // 入力をエージェントのアクションに割り当てます
            ActionSegment<float> continuousAct = actionsOut.ContinuousActions;
            continuousAct[0] = horInput;
            continuousAct[1] = verInput;
            continuousAct[2] = upInput;
            continuousAct[3] = downInput;
            continuousAct[4] = rotInput;
        }


        private void Operation(ActionBuffers actions) {
            // 入力値を取得
            float horInput = actions.ContinuousActions[0];
            float verInput = actions.ContinuousActions[1];
            float upInput = actions.ContinuousActions[2];
            float downInput = actions.ContinuousActions[3];
            float rotInput = actions.ContinuousActions[4];

            // 移動方向を計算
            Vector3 moveDirection = new Vector3(horInput, 0, verInput) * moveSpeed;
            // Rigidbodyに力を加えてドローンを移動させる
            Rbody.AddForce(transform.TransformDirection(moveDirection));

            // 上昇キーが押された場合
            if (upInput > 0) {
                // 上方向に力を加える
                Rbody.AddForce(Vector3.up * verticalForce * upInput);
            }

            // 下降キーが押された場合
            if (downInput > 0) {
                // 下方向に力を加える
                Rbody.AddForce(Vector3.down * verticalForce * downInput);
            }

            // ドローンの回転処理（Y軸周り）
            transform.Rotate(0, rotInput * rotSpeed * Time.fixedDeltaTime, 0);

            // 入力に基づいて傾きを計算
            sidewaysTiltAmount = Mathf.Lerp(sidewaysTiltAmount, -horInput * tiltAng, tiltVel * Time.fixedDeltaTime);
            forwardTiltAmount = Mathf.Lerp(forwardTiltAmount, verInput * tiltAng, tiltVel * Time.fixedDeltaTime);

            // 傾きをドローンに適用
            Quaternion targetRot = Quaternion.Euler(forwardTiltAmount, 0, sidewaysTiltAmount);
            transform.localRotation = targetRot;
        }

    }

}

