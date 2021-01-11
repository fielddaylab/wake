using UnityEngine;
using System;
using Aqua;
using System.Collections.Generic;

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

public class Edge<Position>
{
    public Node<Position> From {get; set; }
    public Node<Position> To {get; set; }
}
