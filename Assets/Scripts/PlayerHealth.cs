using UnityEngine;
using UnityEngine.Networking;

public class PlayerHealth : NetworkBehaviour {

	[SerializeField] int maxHealth = 100;

	[SyncVar (hook = "OnHealthChanged")] int health;

	Player player;

	void Awake() {
		player = GetComponent<Player>();
	}
		
	[ServerCallback]	// always runs on a server and nowhere else
	void OnEnable() {
		health = maxHealth;
	}

	[ServerCallback]
	void Start() {
		health = maxHealth;
	}

	[Server]	// only a server is allowed to handle damaging
	public bool TakeDamage() {
		bool died = false;

		// check if already dead
		if (health <= 0) {
			return died;
		}

		health = health - 20;
		died = health <= 0;

		RpcTakeDamage (died);

		return died;
	}

	[ClientRpc]
	void RpcTakeDamage (bool died) {

		if(isLocalPlayer) {
			PlayerCanvas.canvas.FlashDamageEffect();
		}

		if(died) {
			player.Die();
		}
	}

	void OnHealthChanged (int value) {
		health = value;
		if(isLocalPlayer) {
			PlayerCanvas.canvas.SetHealth(health);
		}
	}
}
