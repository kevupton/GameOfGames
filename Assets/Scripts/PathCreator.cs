using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PathCreator : MonoBehaviour {
	public float nodeSize = 1.0f;
	private Vector2 Size = new Vector2(1.0f, 1.0f);
	public bool diagonalOn = false;
	public Vector2 gCost = new Vector2 (10, 14);

	private List<Node> levelNodes = new List<Node> ();
	private Vector2 startPos;
	private Vector2 Count;


	public Vector2 sw = Vector2.zero;
	public Vector2 ne = Vector2.zero;

	public class Node {
		private Vector2 location;
		public int G = 0;
		public int H = 0;
		private int key = 0;
		private bool isBoundary = false;
		private Vector2 size;
		private Node parent = null;
		private NodeList list;

		public Node(Vector2 loc, int k) {
			location = loc;
			key = k;
		}

		public Node(Vector2 loc, bool ic, int g, int h, int k, Node p) {
			location = loc;
			isBoundary = ic;
			G = g;
			H = h;
			key = k;
			parent = p;
		}

		public void SetNodeList(NodeList l) {
			list = l;
		}

		public NodeList GetNodeList() {
			return list;
		}

		public void SetBoundary(bool x) {
			isBoundary = x;
		}

		public bool IsBoundary() {
			return isBoundary;
		}

		public Node Clone() {
			return new Node (location, isBoundary, G, H, key, parent);
		}

		public int GetKey() {
			return key;
		}

		public Node GetParent() {
			return parent;
		}

		public void SetParent(Node p) {
			parent = p;
		}

		public void RemoveParent() {
			parent = null;
		}

		public Vector2 GetLocation() {
			return location;
		}

		public bool HasParent() {
			if (parent != null) return true;
			else return false;
		}

		public int F {
			get {
				return G + H;
			}
		}
		public void SetSize(Vector2 sz) {
			size = sz;
		}

		public Vector2 GetSize() {
			return size;
		}
	}

	public class NodeList: IEnumerable {
		private List<Node> nodeList = new List<Node> ();
		private Vector2 count;
		private Vector2 start;
		private Vector2 size;
		private Node destNode;
		private Node startNode;
		private bool diagonal;
		private Vector2 cost;
		private int position = 0;

		private class Enumerator: IEnumerator {
			private List<Node> nodeList;
			private int position = -1;

			public Enumerator(List<Node> list) {
				nodeList = list;
			}

			private IEnumerator getEnumerator()
			{
				return (IEnumerator)this;
			}

			public void Reset() {
				position = -1;
			}
			
			public bool MoveNext() {
				position++;
				return (position < nodeList.Count);
			}
			
			public object Current {
				get 
				{ 
					try 
					{
						return nodeList[position];
					}
					
					catch (IndexOutOfRangeException)
					{
						throw new InvalidOperationException();
					}
				}
			}
		}

		public IEnumerator GetEnumerator()
		{
			return new Enumerator(nodeList);
		}

		public NodeList(Vector2 c, Vector2 st, Vector2 sz, Vector2 cst, bool dia = false){
			count = c;
			start = st;
			size = sz;
			diagonal = dia;
			cost = cst;
		}

		public bool IsDiagonal() {
			return diagonal;
		}

		public void SetStartNode(Transform s) {
			startNode = GetClosestNode(s);
		}

		public void SetDestNode(Transform d) {
			destNode = GetClosestNode(d);
		}

		public Node GetStartNode() {
			return startNode;
		}

		public Node GetEndNode() {
			return destNode;
		}

		public Node[] GetChildren(Node node) {
			Vector2 pos = GetNodePos (node);
			int x = (int) pos.x;
			int y = (int) pos.y;

			Node[] children = new Node[0];

			for (int s = (x > 0)? x - 1: 0; s <= ((x >= (count.x - 1))? x: x + 1); s++) {
				for (int v = (y > 0)? y - 1: 0; v <= ((y >= (count.y -1))? y: y + 1); v++) {
					if (diagonal || (!diagonal && (x == s || y == v))) {
						int key = (int) (count.x * v + s);
						
						Node n = nodeList[key];
						
						if (!n.HasParent() && n!= startNode && !n.IsBoundary()) {
							n.SetParent(node);
							SetG (n);
							SetH (n);
							Array.Resize(ref children, children.Length + 1);
							children[children.Length - 1] = n;
						}
					}
				}
			}
			
			return children;
		}
		
		public Vector2 GetNodePos(Node node) {
			int key = node.GetKey ();
			
			int x = (int) (key % count.x);
			int y = (int) Math.Floor (key / count.x);
			
			return new Vector2 (x, y);
		}

		public Node GetNodeAtPos(Vector2 v) {
			int x = (int)v.x;
			int y = (int)v.y;

			int key = (int) (count.x * y + x);
			if (key > nodeList.Count || key < 0) {
				Debug.Log (v);
				Debug.Log (key);
			}
			return nodeList[key];
		}
		
		public Node GetClosestNode(Transform obj) {
			int x = (int) Math.Round((obj.position.x - start.x) / size.x);
			int y = (int) Math.Round((start.y - obj.position.y) / size.y);
			return GetNodeAtPos (new Vector2 (x, y));
		}

		void SetH(Node node) {
			Vector2 nodePos = GetNodePos (node);
			Vector2 destPos = GetNodePos (destNode);

			node.H = (int) (Math.Abs (destPos.y - nodePos.y) + Math.Abs (destPos.x - nodePos.x));
		}
		
		void SetG(Node node) {
			Vector2 nodePos = GetNodePos (node);
			Vector2 parPos = GetNodePos (node.GetParent());
			int c = 0;
			if (parPos.x == nodePos.x || parPos.y == nodePos.y) {
				c = (int) cost.x;
			} else {
				c = (int) cost.y;
			}
			if (node.HasParent ()) {
				c += node.GetParent().G;
			}
			node.G = c;
		}

		public void Add(Node node) {
			Node copy = node.Clone ();
			copy.SetSize (size);
			copy.SetNodeList (this);
			nodeList.Add (copy);
		}

		public int Length {
			get {
				return nodeList.Count;
			}
		}

		public Vector2 GetSize() {
			return size;
		}

		public Vector2 GetDimensions() {
			return count;
		}
	}

	// Use this for initialization
	void Awake () {
		CreateNodes (sw, ne);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public NodeList GetNodeList(Transform startObj, Transform destObj) {
		NodeList list = new NodeList (Count, startPos, Size, gCost, diagonalOn);
		foreach (Node n in levelNodes) {
			list.Add(n);
		}
		list.SetDestNode (destObj);
		list.SetStartNode (startObj);

		return list;
	}

	public void CreateNodes(Vector2 sw, Vector2 ne) {
		Count = new Vector2(
			(float) Math.Floor(Math.Abs (((sw.x - ne.x) / nodeSize))),
 			(float) Math.Floor(Math.Abs (((sw.y - ne.y) / nodeSize)))
			);

		Size = new Vector2 (Math.Abs (sw.x - ne.x)/Count.x, Math.Abs (sw.y - ne.y)/Count.y);
		startPos = new Vector2(Size.x / 2 + sw.x, ne.y - Size.y / 2);
		int z = 0;
		for (int y = 0; y < Count.y; y++) {
			for (int x = 0; x < Count.x; x++) {
				Vector2 curPos = new Vector2(Size.x * x + startPos.x, startPos.y - Size.y * y);
				Node node = new Node(curPos, z);
				node.SetSize(Size);
				node.SetBoundary(IsWall(node));
				levelNodes.Add(node);
				z++;
			}
		}
	}
	
	bool IsWall(Node node) {
		Vector2 topLeft = new Vector2 (node.GetLocation().x - node.GetSize().x/2, node.GetLocation().y + node.GetSize().y/2);
		Vector2 botRight = new Vector2 (node.GetLocation().x + node.GetSize().x/2, node.GetLocation().y - node.GetSize().y/2);
		Collider2D[] objects = Physics2D.OverlapAreaAll(topLeft, botRight);
		foreach (Collider2D obj in objects) {
			if (obj.tag == "Wall") {
				return true;
			}
		}
		return false;
	}
}


