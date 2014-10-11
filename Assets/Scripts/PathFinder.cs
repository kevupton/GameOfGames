using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PathFinder : MonoBehaviour {
	public Transform target;
	public bool pathFound = false;

	public bool drawButtons = true;

	private PathCreator.Node startNode;
	private PathCreator.Node endNode;
	private PathCreator.NodeList nodeList;
	private List<PathCreator.Node> OpenNodes = new List<PathCreator.Node> ();
	private List<PathCreator.Node> ClosedNodes = new List<PathCreator.Node> ();
	private PathCreator pathCreator;

	private PathCreator.Node[] pathway = new PathCreator.Node[0];

	public float refreshTime = 1.0f;
	private float cooldown = 0.0f;

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
		if (!pathFound && Input.GetMouseButton(1)) {
			GetPath ();
		}
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
				if (smallest == endNode) {
					pathFound = true;
					CreatePathway(smallest);
				} else {
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
			foreach (PathCreator.Node n in OpenNodes) {
				if (n == endNode) GUI.backgroundColor = Color.yellow;
				pos = Camera.main.WorldToScreenPoint(new Vector3(n.GetLocation().x - sizea.x/2, -n.GetLocation().y - sizea.y/2));
				if (GUI.Button (new Rect (pos.x, pos.y, sizeb.x, sizeb.y), "")) {
					Debug.Log (n.F + " " + n.H + " " + n.G);
				}
				if (n == endNode) GUI.backgroundColor = Color.green;
			}

			GUI.backgroundColor = Color.red;
			foreach (PathCreator.Node n in ClosedNodes) {
				if (Array.IndexOf(pathway, n) != -1) GUI.backgroundColor = Color.grey;
				if (n == startNode) GUI.backgroundColor = Color.blue;
				pos = Camera.main.WorldToScreenPoint(new Vector3(n.GetLocation().x - sizea.x/2, -n.GetLocation().y - sizea.y/2));
				//Debug.Log (new Vector3(n.GetLocation().x - sizea.x/2, n.GetLocation().y + sizea.y/2) + " " + pos);
				if (GUI.Button (new Rect (pos.x, pos.y, sizeb.x, sizeb.y), "")) {
					Debug.Log (n.F + " " + n.H + " " + n.G);
				}
				if (n == startNode || Array.IndexOf(pathway, n) != -1) GUI.backgroundColor = Color.red;
			}
		}
	}

	void GetChildren(PathCreator.Node rent) {
		foreach (PathCreator.Node n in nodeList.GetChildren(rent)) {
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
			ShortenPathway ();
		}
	}

	void ShortenPathway() {
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
						if (((x - i) == 1) && (xPos.x == iPos.x || xPos.y == iPos.y)) {
							cont = false;
						} else if (xPos.x == iPos.x || xPos.y == iPos.y) {
							for (int z = i + 1; z < x; z++) {
								keys.Add(z);
								if (xPos.x == iPos.x) {
									yp = (int) ((iPos.y < xPos.y)? z - i + iPos.y: iPos.y - (z - i));
									newPos = new Vector2(iPos.x, yp);
								} else {
									xp = (int) ((iPos.x < xPos.x)? z - i + iPos.x: iPos.x - (z - i));
									newPos = new Vector2(xp, iPos.y);
								}
								if (nodeI.GetNodeList().GetNodeAtPos(newPos).IsBoundary()) {
									isWall = true;
									cont = false;
								}
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

}
