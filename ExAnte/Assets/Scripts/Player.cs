using JetBrains.Annotations;
using UnityEngine;
using System.Collections;
[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

	public float maxJumpHeight = 4;
	public float minJumpHeight = 1;
	public float timeToJumpApex = .4f;
    [SerializeField] float accelerationTimeAirborne = .2f;
    [SerializeField] float accelerationTimeGrounded = .1f;
	[SerializeField] float moveSpeed = 6;

	public int Direction = 1;
    public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;

	public float wallSlideSpeedMax = 3;
	public float wallStickTime = .25f;
    [SerializeField] float timeToWallUnstick;

	float gravity;
	float maxJumpVelocity;
	float minJumpVelocity;
	Vector3 velocity;
    float velocityXSmoothing;

	Controller2D controller;

	Vector2 directionalInput;
	bool wallSliding;
	int wallDirX;

	[Header("Camera")]
	[SerializeField] private GameObject fieldOfView;
	[SerializeField] private Transform targetOffset;
	[SerializeField] private LineRenderer cameraLineRend;
	[SerializeField] private Material cameraMat,lineMat;
	[SerializeField] private LayerMask layerMask;
	private Mesh mesh;
	private Vector3 origin;
	private float startingAngle;
	[SerializeField] private float fov = 60f, viewDist = 6f;
	private bool holdCamera = false;

    public float Fov { get => fov; set => fov = value; }
    public float ViewDist { get => viewDist; set => viewDist = value; }

    void Start()
	{
		//Movement
		controller = GetComponent<Controller2D>();

		gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        //FOV
		
		//Line
		GameObject lrObject1 = new GameObject("StartLine",typeof(LineRenderer));
		cameraLineRend = new LineRenderer();
		cameraLineRend = lrObject1.GetComponent<LineRenderer>();
		cameraLineRend.positionCount = 3;
		cameraLineRend.widthMultiplier = 0.08f;
		cameraLineRend.material = lineMat;
		cameraLineRend.sortingLayerName = "BGFX";
        //Mesh
        fieldOfView = new GameObject("FOV", typeof(MeshFilter), typeof(MeshRenderer));
        mesh = new Mesh();
        fieldOfView.GetComponent<MeshFilter>().mesh = mesh;
		MeshRenderer meshRend = fieldOfView.GetComponent<MeshRenderer>();
        meshRend.material = cameraMat;
        meshRend.sortingLayerName = "FGFX";

		origin = Vector3.zero;

    }

	void Update()
	{
		Movement();
        origin = transform.position;
		Vector3 aimDir = (targetOffset.position - transform.position).normalized;
		startingAngle = GetAngleFromVectorFloat(aimDir) + Fov / 2f;
    }
	private void LateUpdate()
	{
        SetCamera();
    }
	private void ChangeDirection()
	{
		Direction = controller.collisions.faceDir;
		transform.localScale = new Vector3(Direction, 1, 1);
	}
    private void Movement()
	{
		CalculateVelocity();
		HandleWallSliding();

		controller.Move(velocity * Time.deltaTime, directionalInput);
		if (!holdCamera)
			ChangeDirection();
		if (controller.collisions.above || controller.collisions.below)
		{
			if (controller.collisions.slidingDownMaxSlope)
			{
				velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
			}
			else
			{
				velocity.y = 0;
			}
		}
	}
    private void SetCamera()
    {
        
        
        int rayCount = 24;
        float curAngle = startingAngle;
        float angleInc = Fov / rayCount;
        

        Vector3[] vertices = new Vector3[rayCount + 1 + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = origin;

        int vertIndex = 1;
        int triIndex = 0;
        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 vertex;
            RaycastHit2D rayCastHit = Physics2D.Raycast(origin, GetVectorFromAngle(curAngle), ViewDist, layerMask);
            if (rayCastHit.collider == null)
            {
                vertex = origin + GetVectorFromAngle(curAngle) * ViewDist;
            }
            else
            {
                vertex = rayCastHit.point;
            }
            vertices[vertIndex] = vertex;
            if (i > 0)
            {
                triangles[triIndex + 0] = 0;
                triangles[triIndex + 1] = vertIndex - 1;
                triangles[triIndex + 2] = vertIndex;
                triIndex += 3;
            }
            vertIndex++;
            curAngle -= angleInc;

			cameraLineRend.SetPosition(1, origin);
			if (i==0)
			{
				cameraLineRend.SetPosition(0, origin + GetVectorFromAngle(curAngle) * ViewDist);
			}
			if (i==rayCount-2)
			{
                cameraLineRend.SetPosition(2, origin + GetVectorFromAngle(curAngle) * ViewDist);
			}
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
		mesh.bounds = new Bounds(origin, Vector3.one * 1000f);
    }

    public void SetDirectionalInput (Vector2 input) {
		directionalInput = input;
	}

	public void OnJumpInputDown() {
		if (wallSliding) {
			if (wallDirX == directionalInput.x) {
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			else if (directionalInput.x == 0) {
				velocity.x = -wallDirX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}
			else {
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if (controller.collisions.below) {
			if (controller.collisions.slidingDownMaxSlope) {
				if (directionalInput.x != -Mathf.Sign (controller.collisions.slopeNormal.x)) { // not jumping against max slope
					velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
					velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
				}
			} else {
				velocity.y = maxJumpVelocity;
			}
		}
	}

	public void OnJumpInputUp() {
		if (velocity.y > minJumpVelocity) {
			velocity.y = minJumpVelocity;
		}
	}
		

	void HandleWallSliding() {
		wallDirX = (controller.collisions.left) ? -1 : 1;
		wallSliding = false;
		if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) {
			wallSliding = true;

			if (velocity.y < -wallSlideSpeedMax) {
				velocity.y = -wallSlideSpeedMax;
			}

			if (timeToWallUnstick > 0) {
				velocityXSmoothing = 0;
				velocity.x = 0;

				if (directionalInput.x != wallDirX && directionalInput.x != 0) {
					timeToWallUnstick -= Time.deltaTime;
				}
				else {
					timeToWallUnstick = wallStickTime;
				}
			}
			else {
				timeToWallUnstick = wallStickTime;
			}

		}

	}

	void CalculateVelocity()
	{
		float targetVelocityX = directionalInput.x * moveSpeed;
		velocity.x = Mathf.SmoothDamp (velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below)?accelerationTimeGrounded:accelerationTimeAirborne);
		velocity.y += gravity * Time.deltaTime;
	}
	public Vector3 GetVectorFromAngle(float angle)
	{
		//angle = 0->360
		float angleRad = angle * (Mathf.PI / 180f);
		return new Vector3(Mathf.Cos(angleRad),Mathf.Sin(angleRad));
	}
	public float GetAngleFromVectorFloat(Vector3 dir)
	{
		dir = dir.normalized;
		float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		if (n < 0) n += 360;
		return n;
	}
	public void SetFOV(float fieldOfView) { this.Fov = fieldOfView; }
	public void HoldCamera(bool holdState)
	{
		holdCamera = holdState;
        cameraLineRend.enabled = holdCamera;
    }
	public void SetViewDist(float viewDistance) { this.ViewDist = viewDistance; }
}
