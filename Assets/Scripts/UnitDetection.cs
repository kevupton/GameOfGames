using UnityEngine;
using System.Collections;

public class UnitDetection : MonoBehaviour {
	public float range = 5.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Collider2D[] enemies = Physics2D.OverlapCircleAll (transform.position, range);
		Vector2 direction;
		foreach (Collider2D enemy in enemies) {
			if (enemy.tag == "Enemy") {
				direction = enemy.transform.position - transform.position;
				RaycastHit2D hit = Physics2D.Raycast(transform.position, direction);
				if (hit.transform.tag == "Enemy") {
					transform.GetComponent<PathFinder>().speed = 0;
					//do other stuff
					break;
				}
			}
		}
	}

}
