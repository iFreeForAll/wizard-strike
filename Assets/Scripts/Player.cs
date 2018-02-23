using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class ToggleEvent : UnityEvent<bool> {} // this helps us not to click checkbox on every object

public class Player : NetworkBehaviour {

	[SyncVar (hook = "OnNameChanged")] public string playerName;
	[SyncVar (hook = "OnColorChanged")] public Color playerColor;

	[SerializeField] ToggleEvent onToggleShared; 		// things for all the players
	[SerializeField] ToggleEvent onToggleLocal; 		// things for local player
	[SerializeField] ToggleEvent onToggleRemote; 		// things for remote players
	[SerializeField] float respawnTime = 4f; 			// how many seconds it takes to respawn

	static List<Player> players = new List<Player>();

	GameObject mainCamera;
	NetworkAnimator anim;

	void Start() {
		anim = GetComponent<NetworkAnimator>();
		mainCamera = Camera.main.gameObject;

		EnablePlayer();
	}

	[ServerCallback]
	void OnEnable() {
		if(!players.Contains(this)) {
			players.Add(this);
		}
	}

	[ServerCallback]
	void OnDisabled() {
		if(players.Contains(this)) {
			players.Remove(this);
		}
	}

	// use it for input+animation
	void Update() {
		// if not a local player - get out, don't read the input
		if(!isLocalPlayer) {
			return;
		}

		anim.animator.SetFloat("Speed", Input.GetAxis("Vertical"));
		anim.animator.SetFloat("Strafe", Input.GetAxis("Horizontal"));
	}

	// things when we die etc.
	void DisablePlayer() {
		onToggleShared.Invoke(false);

		if(isLocalPlayer) {
			PlayerCanvas.canvas.HideReticule();
			mainCamera.SetActive(true);
			onToggleLocal.Invoke(false);
		} else {
		 	onToggleRemote.Invoke(false);
		}
	}

	// things when the player spawns
	void EnablePlayer() {
		onToggleShared.Invoke(true);

		if(isLocalPlayer) {
			PlayerCanvas.canvas.Initialize();
			mainCamera.SetActive(false);
			onToggleLocal.Invoke(true);
		} else {
		 	onToggleRemote.Invoke(true);
		}
	}

	// method when a player dies
	public void Die() {

		if(isLocalPlayer || playerControllerId == -1) {
			anim.SetTrigger("Died");
		}

		if(isLocalPlayer) {
			PlayerCanvas.canvas.WriteGameStatusText("You're dead!");
			PlayerCanvas.canvas.PlayDeathAudio();
		}

		DisablePlayer();

		Invoke("Respawn", respawnTime);
	}

	// method to respawn a player
	void Respawn() {

		if(isLocalPlayer || playerControllerId == -1) {
			anim.SetTrigger("Restart");
		}

		if(isLocalPlayer) {
			Transform spawn = NetworkManager.singleton.GetStartPosition();
			transform.position = spawn.position;
			transform.rotation = spawn.rotation;
			}

		EnablePlayer();
	}

	void OnNameChanged (string value) {
		playerName = value;
		gameObject.name = playerName;

		GetComponentInChildren<Text>(true).text = playerName;
	}

	void OnColorChanged (Color value) {
		playerColor = value;
		GetComponentInChildren<RendererToggler>().ChangeColor(playerColor);
	}

	// tell the players who won and get 'em back to the lobby
	[Server]
	public void Won() {
		// this handles UI for all players
		for (int i = 0; i < players.Count; i++) {
			players[i].RpcGameOver(netId, name);
		}

		Invoke("BackToLobby", 7f);
	}

	[ClientRpc]
	void RpcGameOver(NetworkInstanceId networkID, string name) {
		DisablePlayer();

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		if(isLocalPlayer) {
			if(netId == networkID) {
				PlayerCanvas.canvas.WriteGameStatusText("You're the Best Wizard!");
			} else {
				PlayerCanvas.canvas.WriteGameStatusText("You Lost!\n" + name + " is the Best Wizard!");
			}
		}
	}

	void BackToLobby() {
		FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby ();
	}

}
