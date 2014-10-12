using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PathFinder : MonoBehaviour {
	public Transform target;
	public bool pathFound = false;

	public bool drawButtons = true;
	public float speed = 1.5f;

	private PathCreator.Node startNode;
	private PathCreator.Node endNode;
	private PathCreator.NodeList nodeList;
	private List<PathCreator.Node> OpenNodes = new List<PathCreator.Node> ();
	private List<PathCreator.Node> ClosedNodes = new List<PathCreator.Node> ();
	private PathCreator pathCreator;
	private PathCreator.Node memoryNode = null;
	private PathCreator.Node tempMemoryNode = null;
	public float drag = 1.0f;

	private PathCreator.Node[] pathway = new PathCreator.Node[0];
	public float refreshTime = 1.0f;
	private float cooldown = 0.0f;
	public float pathActiveRange = 0.5f;
	public float pathRelocateRange = 1.0f;
	public float shakeSpeed = 0.05f;
	public float shakePower = 5.0f;
	public bool print = false;
	public PathState state = PathState.WAITING;

	public enum PathState {
		WAITING,
		MOVING,
		STUCK,
		SHAKING
	}

	void Awake() {
		pathCreator = GameObject.FindWithTag ("PathCreator").GetComponent<PathCreator> ();
	}

	// Use this for initialization
	void Start () {
		nodeList = pathCreator.GetNodeList (transform, target);
		startNode = nodeList.GetStartNode ();
		endNode = nodeList.GetEndNode ();
	}
	
	// Update is called once per frame
	void Update () {
		if (print) {
			Debug.Log (pathway[0].GetLocation());
			Debug.Log ("MEM " + memoryNode.GetLocation());
		}
		if (!pathFound && (Input.GetMouseButton(1) || true)) {
			GetPath ();
		}
	}

	void FixedUpdate() {
		if (pathFound) {
			ApplyMotion();
		}
	}

	void ApplyMotion() {
		int layer = 1 << 2;
		PathCreator.Node closest;

		int i = 0;
		bool nextNode = false;
		rigidbody2D.drag = drag;
		if (pathway.Length > 0) {
			for (i = 0; i < pathway.Length; i++) {
				if ((transform.position - (Vector3) pathway[i].GetLocation()).magnitude < pathActiveRange) {
					nextNode = true;
					break;
				} 
			}
			closest = GetClosestPathwayNode();
			float nodeDist = ((Vector3)closest.GetLocation () - transform.position).magnitude;

			if (!nextNode) {
				/*
				if (tempMemoryNode != null) {
					float memDist = (tempMemoryNode.GetLocation() - pathway[0].GetLocation()).magnitude;
					if (((Vector3) pathway[0].GetLocation() - transform.position).magnitude > memDist) {

					}
				}*/
				RaycastHit2D hit = Physics2D.Raycast(transform.position, ((Vector3) closest.GetLocation() - transform.position));
				if (hit != null) {
					if (pathway[0] != closest) {
						if (((Vector3) hit.point - transform.position).magnitude > nodeDist) {
							for (int z = 0; z < pathway.Length; z++) {
								if (pathway[z] == closest) {
									i = z;
									nextNode = true;
									break;
								}
							}
						}
					} else {
						if (((Vector3) hit.point - transform.position).magnitude < nodeDist) {
							if (memoryNode == null) {
								memoryNode = nodeList.GetClosestNode(transform);
							} else {
								if (((Vector3) memoryNode.GetLocation() - transform.position).magnitude > pathActiveRange * 2) {
									PathCreator.Node[] array1 = new PathCreator.Node[] {memoryNode};
									PathCreator.Node[] array2 = pathway;
									pathway = new PathCreator.Node[pathway.Length + 1];
									array1.CopyTo(pathway,0);
									array2.CopyTo(pathway,1);
									memoryNode = null;
								}
							}
						}
					}
				}
			}

			if (nextNode) {
				if (memoryNode != null) {
					memoryNode = null;
				}
				//tempMemoryNode = pathway[0];
				for (int x = i; x < (pathway.Length - 1); x++) {
					pathway[x - i] = pathway[x + 1];
				}
				Array.Resize(ref pathway, pathway.Length - i -1);
			}

			if (pathway.Length > 0 && speed > 0) {
				Vector3 direction = Vector3.zero;
				if (rigidbody2D.velocity.magnitude == 0 && state != PathState.WAITING) {
					state = PathState.STUCK;
				} else if (rigidbody2D.velocity.magnitude < shakeSpeed && state != PathState.WAITING) {
					direction = Quaternion.AngleAxis(UnityEngine.Random.Range(-180, 180), Vector3.up) * 
					          (-(Vector3) pathway[0].GetLocation() - transform.position).normalized * 
								shakePower;

					state = PathState.SHAKING;
				} else {
					state = PathState.MOVING;
					direction = ((Vector3) pathway[0].GetLocation() - transform.position).normalized * speed;
				}
				rigidbody2D.AddForce(direction);
			}
		} 
	}

	void OnCollisionStay2D (Collision2D collision) {
		if (state == PathState.STUCK) {
			List<Vector2> points = new List<Vector2> ();

			foreach (ContactPoint2D x in collision.contacts) {
				if ((x.collider.transform == transform || x.otherCollider.transform == transform) &&
				    (x.collider.tag == "Wall" || x.otherCollider.tag == "Wall")) {
					points.Add(x.point);
				}
			}
			Vector2 dir = Vector2.zero;
			if (points.Count == 2) {
				Vector2 v = points[0] - points[1];
				Vector2 v2 = (Vector2) transform.position - points[0];
				dir = (Vector2) Vector3.Cross((Vector3) v, new Vector3(0,0,1));

				if (Vector2.Dot(dir, v2) < 0) {
					dir *= -1;
				}
			} else if (points.Count == 1) {
				dir = ((Vector2) transform.position - points[0]).normalized;

			} 
			rigidbody2D.AddForce(dir.normalized * shakePower);
		}
	}

	PathCreator.Node GetClosestPathwayNode() {
		PathCreator.Node closest = null;
		foreach (PathCreator.Node node in pathway) {
			if (closest == null) {
				closest  = node;
			} else {
				if ((transform.position - (Vector3) node.GetLocation()).magnitude <
				    (transform.position - (Vector3) closest.GetLocation()).magnitude) {
					closest = node;
				}
			}
		}
		return closest;
	}

	void GetPath() {
		if (!pathFound) {
			//pathFound = true;
			if (OpenNodes.Count == 0) {
				if (ClosedNodes.Count == 0) {
					GetChildren(startNode);
				} else {
					pathFound = true;
				}
			}
			if (ClosedNodes.Count == 0) {
				ClosedNodes.Add(startNode);
			}
			PathCreator.Node smallest = null;

			foreach (PathCreator.Node n in OpenNodes) {
				if (smallest != endNode) {
					if (smallest == null) {
						smallest = n;
					} else {
						if (n.F < smallest.F) {
							smallest = n;
						}
					}
				}

			}
			if (smallest != null && !pathFound) {
				OpenNodes.Remove(smallest);
				ClosedNodes.Add(smallest);
				GetChildren(smallest);
			}
			if (OpenNodes.Contains(endNode)) {
				pathFound = true;
				CreatePathway(endNode);
			}
		}
	}

	void OnGUI() {
		if (drawButtons) {
			Vector2 pos;
			Vector3 sizea = nodeList.GetSize ();
			Vector3 sizeb = Camera.main.WorldToScreenPoint(sizea);
			Vector3 center = Camera.main.WorldToScreenPoint(Vector3.zero);
			sizeb = sizeb - center;

			GUI.backgroundColor = Color.green;
			List<PathCreator.Node> printedPath = new List<PathCreator.Node> ();
			foreach (PathCreator.Node n in OpenNodes) {
				if (Array.IndexOf(pathway, n) != -1) {
					GUI.backgroundColor = Color.grey;
					printedPath.Add (n);
				}
				if (n == endNode) GUI.backgroundColor = Color.yellow;
				pos = Camera.main.WorldToScreenPoint(new Vector3(n.GetLocation().x - sizea.x/2, -n.GetLocation().y - sizea.y/2));
				if (GUI.Button (new Rect (pos.x, pos.y, sizeb.x, sizeb.y), "")) {
					Debug.Log ("F: " + n.F + " H:" + n.H + " G:" + n.G + " X:" + n.GetNodeList().GetNodePos(n).x +
					           " Y:" + n.GetNodeList().GetNodePos(n).y);
				}
				if (n == endNode || Array.IndexOf(pathway, n) != -1) GUI.backgroundColor = Color.green;
			}

			GUI.backgroundColor = Color.red;
			foreach (PathCreator.Node n in ClosedNodes) {
				if (Array.IndexOf(pathway, n) != -1) {
					GUI.backgroundColor = Color.grey;
					printedPath.Add(n);
				}
				if (n == startNode) GUI.backgroundColor = Color.blue;
				pos = Camera.main.WorldToScreenPoint(new Vector3(n.GetLocation().x - sizea.x/2, -n.GetLocation().y - sizea.y/2));
				//Debug.Log (new Vector3(n.GetLocation().x - sizea.x/2, n.GetLocation().y + sizea.y/2) + " " + pos);
				if (GUI.Button (new Rect (pos.x, pos.y, sizeb.x, sizeb.y), "")) {
					Debug.Log ("F: " + n.F + " H:" + n.H + " G:" + n.G + " X:" + n.GetNodeList().GetNodePos(n).x +
					           " Y:" + n.GetNodeList().GetNodePos(n).y);
				}
				if (n == startNode || Array.IndexOf(pathway, n) != -1) GUI.backgroundColor = Color.red;
			}

			foreach (PathCreator.Node n in pathway) {
				if (!printedPath.Contains(n)) {
					GUI.backgroundColor = Color.grey;
					pos = Camera.main.WorldToScreenPoint(new Vector3(n.GetLocation().x - sizea.x/2, -n.GetLocation().y - sizea.y/2));
					//Debug.Log (new Vector3(n.GetLocation().x - sizea.x/2, n.GetLocation().y + sizea.y/2) + " " + pos);
					if (GUI.Button (new Rect (pos.x, pos.y, sizeb.x, sizeb.y), "")) {
						Debug.Log ("F: " + n.F + " H:" + n.H + " G:" + n.G + " X:" + n.GetNodeList().GetNodePos(n).x +
						           " Y:" + n.GetNodeList().GetNodePos(n).y);
					}
				}
			}
		}
	}

	void GetChildren(PathCreator.Node rent) {
		PathCreator.Node[] children = nodeList.GetChildren (rent);
		if (children == null) Debug.Log ("ERROR");
		else
		foreach (PathCreator.Node n in children) {
			OpenNodes.Add(n);
		}
	}

	void CreatePathway(PathCreator.Node node) {
		PathCreator.Node prevNode, prevPrevNode;
		bool cont = true;
		while(cont) {
			Array.Resize(ref pathway, pathway.Length + 1);
			pathway[pathway.Length - 1] = node;
			prevNode = node;
			if (node.HasParent()) {
				node = node.GetParent();
			} else {
				cont =false;
			}
		}
		Array.Reverse (pathway);
		if (node.GetNodeList().IsDiagonal()) {
			OptimizePathway ();
		}
	}

	void OptimizePathway() {
		int xp, yp, curp;
		List<int> optimized = new List<int> ();
		List<int> keys = new List<int> ();
		bool cont = true;
		bool isWall = false;

		PathCreator.Node nodeI, nodeX;
		Vector2 iPos, xPos, newPos;
		for (int i = 0; i < pathway.Length; i++) {
			if (!optimized.Contains(i)) {
				nodeI = pathway[i];
				iPos = nodeI.GetNodeList().GetNodePos(nodeI);
				cont = true;
				isWall = false;
				for (int x = i + 1; x < pathway.Length; x++) {
					if (cont) {
						keys.Clear();
						nodeX = pathway[x];
						xPos = nodeI.GetNodeList().GetNodePos(nodeX);
						/*
						 if (((x - i) == 1) && (xPos.x == iPos.x || xPos.y == iPos.y)) {
							cont = false;
						} else */
						if (xPos.x == iPos.x || xPos.y == iPos.y) {
							for (int z = i + 1; z < x; z++) {
								keys.Add(z);
							}
							
							if (ContainsWallInBetween(nodeI, nodeX)) {
								isWall = true;
								cont = false;
							}
							if (!isWall) {
								curp = 0;
								foreach (int k in keys) {
									curp++;
									if (xPos.x == iPos.x) {
										yp = (int) ((iPos.y < xPos.y)? curp + iPos.y: iPos.y - curp);
										newPos = new Vector2(iPos.x, yp);
									} else {
										xp = (int) ((iPos.x < xPos.x)? curp + iPos.x: iPos.x - curp);
										newPos = new Vector2(xp, iPos.y);
									}
									pathway[k] = nodeI.GetNodeList().GetNodeAtPos(newPos);
									optimized.Add(k);
								}
							}
						}
					}
				}
			}
		}
	}

	bool ContainsWallInBetween(PathCreator.Node node1, PathCreator.Node node2) {
		Vector2 pos1 = node1.GetNodeList ().GetNodePos (node1);
		Vector2 pos2 = node1.GetNodeList ().GetNodePos (node2);
		bool wall = false;
		Vector2 diff = pos1 - pos2;
		Vector2 newPos;
		for (int z = 1; z <= Math.Abs(diff.x + diff.y); z++) {
			if (diff.x == 0) {
				newPos = new Vector2(pos1.x, ((pos1.y < pos2.y)? pos1.y + z: pos1.y - z));
			} else {
				newPos = new Vector2(((pos1.x < pos2.x)? pos1.x + z: pos1.x - z), pos1.y);
			}
			if (node1.GetNodeList().GetNodeAtPos(newPos).IsBoundary()) return true;
		}

		return wall;
	}
}
