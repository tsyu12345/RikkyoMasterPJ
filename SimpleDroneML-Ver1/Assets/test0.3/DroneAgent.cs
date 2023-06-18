using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DroneAgent3 : Agent {
    // 各種パラメータ
    public float moveSpeed = 5f;
    public float rotSpeed = 100f;
    public float verticalForce = 20f;
    public float forwardTiltAmount = 0;
    public float sidewaysTiltAmount = 0;
    public float tiltVel = 2f;

    private Rigidbody playerRb;
    private float tiltAng = 45f;
    private int cptCount;

    public override void Initialize() {
        playerRb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin() {
        // 初期化処理
        cptCount = 0;
        //所属するフィールド内のx=0, y=5, z=0の位置にドローンを移動させる
        transform.localPosition = new Vector3(0, 5, 0);
        transform.rotation = Quaternion.identity;
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        // エピソード開始時にドローンに初期推進力を与える
        playerRb.AddForce(transform.TransformDirection(new Vector3(0, 200.0f, 200.0f)));
    }

    private void OnTriggerEnter(Collider other) {
        // タグが"CheckPoint"のオブジェクトに衝突した場合
        if (other.CompareTag("CheckPoint")) {
            // 報酬を追加し、チェックポイントカウントを増やす
            AddReward(1.0f);
            cptCount++;
            // すべてのチェックポイントを通過した場合
            if (cptCount >= 4) {
                // ゴール報酬を追加し、エピソードを終了する
                AddReward(10.0f);
                EndEpisode();
            }
        } else if (other.CompareTag("Wall")) { // タグが"Wall"のオブジェクトに衝突した場合
            // ペナルティ報酬を追加し、エピソードを終了する
            AddReward(-5.0f);
            EndEpisode();
        }
    }

    public override void CollectObservations(VectorSensor sensor) {
        // ドローンの速度を観察
        sensor.AddObservation(playerRb.velocity);
        // ドローンの回転を観察
        sensor.AddObservation(transform.rotation.eulerAngles);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // 入力値を取得
        float horInput = actions.ContinuousActions[0];
        float verInput = actions.ContinuousActions[1];
        float upInput = actions.ContinuousActions[2];
        float downInput = actions.ContinuousActions[3];
        float rotInput = actions.ContinuousActions[4];

        // 移動方向を計算
        Vector3 moveDirection = new Vector3(horInput, 0, verInput) * moveSpeed;
        // Rigidbodyに力を加えてドローンを移動させる
        playerRb.AddForce(transform.TransformDirection(moveDirection));

        // 上昇キーが押された場合
        if (upInput > 0) {
            // 上方向に力を加える
            playerRb.AddForce(Vector3.up * verticalForce * upInput);
        }

        // 下降キーが押された場合
        if (downInput > 0) {
            // 下方向に力を加える
            playerRb.AddForce(Vector3.down * verticalForce * downInput);
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
}
