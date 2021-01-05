using UnityEngine;
using System;

public class Graph<Position>
{
    public List<Node<Position>> Nodes {get; private set; }
    public List<Edge<Position>> Edges {get; private set; }


}

public class Node<Position> 
{
    public BestiaryDesc critter {get; set;}
    public Position position {get; set;}
}

public class Edge
{
    public Node<Position> From {get; set; }
    public Node<Position> To {get; set; }
}
