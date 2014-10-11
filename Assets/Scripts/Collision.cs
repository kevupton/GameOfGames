using UnityEngine;
using System.Collections;

public class Collision : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnCollisionEnter2D(Collision2D e) {
		Debug.Log (e.relativeVelocity);
	}

	void OnTriggerEnter2D(Collider2D obj) {
		//Debug.Log (obj.rigidbody2D.velocity);
	}

}
