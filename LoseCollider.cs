using UnityEngine;
using System.Collections;

public class LoseCollider : MonoBehaviour {

private LevelManager levelManager;

	void Start(){
		levelManager = FindObjectOfType<LevelManager>();
		if(levelManager == null){
			GameObject levelManagerInstance = Instantiate(Resources.Load("LevelManager")) as GameObject;
			levelManager = FindObjectOfType<LevelManager>();
		}
	}

	void OnTriggerEnter2D(Collider2D collider) {

		Attacker attacker = collider.gameObject.GetComponent<Attacker>();

       if(attacker){
			  levelManager.LoadLevel("03b Lose");
       }
    }
}
