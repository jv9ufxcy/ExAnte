using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Player))]
public class PlayerInput : MonoBehaviour {

	Player player;
	//input str
	[SerializeField] private string horiInput= "Horizontal", vertInput= "Vertical", cameraInput, barkInput, biteInput;
	public int horizontalAxis, verticalAxis;
	//camera
	private float defaultFOV = 60f;
    [SerializeField] private float shrinkMultiplier = 18f;
	private float camCooldown, camCooldownMax = 1f;

	void Start () {
		player = GetComponent<Player> ();
	}

	void Update ()
	{
		MovementInput();
		if (camCooldown>0)
		{
			camCooldown -= Time.deltaTime;
		}
		else
			CameraCharge();
	}
	private void CameraCharge()
	{
		if (Input.GetButtonDown(cameraInput))
		{
			player.SetFOV(defaultFOV);
			player.HoldCamera(true);
		}
		if (Input.GetButton(cameraInput))//charge photo
		{
			if (player.Fov>0)
			{
				float cameravalue = player.Fov;
				cameravalue -= Time.deltaTime * shrinkMultiplier;
				player.SetFOV(cameravalue);

			}
			
		}
        if (Input.GetButtonUp(cameraInput))//take photo
		{
            player.SetFOV(0);
            player.HoldCamera(false);
			camCooldown = camCooldownMax;
        }

    }
	private void MovementInput()
	{
		Vector2 directionalInput = new Vector2(Input.GetAxisRaw(horiInput), Input.GetAxisRaw(vertInput));
		player.SetDirectionalInput(directionalInput);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			player.OnJumpInputDown();
		}
		if (Input.GetKeyUp(KeyCode.Space))
		{
			player.OnJumpInputUp();
		}
	}
}
