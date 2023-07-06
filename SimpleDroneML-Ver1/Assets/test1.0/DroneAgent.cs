using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class DroneAgent : Agent {
    // 各種パラメータ
    public float moveSpeed = 5f;
    public float rotSpeed = 100f;
    public float verticalForce = 20f;
    public float forwardTiltAmount = 0;
    public float sidewaysTiltAmount = 0;
    public float tiltVel = 2f;

    public Transform target;
    public float goalThreshold;
    private bool isReachTarget = false;

    //フィールドの床
    public GameObject plane;
    public float limitAltitude = 30f;

    public Transform startPlace;

    private Rigidbody playerRb;
    private float tiltAng = 45f;
    private int cptCount;
    private float lowAltitudeTime = 0f;
    private float lowAltitudeThreshold = 1f; // ドローンがこの高さ以下にいるときに計測を開始
    private float timeThreshold = 5f; // この時間以上同じ高さにいるとペナルティを与える

    public override void Initialize() {
        playerRb = GetComponent<Rigidbody>();
        if(target == null) {
            throw new System.ArgumentNullException("target", "Arguments 'Target' is required");
        }
        if(plane == null) {
            throw new System.ArgumentNullException("plane", "Arguments 'Plane' is required");
        }
        if(startPlace == null) {
            throw new System.ArgumentNullException("startPlace", "Arguments 'StartPlace' is required");
        }
    }

    public override void OnEpisodeBegin() {
        Debug.Log("OnEpisodeBegin() called");
        // 初期化処理
        cptCount = 0;
        //startPlaceの場所へ位置を初期化（y = 5は固定）
        transform.localPosition = new Vector3(startPlace.localPosition.x, 20.0f, startPlace.localPosition.z);
        transform.rotation = Quaternion.identity;
        playerRb.velocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;

        // エピソード開始時にドローンに初期推進力を与える
        playerRb.AddForce(transform.TransformDirection(new Vector3(0, 200.0f, 0.0f)));

        //targetの位置をランダムに変更
        if(isReachTarget) {
            target.localPosition = new Vector3(Random.Range(-2f, 2f), Random.Range(1.0f, 2.0f), Random.Range(-2f, 2f));
        } else {
            //targetの位置を変更しない
            target.localPosition = target.localPosition;
        }
        
    }


    /**TODO : 今はCheckPoint通過による報酬付与になっているのでTarget接触時に直す。
    */

    /**このメソッドは、Agentがオブジェクトに接触したときに呼ばれるunityのコールバック関数**/
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("obstacle")) { // タグが"Wall"のオブジェクトに衝突した場合
            // ペナルティ報酬を追加し、エピソードを終了する
            AddReward(-1.0f);
            EndEpisode();
        } else if(other.CompareTag("target")) { // タグが"target"のオブジェクトに衝突した場合
            AddReward(1.0f);
            isReachTarget = true;
            EndEpisode();
        }
    }


    /// <summary>
    /// エージェントの観測を定義するメソッド
    /// 今回は、ドローンの速度と回転,位置（サイズ９）とターゲットの位置（サイズ3）の計１２サイズを観測する
    /// </summary>
    public override void CollectObservations(VectorSensor sensor) {
        // ドローンの速度を観察
        sensor.AddObservation(playerRb.velocity);
        // ドローンの回転を観察
        sensor.AddObservation(transform.rotation.eulerAngles);
        //ドローンの位置を観察
        sensor.AddObservation(transform.localPosition);

        // ターゲットの位置を観察(x,y,z)
        sensor.AddObservation(target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        //入力に基づくドローンの制御を行う
        Operation(actions);

        // ターゲットとの距離を計算
        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        if(distanceToTarget < goalThreshold) {
            // ゴール報酬を追加し、エピソードを終了する
            AddReward(1.0f);
            this.isReachTarget = true;
            EndEpisode();
            Debug.Log("close target");

        } else {
            AddReward(-1.0f);
            this.isReachTarget = false;
        }
        
        //異なる高度への移動を促すため、高度が変わらない場合はペナルティを与える
            // ドローンの高さが一定値以下であるかチェック
        if (transform.localPosition.y <= lowAltitudeThreshold) {
            // 一定の高さ以下であれば、時間を計測
            lowAltitudeTime += Time.fixedDeltaTime;
        } else {
            // 一定の高さ以上であれば、時間をリセット
            lowAltitudeTime = 0f;
        }

        if(isOutRange(limitAltitude)) {
            // ペナルティ報酬を追加し、エピソードを終了する
            //AddReward(-1.0f);
            EndEpisode();
        }
    }

    /// <summary>
    /// ドローンがフィールド（Plane）外に出たかどうかを判定するメソッド
    /// </summary>
    /// <param name="yMax">フィールドの高さ(限界高度)</param>
    /// <returns>フィールド外に出た場合はtrue、そうでない場合はfalseを返す</returns>
    private bool isOutRange(float yMax) {
        
        // PlaneオブジェクトのTransformコンポーネントを取得
        Transform planeTransform = plane.GetComponent<Transform>();

        // Planeオブジェクトのローカルスケールを取得
        Vector3 planeLocalScale = planeTransform.localScale;

        // Planeオブジェクトの中心のローカル座標を取得
        Vector3 planeCenterLocalPosition = planeTransform.localPosition;

        // Planeオブジェクトの4辺のローカル座標を計算
        Vector3 leftEdgeLocalPosition = planeCenterLocalPosition + new Vector3(-planeLocalScale.x / 2, 0, 0);
        Vector3 rightEdgeLocalPosition = planeCenterLocalPosition + new Vector3(planeLocalScale.x / 2, 0, 0);
        Vector3 topEdgeLocalPosition = planeCenterLocalPosition + new Vector3(0, 0, planeLocalScale.z / 2);
        Vector3 bottomEdgeLocalPosition = planeCenterLocalPosition + new Vector3(0, 0, -planeLocalScale.z / 2);

        //ドローンの位置がフィールドの範囲外かどうかを判定
        var isInXRange = transform.localPosition.x < rightEdgeLocalPosition.x && transform.localPosition.x > leftEdgeLocalPosition.x;
        var isInYRange = transform.localPosition.y < yMax && transform.localPosition.y > 0;
        var isInZRange = transform.localPosition.z < topEdgeLocalPosition.z && transform.localPosition.z > bottomEdgeLocalPosition.z;

        if(!isInYRange || !isInXRange || !isInZRange) {
            Debug.Log("Out of range");
            return true;
        } else {
            return false;
        }
    }


    /// <summary>
    /// ドローンの制御系をまとめたラッパメソッド
    /// </summary>
    /// <param name="actions">Agent.OnActionReceived()の第１引数をそのまま代入</param>
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
