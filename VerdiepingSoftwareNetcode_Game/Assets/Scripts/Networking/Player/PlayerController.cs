using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
	[Header("Movement")]
	[SerializeField] private float walkSpeed = 4f;
	[SerializeField] private float runSpeed = 7f;
	[SerializeField] private float crouchSpeed = 2f;
	[SerializeField] private float rotateSpeed = 360f;
	[SerializeField] private float jumpHeight = 2f;

	[Header("Crouch")]
	[SerializeField] private float crouchHeight = 1f;
	[SerializeField] private float crouchSmoothSpeed = 8f;
	[SerializeField] private LayerMask ceilingMask;

	[Header("Mouse Look")]
	public float mouseSensitivity = 100f;
	public Transform playerCamera;
	public float maxLookAngle = 80f;

	[Header("Animator")]
	[SerializeField] private string horizontalID = "Hor";
	[SerializeField] private string verticalID = "Vert";
	[SerializeField] private string stateID = "State";
	[SerializeField] private string jumpID = "IsJump";
	[SerializeField] private string crouchID = "IsCrouch";

	private CharacterController controller;
	public Animator animator;

	[Header("Headbob")]
	[SerializeField] private bool enableHeadbob = true;
	[SerializeField] private float walkBobSpeed = 8f;
	[SerializeField] private float walkBobAmount = 0.05f;
	[SerializeField] private float runBobSpeed = 12f;
	[SerializeField] private float runBobAmount = 0.09f;
	[SerializeField] private float crouchBobSpeed = 4f;
	[SerializeField] private float crouchBobAmount = 0.03f;
	[SerializeField] private float bobSmooth = 10f;

	private NetworkVariable<float> netPitch = new NetworkVariable<float>(
	0f,
	NetworkVariableReadPermission.Everyone,
	NetworkVariableWritePermission.Owner
);

	private float bobTimer;
	private Vector3 baseCameraPos;
	private Vector3 headbobOffset;

	private Vector3 gravityVelocity;
	private Vector3 surfaceNormal = Vector3.up;

	private float xRotation;
	private float jumpCooldown = 0.2f;
	private float jumpTimer;

	private bool isGrounded;
	private bool isCrouching;

	private float originalHeight;

	[SerializeField] private float _standUpCameraHeight = 1.8f;
	[SerializeField] private float _crouchCameraHeight = 1.2f;

	public static bool CanMove = true;

	public override void OnNetworkSpawn()
	{
		controller = GetComponent<CharacterController>();
		originalHeight = controller.height;
		controller.center = new Vector3(controller.center.x, originalHeight / 2f, controller.center.z);
		playerCamera.localPosition = new Vector3(0, _standUpCameraHeight, 0.4f);

		baseCameraPos = playerCamera.localPosition;

		if (!IsOwner)
		{
			if (playerCamera) playerCamera.gameObject.SetActive(false);
			enabled = false;
			return;
		}

		xRotation = 0f;
		playerCamera.localRotation = Quaternion.identity;
	}

	void Update()
	{
		if (IsOwner && CanMove)
		{
			HandleMouseLook();
			HandleMovement();
		}
	}

	void HandleMouseLook()
	{
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

		playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
		netPitch.Value = xRotation;
		transform.Rotate(Vector3.up * mouseX);
	}

	void HandleMovement()
	{
		float deltaTime = Time.deltaTime;

		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		bool isRun = Input.GetKey(KeyCode.LeftShift);
		bool isJump = Input.GetButton("Jump");
		bool crouchInput = Input.GetKey(KeyCode.LeftControl);

		HandleCrouch(crouchInput);

		Vector2 axis = new Vector2(x, z);
		axis = Vector2.ClampMagnitude(axis, 1f);

		Vector3 forward = transform.forward;
		Vector3 right = transform.right;

		Vector3 movement = axis.x * right + axis.y * forward;
		movement = Vector3.ProjectOnPlane(movement, surfaceNormal);

		float currentSpeed = walkSpeed;

		if (isCrouching)
			currentSpeed = crouchSpeed;
		else if (isRun)
			currentSpeed = runSpeed;

		if (controller.isGrounded)
		{
			isGrounded = true;
			gravityVelocity = Physics.gravity;

			if (isJump && jumpTimer <= 0f && !isCrouching)
			{
				float gravity = Mathf.Abs(Physics.gravity.y);
				gravityVelocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
				jumpTimer = jumpCooldown;
			}
		}
		else
		{
			isGrounded = false;
			gravityVelocity += Physics.gravity * deltaTime;
		}

		jumpTimer -= deltaTime;

		Vector3 displacement =
			movement * currentSpeed +
			gravityVelocity;

		controller.Move(displacement * deltaTime);

		HandleHeadbob(axis, isRun);

		UpdateAnimation(axis, isRun, !isGrounded);
	}

	void HandleCrouch(bool crouchInput)
	{
		if (crouchInput)
		{
			isCrouching = true;
		}
		else
		{
			if (!Physics.Raycast(transform.position, Vector3.up, originalHeight, ceilingMask))
			{
				isCrouching = false;
			}
		}

		float targetHeight = isCrouching ? crouchHeight : originalHeight;

		controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchSmoothSpeed);

		if (animator != null && animator.GetBool(crouchID) != isCrouching) CrouchBehaviour();
	}

	void CrouchBehaviour()
	{
		controller.center = isCrouching ? new Vector3(controller.center.x, crouchHeight / 2f, controller.center.z) 
			: new Vector3(controller.center.x, originalHeight / 2f, controller.center.z);

		baseCameraPos = isCrouching ? new Vector3(0, _crouchCameraHeight, 0.6f)
			: new Vector3(0, _standUpCameraHeight, 0.4f);

		if (animator != null) animator.SetBool(crouchID, isCrouching);
	}

	void UpdateAnimation(Vector2 axis, bool isRun, bool isAir)
	{
		if (animator != null)
		{
			animator.SetFloat(horizontalID, axis.x);
			animator.SetFloat(verticalID, axis.y);
		}


		float stateValue = axis.magnitude;
		if (isRun && !isCrouching)
			stateValue = 1f;
		else stateValue = 0f;

		if (animator != null)
		{
			animator.SetFloat(stateID, stateValue);
			animator.SetBool(jumpID, isAir);
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (hit.normal.y > controller.stepOffset)
			surfaceNormal = hit.normal;
	}

	void HandleHeadbob(Vector2 axis, bool isRun)
	{
		if (!enableHeadbob)
		{
			playerCamera.localPosition = baseCameraPos;
			return;
		}

		if (!isGrounded || axis.magnitude < 0.1f)
		{
			bobTimer = 0f;
			headbobOffset = Vector3.Lerp(headbobOffset, Vector3.zero, Time.deltaTime * bobSmooth);
		}
		else
		{
			float speed = walkBobSpeed;
			float amount = walkBobAmount;

			if (isCrouching)
			{
				speed = crouchBobSpeed;
				amount = crouchBobAmount;
			}
			else if (isRun)
			{
				speed = runBobSpeed;
				amount = runBobAmount;
			}

			bobTimer += Time.deltaTime * speed;

			float y = Mathf.Sin(bobTimer) * amount;
			float x = Mathf.Cos(bobTimer * 0.5f) * amount * 0.5f;

			headbobOffset = new Vector3(x, y, 0f);
		}

		playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, baseCameraPos + headbobOffset, Time.deltaTime * bobSmooth);
	}
}